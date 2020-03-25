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
            if (errors.Any())
            {
                foreach (Error error in errors)
                {
                    // Not selecting a verb or requesting version or help is handled as success
                    if (!(error is NoVerbSelectedError || error is VersionRequestedError
                       || error is HelpVerbRequestedError || error is HelpRequestedError))
                    {
                        ReturnValue = 1; return;
                    }
                }
            }
            ReturnValue = 0;
        }

        /// <summary>
        /// Sets general options
        /// </summary>
        public void SetGeneralOptions(bool verbose)
        {
            LogLevel.MinimumLevel = (verbose)?
                LogEventLevel.Verbose : LogEventLevel.Information;
        }

        private void RunCommand(object options)
        {
            int rv = 0;
            var ctx = Context;

            switch (options)
            {
                case ListProjectsOptions opt:
                    var output = new List<KeyValuePair<string, string>>();
                    rv = Api.ListProjects(ctx, ref output);
                    break;
                default:
                    break;
            }
            ReturnValue = rv;
        }
    }
}
