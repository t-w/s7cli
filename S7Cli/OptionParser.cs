using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Serilog;
using Serilog.Core;


using S7Lib;
using Serilog.Events;

namespace S7Cli
{
    public class OptionParser
    {
        /// <summary>
        /// Command return value
        /// </summary>
        public int ReturnValue;

        /// <summary>
        /// Whether to run command (for parser testing purposes)
        /// </summary>
        private bool Run;
        /// <summary>
        /// Configured logger instance
        /// </summary>
        private Logger Log;
        /// <summary>
        /// Dynamic switch log level
        /// </summary>
        private LoggingLevelSwitch LogLevel;
        /// <summary>
        /// Context for running S7Lib commands
        /// </summary>
        private S7Context Context;
        
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
            Context = new S7Context(log: Log);
        }

        /// <summary>
        /// Parses command-line arguments and runs respective commands, by default.
        /// We can choose to not run a command for testing the argument parsing logic.
        /// </summary>
        /// <param name="args">Command-line args</param>
        /// <param name="run">Whether to run command after parsing args</param>
        /// <returns>0 on success, 1 otherwise</returns>
        public int Parse(string[] args)
        {
            var result = Parser.Default.ParseArguments(args, OptionTypes.Get());
            result.WithNotParsed(errors => HandleErrors(errors.ToList()));
            
            // Early return, if we choose not to run the parsed command
            if (!Run) return ReturnValue;

            result.WithParsed<Options>(opts => SetGeneralOptions(opts.Verbose));
            result.WithParsed(RunCommand);
            
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
                ReturnValue = -1;
                foreach (Error error in errors)
                {
                    // Not selecting a verb or requesting version or help is handled as success
                    if (!(error is NoVerbSelectedError || error is VersionRequestedError
                       || error is HelpVerbRequestedError || error is HelpRequestedError))
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
            LogLevel.MinimumLevel = (verbose)?
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
            int rv = 0;
            var ctx = Context;

            switch (options)
            {
                case ListProjectsOptions opt:
                    var projects = new Dictionary<string, string>();
                    rv = Api.ListProjects(ctx, ref projects);
                    break;
                case ListProgramsOptions opt:
                    var programs = new List<string>();
                    rv = Api.ListPrograms(ctx, ref programs, opt.Project);
                    break;
                case ListContainersOptions opt:
                    var containers = new List<string>();
                    rv = Api.ListContainers(ctx, ref containers, opt.Project);
                    break;
                case ListStationsOptions opt:
                    var stations = new List<string>();
                    rv = Api.ListStations(ctx, ref stations, opt.Project);
                    break;
                case CreateProjectOptions opt:
                    rv = Api.CreateProject(ctx, opt.ProjectName, opt.ProjectDir);
                    break;
                case CreateLibraryOptions opt:
                    rv = Api.CreateLibrary(ctx, opt.ProjectName, opt.ProjectDir);
                    break;
                case RegisterProjectOptions opt:
                    rv = Api.RegisterProject(ctx, opt.ProjectFilePath);
                    break;
                case RemoveProjectOptions opt:
                    if (!opt.Force)
                        if (!Confirm($"Remove project {opt.Project}"))
                            break;
                    rv = Api.RemoveProject(ctx, opt.Project);
                    break;
                case ImportSourceOptions opt:
                    rv = Api.ImportSource(ctx,
                        opt.Project, opt.Program, opt.Source, opt.Overwrite);
                    break;
                case ImportSourcesDirOptions opt:
                    rv = Api.ImportSourcesDir(ctx,
                        opt.Project, opt.Program, opt.SourcesDir, opt.Overwrite);
                    break;
                case ExportAllSourcesOptions opt:
                    rv = Api.ExportAllSources(ctx,
                        opt.Project, opt.Program, opt.SourcesDir);
                    break;
                case ImportLibSourcesOptions opt:
                    rv = Api.ImportLibSources(ctx,
                        library: opt.Library, libProgram: opt.LibProgram,
                        project: opt.Project, projProgram: opt.ProjProgram,
                        overwrite: opt.Overwrite);
                    break;
                case ImportLibBlocksOptions opt:
                    rv = Api.ImportLibBlocks(ctx,
                        library: opt.Library, libProgram: opt.LibProgram,
                        project: opt.Project, projProgram: opt.ProjProgram,
                        overwrite: opt.Overwrite);
                    break;
                case ImportSymbolsOptions opt:
                    rv = Api.ImportSymbols(ctx, opt.Project, opt.ProgramPath, opt.SymbolFile,
                        flag: opt.Flag, allowConflicts: opt.AllowConflicts);
                    break;
                case ExportSymbolsOptions opt:
                    rv = Api.ExportSymbols(ctx, opt.Project, opt.ProgramPath, opt.SymbolFile);
                    break;
                case CompileSourceOptions opt:
                    rv = Api.CompileSource(ctx, opt.Project, opt.Program, opt.Source);
                    break;
                case CompileSourcesOptions opt:
                    rv = Api.CompileSources(ctx, opt.Project, opt.Program, opt.Sources.ToList());
                    break;
                case CompileAllStationsOptions opt:
                    rv = Api.CompileAllStations(ctx, opt.Project, opt.AllowFail);
                    break;
                case StartProgramOptions opt:
                    if (!opt.Force)
                        if (!Confirm($"[ONLINE] Start {opt.Project}:{opt.Station}:{opt.Module}:{opt.Program}"))
                            break;
                    rv = Online.StartProgram(ctx,
                        opt.Project, opt.Station, opt.Module, opt.Program);
                    break;
                case StopProgramOptions opt:
                    if (!opt.Force)
                        if (!Confirm($"[ONLINE] Stop {opt.Project}:{opt.Station}:{opt.Module}:{opt.Program}"))
                            break;
                    rv = Online.StopProgram(ctx,
                        opt.Project, opt.Station, opt.Module, opt.Program);
                    break;
                case DownloadProgramBlocksOptions opt:
                    if (!opt.Force)
                        if (!Confirm($"[ONLINE] Download blocks in {opt.Project}:{opt.Station}:{opt.Module}:{opt.Program}"))
                            break;
                    rv = Online.DownloadProgramBlocks(ctx,
                        opt.Project, opt.Station, opt.Module, opt.Program, opt.Overwrite);
                    break;
            }
            ReturnValue = rv;
        }
    }
}
