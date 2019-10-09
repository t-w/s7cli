using System.Collections.Generic;
using System.Linq;

using CommandLine;


namespace S7_cli
{
    class OptionParser
    {
        public static int parse(string[] args)
        {
            var parseErrors = new List<CommandLine.Error>();
            
            // TODO: Make S7Command methods static, so that no instance is required
            S7Command cmd = new S7Command();

            // TODO: Consistent naming for getList* or List* (should actually be getNames...)
            // TODO: Remove huge try catch block, catch at a lower level and return error code
            try
            {
                var result = Parser.Default.ParseArguments(args, OptionTypes.get())
                    .WithParsed<Options>(opts =>
                        setGeneralOptions(debugLevel: opts.debug, serverMode: opts.serverMode == "y"))
                    .WithParsed<CreateProjectOptions>(opts =>
                        cmd.createProject(opts.projectName, opts.projectDir))
                    .WithParsed<CreateLibOptions>(opts =>
                        cmd.createLibrary(opts.libName, opts.libDir))
                    .WithParsed<ImportConfigOptions>(opts =>
                        cmd.importConfig(opts.project, opts.config))
                    .WithParsed<ExportConfigOptions>(opts =>
                        cmd.exportConfig(opts.project, opts.station, opts.config))

                    .WithParsed<ListProgramsOptions>(opts =>
                        cmd.getListOfPrograms(opts.project))
                    .WithParsed<ListStationsOptions>(opts =>
                        cmd.getListOfStations(opts.project))
                    .WithParsed<ListContainersOptions>(opts =>
                        cmd.getListOfContainers(opts.project))
                    .WithParsed<listConnectionsOptions>(opts =>
                        cmd.getListOfConnections(opts.project))

                    .WithParsed<ImportSymbolsOptions>(opts =>
                        cmd.importSymbols(opts.project, opts.symbols, opts.program, opts.conflictOk == "y"))
                    .WithParsed<ExportSymbolsOptions>(opts =>
                        cmd.exportSymbols(opts.project, opts.program, opts.output, opts.force == "y"))
                    .WithParsed<ListSourcesOptions>(opts =>
                        cmd.listSources(opts.project, opts.program))
                    .WithParsed<ListBlocksOptions>(opts =>
                        cmd.listBlocks(opts.project, opts.program))
                    .WithParsed<ImportLibSourcesOptions>(opts =>
                        cmd.importLibSources(opts.project, opts.library, opts.libraryProgram, opts.program, opts.force == "y"))
                    .WithParsed<ImportLibBlocksOptions>(opts =>
                        cmd.importLibBlocks(opts.project, opts.library, opts.libraryProgram, opts.program, opts.force == "y"))
                    .WithParsed<ImportSourcesOptions>(opts =>
                        cmd.importSources(opts.project, opts.program, opts.sources.Split(','), opts.force == "y"))
                    .WithParsed<ImportSourcesDirOptions>(opts =>
                        cmd.importSourcesDir(opts.project, opts.program, opts.sourcesDir, opts.force == "y"))
                    .WithParsed<CompileSourcesOptions>(opts =>
                        cmd.compileSources(opts.project, opts.program, opts.sources.Split(',')))
                    .WithParsed<ExportSourcesOptions>(opts =>
                        cmd.exportSources(opts.project, opts.program, opts.sources.Split(','), opts.outputDir))
                    .WithParsed<ExportAllSourcesOptions>(opts =>
                        cmd.exportAllSources(opts.project, opts.program, opts.outputDir))

                    .WithParsed<ExportAllStationsOptions>(opts =>
                        cmd.exportAllStations(opts.project, opts.outputDir))

                    .WithParsed<ExportProgramStructureOptions>(opts =>
                        cmd.exportProgramStructure(opts.project, opts.program, opts.output))
                    .WithParsed<CompileStationOptions>(opts =>
                        cmd.compileStation(opts.project, opts.station))

                    .WithParsed<CompileAllConnectionsOptions>(opts =>
                        cmd.compileAllConnections(opts.project))
                    .WithParsed<CompileAllStationsOptions>(opts =>
                        cmd.compileAllStations(opts.project))
                    .WithParsed<DownloadAllConnectionsOptions>(opts =>
                        cmd.downloadAllConnections(opts.project))

                    .WithParsed<DownloadSystemDataOptions>(opts =>
                        cmd.downloadSystemData(opts.project, opts.program, opts.force == "y"))
                    .WithParsed<DownloadBlocksOptions>(opts =>
                        cmd.downloadBlocks(opts.project, opts.program, opts.force == "y"))
                    .WithParsed<DownloadOptions>(opts =>
                        cmd.download(opts.project, opts.program))
                    .WithParsed<StartCpuOptions>(opts =>
                        cmd.startCPU(opts.project, opts.program))
                    .WithParsed<StopCpuOptions>(opts =>
                        cmd.stopCPU(opts.project, opts.program))

                    .WithParsed<DownloadStationOptions>(opts =>
                        cmd.downloadStation(opts.project, opts.stationName, opts.stationType, opts.allStations == "y",
                            opts.moduleName, opts.force == "y"))
                    .WithParsed<StopStationOptions>(opts =>
                        cmd.stopStation(opts.project, opts.stationName, opts.stationType, opts.allStations == "y",
                            opts.moduleName))
                    .WithParsed<StartStationOptions>(opts =>
                        cmd.startStation(opts.project, opts.stationName, opts.stationType, opts.allStations == "y",
                            opts.moduleName))

                    .WithParsed<updateNetworkInterfaceOptions>(opts =>
                        cmd.updateNetworkInterface(opts.project, opts.station, opts.module, opts.ipAddress, opts.subnetMask))
                    .WithParsed<renameStationOptions>(opts =>
                        cmd.renameStationOptions(opts.project, opts.target, opts.name))

                    .WithNotParsed(errors => parseErrors = errors.ToList());
            }
            catch (S7ProjectNotOpenException e)
            {
                Logger.log("Error: exception: project not opened with info:\n" + e.ToString() + ", " + e.Message + "\n");
                return 1;
            }

            // Handle parsing errors
            if (parseErrors.Any())
            {
                foreach (Error error in parseErrors)
                {
                    // Not selecting a verb or requesting help is handled as success
                    Logger.log(error.ToString());
                    if (!(error is NoVerbSelectedError || error is VersionRequestedError
                       || error is HelpVerbRequestedError || error is HelpRequestedError))
                    {
                        return 1;
                    }
                    S7CommandStatus.set_status(S7CommandStatus.success);
                }
            }
            
            return 0;
        }

