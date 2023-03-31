using System.Collections.Generic;
using System.Linq;
using System;

using CommandLine;
using Serilog;
using Serilog.Core;
using Serilog.Events;

using S7Lib;
using Newtonsoft.Json;

namespace S7Cli
{
    public sealed class OptionParser : IDisposable
    {
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
        public OptionParser()
        {
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
        /// <param name="run">Whether to run the command</param>
        public void Parse(string[] args, bool run = true)
        {
            var result = Parser.Default.ParseArguments(args, OptionTypes.Get());

            try
            {
                result.WithNotParsed(errors => HandleErrors(errors.ToList()));
                result.WithParsed<Options>(opts => SetGeneralOptions(opts.Verbose));
            }
            catch (FormatException exc)
            {
                throw new ArgumentException($"Could not parse command `{String.Join(" ", args)}`", nameof(args), exc);
            }

            // Early return, if we choose not to run the parsed command
            if (!run)
                return;

            try
            {
                result.WithParsed<Options>(RunCommand);
            }
            catch (Exception exc)
            {
                Log.Error(exc, $"Could not run command `{String.Join(" ", args)}`.");
                throw;
            }
        }

        /// <summary>
        /// Handles parsing errors
        /// </summary>
        /// <param name="errors">List of parsing errors</param>
        private void HandleErrors(IList<Error> errors)
        {
            if (errors.Any())
            {
                foreach (Error error in errors)
                {
                    if (!(error is VersionRequestedError) && !(error is HelpVerbRequestedError) && !(error is HelpRequestedError))
                    {
                        throw new FormatException($"Unhandled parsing error: {error}");
                    }
                }
            }
        }

        /// <summary>
        /// Sets general options
        /// </summary>
        /// <param name="verbose">Whether to activate verbose option</param>
        private void SetGeneralOptions(bool verbose)
        {
            LogLevel.MinimumLevel = (verbose) ?
                LogEventLevel.Verbose : LogEventLevel.Information;
        }

        /// <summary>
        /// Logs the output of a List* function to either console or Log with Information level
        /// </summary>
        /// <param name="results">Output of List* function</param>
        /// <param name="resultName">Name of result to be prefixed to log output</param>
        /// <param name="json">Whether to produce JSON output to Console (true) or to Loh (false)</param>
        private void LogListResult(IList<String> results, string resultName="", bool json = false)
        {
            if (json)
            {
                Console.Write(JsonConvert.SerializeObject(results));
            }
            else
            {
                var logLines = results.Select(result => $"{resultName}={result}").ToList();
                foreach (var logLine in logLines)
                    Log.Information(logLine);
            }
        }

        private void RunCommand(object options)
        {
            Api = new S7Handle(logger: Log);

            switch (options)
            {
                case ListProjectsOptions _:
                    var projects = Api.ListProjects();
                    foreach (var item in projects)
                        Log.Information("Project={Name}, LogPath={LogPath}", item.Value, item.Key);
                    break;
                case ListProgramsOptions opt:
                    var programs = Api.ListPrograms(opt.Project, opt.Json);
                    LogListResult(programs, "Program", opt.Json);
                    break;
                case ListContainersOptions opt:
                    var containers = Api.ListContainers(opt.Project);
                    foreach (var container in containers)
                        Log.Information("Container={Container}", container);
                    break;
                case ListStationsOptions opt:
                    var stations = Api.ListStations(opt.Project);
                    foreach (var station in stations)
                        Log.Information("Station={Station}", station);
                    break;
                case ListModulesOptions opt:
                    var modules = Api.ListModules(opt.Project);
                    foreach (var module in modules)
                        Log.Information("Module={Module}", module);
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
                                         project: opt.Project, program: opt.Program,
                                         overwrite: opt.Overwrite);
                    break;
                case ImportLibBlocksOptions opt:
                    Api.ImportLibBlocks(library: opt.Library, libProgram: opt.LibProgram,
                                        project: opt.Project, program: opt.Program,
                                        overwrite: opt.Overwrite);
                    break;
                case ImportSymbolsOptions opt:
                    Api.ImportSymbols(opt.Project, opt.Program, opt.SymbolFile,
                                      overwrite: opt.Overwrite, nameLeading: opt.NameLeading,
                                      allowConflicts: opt.AllowConflicts);
                    break;
                case ExportSymbolsOptions opt:
                    Api.ExportSymbols(opt.Project, opt.Program, opt.SymbolFile);
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
                        if (!Confirm($"[ONLINE] Start {opt.Project}\\{opt.Program}"))
                            break;
                    Api.StartProgram(opt.Project, opt.Program);
                    break;
                case StopProgramOptions opt:
                    if (!opt.Force)
                        if (!Confirm($"[ONLINE] Stop {opt.Project}\\{opt.Program}"))
                            break;
                    Api.StopProgram(opt.Project, opt.Program);
                    break;
                case DownloadProgramBlocksOptions opt:
                    if (!opt.Force)
                        if (!Confirm($"[ONLINE] Download blocks in {opt.Project}\\{opt.Program}"))
                            break;
                    Api.DownloadProgramBlocks(opt.Project, opt.Program, opt.Overwrite);
                    break;
                case removeProgramOnlineBlocksOptions opt:
                    if (!opt.Force)
                        if (!Confirm($"[ONLINE] Remove user blocks in {opt.Project}\\{opt.Program}"))
                            break;
                    Api.RemoveProgramOnlineBlocks(opt.Project, opt.Program);
                    break;
                default:
                    throw new ArgumentException($"Unknown options {options}", nameof(options));
            }

            // TODO Save Project? Review auto-save logic
        }

        /// <summary>
        /// Prompts user to confirm a portentially dangerous command
        /// </summary>
        /// <param name="message">Command description</param>
        /// <returns></returns>
        private bool Confirm(string message)
        {
            Console.WriteLine($"Are you sure you want to perform the following command:\n" +
                              $" - {message} (N/y) ?");
            var line = Console.ReadLine();
            if (line.ToLower() == "y") return true;
            Console.WriteLine("Command cancelled. To confirm type 'y'");
            return false;
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
