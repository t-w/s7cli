using System.Collections.Generic;
using System.Linq;
using System;

using CommandLine;
using Serilog;
using Serilog.Core;
using Serilog.Events;

using S7Lib;


namespace S7Cli
{
    public sealed class OptionParser : IDisposable
    {
        /// <summary>
        /// Command return value
        /// </summary>
        public int ReturnValue;

        /// <summary>
        /// Whether to run command (for parser testing purposes)
        /// </summary>
        private readonly bool Run;
        /// <summary>
        /// Configured logger instance
        /// </summary>
        private readonly Logger Log;
        /// <summary>
        /// Dynamic switch log level
        /// </summary>
        private readonly LoggingLevelSwitch LogLevel;
        /// <summary>
        /// Context for running S7Lib commands
        /// </summary>
        private S7Handle Api = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="run">Whether to run parsed command</param>
        public OptionParser(bool run = true)
        {
            Run = run;
            LogLevel = new LoggingLevelSwitch();
            Log = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(LogLevel)
                .WriteTo.Console()
                .CreateLogger();
        }

        public void Dispose()
        {
            Api?.Dispose();
            Log?.Dispose();
        }

        /// <summary>
        /// Parses command-line arguments and runs respective commands, by default.
        /// We can choose to not run a command for testing the argument parsing logic.
        /// </summary>
        /// <param name="args">Command-line args</param>
        /// <returns>0 on success, 1 otherwise</returns>
        public int Parse(string[] args)
        {
            var result = Parser.Default.ParseArguments(args, OptionTypes.Get());
            result.WithNotParsed(errors => HandleErrors(errors.ToList()));

            // Early return, if we choose not to run the parsed command
            if (!Run)
                return ReturnValue;

            result.WithParsed<Options>(opts => SetGeneralOptions(opts.Verbose));

            try
            {
                result.WithParsed<Options>(RunCommand);
            }
            catch (Exception exc)
            {
                Log.Error(exc, $"Could not run command.");
                ReturnValue = 1;
            }

            return ReturnValue;
        }

        /// <summary>
        /// Handles parsing errors
        /// </summary>
        /// <param name="errors">List of parsing errors</param>
        private void HandleErrors(IList<Error> errors)
        {
            ReturnValue = 0;
            if (errors.Any())
            {
                ReturnValue = 1;
                foreach (Error error in errors)
                {
                    // Not selecting a verb or requesting version or help is handled as success
                    if (error is NoVerbSelectedError || error is VersionRequestedError
                       || error is HelpVerbRequestedError || error is HelpRequestedError)
                    {
                        ReturnValue = 0; return;
                    }
                }
            }
        }

        /// <summary>
        /// Sets general options
        /// </summary>
        private void SetGeneralOptions(bool verbose)
        {
            LogLevel.MinimumLevel = (verbose) ?
                LogEventLevel.Verbose : LogEventLevel.Information;
        }

        /// <summary>
        /// Prompts user to confirm a portentially dangerous command
        /// </summary>
        /// <param name="message">Command description</param>
        /// <returns></returns>
        private bool Confirm(string message)
        {
            System.Console.WriteLine($"Are you sure you want to perform the following command:\n" +
                                     $" - {message} (N/y) ?");
            var line = System.Console.ReadLine();
            if (line.ToLower() == "y") return true;
            System.Console.WriteLine("Command cancelled. To confirm type 'y'");
            return false;
        }