        /// <summary>
        /// Sets general options, such as debug level and unnatended server mode
        /// </summary>
        /// <param name="debugLevel">Logging level (check Logger class)</param>
        /// <param name="serverMode">Unattended server mode status</param>
        static public void setGeneralOptions(int debugLevel = Logger.min_debug_level, bool serverMode = true)
        {
            setDebugLevel(debugLevel);
            setUnattendedServerMode(serverMode);
        }

        /// <summary>
        /// Sets Logger debugging level
        /// </summary>
        /// <param name="debugLevel">Logging level (check Logger class)</param>
        static public void setDebugLevel(int debugLevel)
        {
            int min = Logger.min_debug_level;
            int max = Logger.max_debug_level;
            if (debugLevel >= min && debugLevel <= max)
            {
                Logger.setLevel(debugLevel);
            }
            else
            {
                Logger.log_error($"Specified bug level is out of range ({min}-{max})");
            }
        }

        /// <summary>
        /// Sets unnatended server mode option (disables UI prompts, selecting default option)
        /// </summary>
        /// <param name="serverMode">Unnatended server mode status</param>
        static public void setUnattendedServerMode(bool serverMode = true)
        {
            SimaticAPI api = SimaticAPI.Instance;
            if (serverMode)
                api.enableUnattendedServerMode();
            else
                api.disableUnattendedServerMode();
        }
    }
}
