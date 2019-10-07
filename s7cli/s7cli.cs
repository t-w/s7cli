/************************************************************************
 * s7cli.cs - the s7cli main program                                    *
 *                                                                      *
 * Copyright (C) 2013-2019 CERN                                         *
 *                                                                      *
 * This program is free software: you can redistribute it and/or modify *
 * it under the terms of the GNU General Public License as published by *
 * the Free Software Foundation, either version 3 of the License, or    *
 * (at your option) any later version.                                  *
 *                                                                      *
 * This program is distributed in the hope that it will be useful,      *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of       *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the        *
 * GNU General Public License for more details.                         *
 *                                                                      *
 * You should have received a copy of the GNU General Public License    *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.*
 ************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Runtime.InteropServices;
using System.Reflection;

using CommandLine;

namespace S7_cli
{
    /// <summary>
    /// s7cli main program
    /// </summary>
    public class s7cli
    {
        /// <summary>
        /// Main program
        /// </summary>
        /// <param name="args">command-line arguments</param>
        /// <returns>0 on success, error code otherwise</returns>
        public static int Main(string[] args)
        {
            //Logger.setLevel(Logger.level_debug);   // switch on more debugging info
            show_logo();
            Console.Write("\n\n");

            S7Command cmd = new S7Command();
            // TODO: Correctly handle help and version commands, which do not set the command status
            var parseErrors = new List<CommandLine.Error>();

            // TODO: Consistent naming for getList* or List* (should actually be getNames...)
            // TODO: Remove huge try catch block, catch at a lower level and return error code
            try
            {
                var result = Parser.Default.ParseArguments(args, OptionTypes.get())
                    .WithParsed<Options>(opts =>
                        setGeneralOptions(debugLevel : opts.debug, serverMode : opts.serverMode == "y"))
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

                    .WithNotParsed(errors => parseErrors = errors.ToList());
            }
            catch (S7ProjectNotOpenException e)
            {
                Logger.log("Error: exception: project not opened with info:\n" + e.ToString() + ", " + e.Message + "\n");
                //S7cli_Status.exit_with_info(S7CommandStatus.failure);
                S7cli_Status.show(S7CommandStatus.failure);
                return S7CommandStatus.failure;
            }

            // Handle parsing errors
            if (parseErrors.Any())
            {
                S7CommandStatus.set_status(S7CommandStatus.failure);
            }

            //S7cli_Status.exit_with_info(S7CommandStatus.get_status());
            int status = S7CommandStatus.get_status();
            S7cli_Status.show(status);
            return status;

            //siemensPLCProject project = new siemensPLCProject("D:\\controls\\apps\\sector56\\plc\\mirror56");

            //System.Console.Write("\nsources LogPath: " + sources.LogPath + "\n");
            //S7SWItems src_modules = project.getSourceModules("ARC56_program");
            //System.Console.Write("\nsrouce modules count: " + src_modules.Count + "\n");

            //S7SWItem src_module = project.getSourceModule("ARC56_program", "4_Compilation_OB");
            //System.Console.Write(src_module.NameToString());
            //System.Console.Write(src_module.Name);
            //src_modules.Add("Test1", SimaticLib.S7SWObjType.S7Source ,"D:\\test1.scl");
            //project.addSourceModuleSCL("ARC56_program", "D:\\test1.scl");

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

        /// <summary>
        /// Return program version (as string)
        /// </summary>
        /// <returns>program version (string)</returns>
        static public string get_version()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.Major.ToString() + "." +
                   Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString() +
                   //Assembly.GetExecutingAssembly().GetName().Version.MajorRevision.ToString() + "." +
                   (Assembly.GetExecutingAssembly().GetName().Version.MinorRevision > 0 ? "." +
                     Assembly.GetExecutingAssembly().GetName().Version.MinorRevision.ToString() : "");
        }

        /// <summary>
        /// Shows program logo / info
        /// </summary>
        static public void show_logo()
        {
            string logo = @"

                  _|_|_|_|_|            _|  _|
          _|_|_|          _|    _|_|_|  _|
        _|_|            _|    _|        _|  _|
            _|_|      _|      _|        _|  _|
        _|_|_|      _|          _|_|_|  _|  _|   " + get_version() + @"

        Command-line interface for Siemens SIMATIC Step7(tm)
        (C) 2013-2019 CERN, TE-CRG-CE

        Authors: Michal Dudek, Tomasz Wolak
";
            Console.Write(logo);
        }
    }
}