        private void RunCommand(object options)
        {
            Api = new S7Handle(log: Log);

            switch (options)
            {
                case ListProjectsOptions _:
                    var projects = new Dictionary<string, string>();
                    Api.ListProjects(ref projects);
                    break;
                case ListProgramsOptions opt:
                    var programs = new List<string>();
                    Api.ListPrograms(ref programs, opt.Project);
                    break;
                case ListContainersOptions opt:
                    var containers = new List<string>();
                    Api.ListContainers(ref containers, opt.Project);
                    break;
                case ListStationsOptions opt:
                    var stations = new List<string>();
                    Api.ListStations(ref stations, opt.Project);
                    break;
                case CreateProjectOptions opt:
                    Api.CreateProject(opt.ProjectName, opt.ProjectDir);
                    break;
                case CreateLibraryOptions opt:
                    Api.CreateLibrary(opt.ProjectName, opt.ProjectDir);
                    break;
                case RegisterProjectOptions opt:
                    Api.RegisterProject(opt.ProjectFilePath);
                    break;
                case RemoveProjectOptions opt:
                    if (!opt.Force)
                        if (!Confirm($"Remove project {opt.Project}"))
                            break;
                    Api.RemoveProject(opt.Project);
                    break;
                case ImportSourceOptions opt:
                    Api.ImportSource(opt.Project, opt.Program, opt.Source, opt.Overwrite);
                    break;
                case ImportSourcesDirOptions opt:
                    Api.ImportSourcesDir(opt.Project, opt.Program, opt.SourcesDir, opt.Overwrite);
                    break;
                case ExportAllSourcesOptions opt:
                    Api.ExportAllSources(opt.Project, opt.Program, opt.SourcesDir);
                    break;
                case ImportLibSourcesOptions opt:
                    Api.ImportLibSources(library: opt.Library, libProgram: opt.LibProgram,
                                         project: opt.Project, projProgram: opt.ProjProgram,
                                         overwrite: opt.Overwrite);
                    break;
                case ImportLibBlocksOptions opt:
                    Api.ImportLibBlocks(library: opt.Library, libProgram: opt.LibProgram,
                                        project: opt.Project, projProgram: opt.ProjProgram,
                                        overwrite: opt.Overwrite);
                    break;
                case ImportSymbolsOptions opt:
                    Api.ImportSymbols(opt.Project, opt.ProgramPath, opt.SymbolFile,
                                      flag: opt.Flag, allowConflicts: opt.AllowConflicts);
                    break;
                case ExportSymbolsOptions opt:
                    Api.ExportSymbols(opt.Project, opt.ProgramPath, opt.SymbolFile);
                    break;
                case CompileSourceOptions opt:
                    Api.CompileSource(opt.Project, opt.Program, opt.Source);
                    break;
                case CompileSourcesOptions opt:
                    Api.CompileSources(opt.Project, opt.Program, opt.Sources.ToList());
                    break;
                case CompileAllStationsOptions opt:
                    Api.CompileAllStations(opt.Project, opt.AllowFail);
                    break;
                case EditModuleOptions opt:
                    var properties = ParseModuleProperties(opt);
                    Api.EditModule(opt.Project, opt.Station, opt.Rack, opt.Module, properties);
                    break;
                case StartProgramOptions opt:
                    if (!opt.Force)
                        if (!Confirm($"[ONLINE] Start {opt.Project}:{opt.Station}:{opt.Module}:{opt.Program}"))
                            break;
                    Api.StartProgram(opt.Project, opt.Station, opt.Module, opt.Program);
                    break;
                case StopProgramOptions opt:
                    if (!opt.Force)
                        if (!Confirm($"[ONLINE] Stop {opt.Project}:{opt.Station}:{opt.Module}:{opt.Program}"))
                            break;
                    Api.StopProgram(opt.Project, opt.Station, opt.Module, opt.Program);
                    break;
                case DownloadProgramBlocksOptions opt:
                    if (!opt.Force)
                        if (!Confirm($"[ONLINE] Download blocks in {opt.Project}:{opt.Station}:{opt.Module}:{opt.Program}"))
                            break;
                    Api.DownloadProgramBlocks(opt.Project, opt.Station, opt.Module, opt.Program, opt.Overwrite);
                    break;
                default:
                    ReturnValue = 1;
                    Log.Error($"Unknown options {options}");
                    break;
            }

            // TODO Save Project? Review auto-save logic
        }

        private Dictionary<string, object> ParseModuleProperties(EditModuleOptions opt)
        {
            bool parsedBool;
            var propertyDict = new Dictionary<string, object>();
            if (!String.IsNullOrEmpty(opt.IPAddress))
                propertyDict["IPAddress"] = opt.IPAddress;
            if (!String.IsNullOrEmpty(opt.SubnetMask))
                propertyDict["SubnetMask"] = opt.SubnetMask;
            if (!String.IsNullOrEmpty(opt.RouterAddress))
                propertyDict["RouterAddress"] = opt.RouterAddress;
            if (!String.IsNullOrEmpty(opt.MACAddress))
                propertyDict["MACAddress"] = opt.MACAddress;
            if (!String.IsNullOrEmpty(opt.IPActive))
            {
                if (Boolean.TryParse(opt.IPActive, out parsedBool))
                    propertyDict["IPActive"] = parsedBool;
                else
                    Log.Warning($"Could not parse bool from --ipActive \"{opt.IPActive}\". Ignoring.");
            }
            if (!String.IsNullOrEmpty(opt.RouterActive))
            {
                if (Boolean.TryParse(opt.RouterActive, out parsedBool))
                    propertyDict["RouterActive"] = parsedBool;
                else
                    Log.Warning($"Could not parse bool from --routerActive \"{opt.RouterActive}\". Ignoring.");
            }
            return propertyDict;
        }

    }
}
