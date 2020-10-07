using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.ListOfString;

using Grpc.Core;
using S7Service;

using S7Lib;
using System.IO;

namespace Step7Server
{
    class Step7Impl : Step7.Step7Base
    {
        private string S7CliPath;
        private bool S7CliVerbose;
        static SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
        private Logger Log;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="s7CliPath">Path to S7Cli.exe</param>
        /// <param name="s7CliVerbose">Whether to call S7Cli with verbose flag</param>
        public Step7Impl(string s7CliPath, bool s7CliVerbose)
        {
            S7CliPath = s7CliPath;
            S7CliVerbose = s7CliVerbose;
            Log = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        }

        /// <summary>
        /// Launches a S7Cli child process and returns its exitCode and log
        /// </summary>
        /// <remarks>
        /// Spawning a separate child process is crucial for performing some S7Api requests,
        /// as the lower-level Simatic API can lock resources and interfere with subsequent
        /// function calls.
        /// This way, affected resources are freed as soon as the child proccess terminates.
        /// Additionally, only one S7Cli can be spawned at a time.
        /// This is ensured by a semaphore.
        /// </remarks>
        /// <param name="log">Output ordered list of log messages</param>
        /// <param name="arguments">Command-line arguments for S7CLi proccess</param>
        /// <param name="timeout">Timeout for acquiring semaphore, in seconds</param>
        /// <returns></returns>
        private int LaunchS7Cli(ref List<string> log, List<string> arguments, int timeout = 5*60)
        {
            var exitCode = -1;
            var argString = CreateArgumentString(arguments);

            try
            {
                Semaphore.Wait(millisecondsTimeout: timeout * 1000);
                Log.Information($"S7Cli.exe {argString}");
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = S7CliPath,
                    Arguments = argString,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                var process = Process.Start(processStartInfo);
                while (!process.StandardOutput.EndOfStream)
                    log.Add(process.StandardOutput.ReadLine());
                process.WaitForExit(milliseconds: timeout * 1000);
                exitCode = process.ExitCode;
            }
            catch (Exception exc)
            {
                Log.Error(exc, $"Could not run {S7CliPath}");
            }
            finally
            {
                Semaphore.Release();
            }
            return exitCode;
        }

        /// <summary>
        /// creates S7Cli command-line arguments
        /// </summary>
        /// <remarks>
        /// Handles the escaping of spaces in arguments, as well as the verbose option
        /// </remarks>
        /// <param name="arguments">List of command-line arguments for S7Cli</param>
        /// <returns></returns>
        private string CreateArgumentString(List<string> arguments)
        {
            string argString = "";
            if (S7CliVerbose) arguments.Add("-v");

            foreach (var arg in arguments)
            {
                // Surround argument in quotes to escape space chars, if present
                string procArg = arg.Contains(" ")? "\"" + arg + "\"" : arg;
                argString += $" {procArg}";
            }
            return argString;
        }

        private S7Context CreateApiContext(ref List<string> log)
        {
            var logger = new LoggerConfiguration().WriteTo.StringList(log).CreateLogger();
            var ctx = new S7Context(log: logger);
            return ctx;
        }

        private StatusReply CreateStatusReply(int rv, ref List<string> log)
        {
            var reply = new StatusReply { ExitCode = rv };
            reply.Log.AddRange(log);
            return reply;
        }

        private ListReply CreateListReply(int rv, ref List<string> log, ref List<string> list)
        {
            var reply = new ListReply { Status = CreateStatusReply(rv, ref log) };
            reply.Items.AddRange(list);
            return reply;
        }

        // Server commands

        // List commands

        private ListReply ListProjectsImpl(ListProjectsRequest req)
        {
            var log = new List<string>();
            var output = new Dictionary<string, string>();
            var ctx = CreateApiContext(ref log);
            var rv = Api.ListProjects(ctx, ref output);
            var projectList = new List<string>(output.Values);
            return CreateListReply(rv, ref log, ref projectList);
        }

        public override Task<ListReply> ListProjects(ListProjectsRequest req, ServerCallContext context)
        {
            return Task.FromResult(ListProjectsImpl(req));
        }

        private ListReply ListProgramsImpl(ListProgramsRequest req)
        {
            var log = new List<string>();
            var output = new List<string>();
            var ctx = CreateApiContext(ref log);
            var rv = Api.ListPrograms(ctx, ref output, req.Project);
            return CreateListReply(rv, ref log, ref output);
        }

        public override Task<ListReply> ListPrograms(ListProgramsRequest req, ServerCallContext context)
        {
            return Task.FromResult(ListProgramsImpl(req));
        }

        private ListReply ListContainersImpl(ListContainersRequest req)
        {
            var log = new List<string>();
            var output = new List<string>();
            var ctx = CreateApiContext(ref log);
            var rv = Api.ListContainers(ctx, ref output, req.Project);
            return CreateListReply(rv, ref log, ref output);
        }

        public override Task<ListReply> ListContainers(ListContainersRequest req, ServerCallContext context)
        {
            return Task.FromResult(ListContainersImpl(req));
        }

        private ListReply ListStationsImpl(ListStationsRequest req)
        {
            var log = new List<string>();
            var output = new List<string>();
            var ctx = CreateApiContext(ref log);
            var rv = Api.ListStations(ctx, ref output, req.Project);
            return CreateListReply(rv, ref log, ref output);
        }

        public override Task<ListReply> ListStations(ListStationsRequest req, ServerCallContext context)
        {
            return Task.FromResult(ListStationsImpl(req));
        }

        // Project commands

        private StatusReply CreateProjectImpl(CreateProjectRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "createProject",
                "--name", req.ProjectName, "--dir", req.ProjectDir };
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> CreateProject(CreateProjectRequest req, ServerCallContext context)
        {
            return Task.FromResult(CreateProjectImpl(req));
        }

        private StatusReply CreateLibraryImpl(CreateLibraryRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "createLibrary",
                "--name", req.ProjectName, "--dir", req.ProjectDir };
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> CreateLibrary(CreateLibraryRequest req, ServerCallContext context)
        {
            return Task.FromResult(CreateLibraryImpl(req));
        }

        private StatusReply RegisterProjectImpl(RegisterProjectRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "registerProject",
                "--projectFilePath", req.ProjectFilePath };
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> RegisterProject(RegisterProjectRequest req, ServerCallContext context)
        {
            return Task.FromResult(RegisterProjectImpl(req));
        }

        private StatusReply RemoveProjectImpl(RemoveProjectRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "removeProject",
                "--force", "--project", req.Project };
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> RemoveProject(RemoveProjectRequest req, ServerCallContext context)
        {
            return Task.FromResult(RemoveProjectImpl(req));
        }

        // Source commands

        private StatusReply ImportSourceImpl(ImportSourceRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "importSource",
                "--project", req.Project, "--program", req.Program, "--source", req.Source };
            if (req.Overwrite) arguments.Add("--overwrite");
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> ImportSource(ImportSourceRequest req, ServerCallContext context)
        {
            return Task.FromResult(ImportSourceImpl(req));
        }

        private StatusReply ImportSourcesDirImpl(ImportSourcesDirRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "importSourcesDir",
                "--project", req.Project, "--program", req.Program, "--sourcesDir", req.SourcesDir };
            if (req.Overwrite) arguments.Add("--overwrite");
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> ImportSourcesDir(ImportSourcesDirRequest req, ServerCallContext context)
        {
            return Task.FromResult(ImportSourcesDirImpl(req));
        }

        private StatusReply ExportAllSourcesImpl(ExportAllSourcesRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "exportAllSources",
                "--project", req.Project, "--program", req.Program, "--sourcesDir", req.SourcesDir };
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> ExportAllSources(ExportAllSourcesRequest req, ServerCallContext context)
        {
            return Task.FromResult(ExportAllSourcesImpl(req));
        }

        private StatusReply ImportLibSourcesImpl(ImportLibSourcesRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "importLibSources",
                "--project", req.Project, "--library", req.Library, "--projProgram", req.ProjProgram, "--libProgram", req.LibProgram };
            if (req.Overwrite) arguments.Add("--overwrite");
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> ImportLibSources(ImportLibSourcesRequest req, ServerCallContext context)
        {
            return Task.FromResult(ImportLibSourcesImpl(req));
        }

        private StatusReply ImportLibBlocksImpl(ImportLibBlocksRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "importLibBlocks",
                "--project", req.Project, "--library", req.Library, "--projProgram", req.ProjProgram, "--libProgram", req.LibProgram };
            if (req.Overwrite) arguments.Add("--overwrite");
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> ImportLibBlocks(ImportLibBlocksRequest req, ServerCallContext context)
        {
            return Task.FromResult(ImportLibBlocksImpl(req));
        }

        private StatusReply ImportSymbolsImpl(ImportSymbolsRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "importSymbols",
                "--project", req.Project, "--programPath", req.ProgramPath, "--symbolFile", req.SymbolFile, "--flag", $"{(int)req.Flag}" };
            if (req.AllowConflicts) arguments.Add("--allowConflicts");
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> ImportSymbols(ImportSymbolsRequest req, ServerCallContext context)
        {
            return Task.FromResult(ImportSymbolsImpl(req));
        }

        private StatusReply ExportSymbolsImpl(ExportSymbolsRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "exportSymbols",
                "--project", req.Project, "--programPath", req.ProgramPath, "--symbolFile", req.SymbolFile };
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> ExportSymbols(ExportSymbolsRequest req, ServerCallContext context)
        {
            return Task.FromResult(ExportSymbolsImpl(req));
        }

        private StatusReply CompileSourceImpl(CompileSourceRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "compileSource",
                "--project", req.Project, "--program", req.Program, "--source", req.Source };
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> CompileSource(CompileSourceRequest req, ServerCallContext context)
        {
            return Task.FromResult(CompileSourceImpl(req));
        }

        private StatusReply CompileSourcesImpl(CompileSourcesRequest req)
        {
            var log = new List<string>();
            var sources = new List<string>(req.Sources);
            var arguments = new List<string> { "compileSources",
                "--project", req.Project, "--program", req.Program, "--sources", string.Join(",", sources) };
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> CompileSources(CompileSourcesRequest req, ServerCallContext context)
        {
            return Task.FromResult(CompileSourcesImpl(req));
        }

        private StatusReply CompileAllStationsImpl(CompileAllStationsRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "compileAllStations", "--project", req.Project };
            if (req.AllowFail) arguments.Add("--allowFail");
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> CompileAllStations(CompileAllStationsRequest req, ServerCallContext context)
        {
            return Task.FromResult(CompileAllStationsImpl(req));
        }

        /// <summary>
        /// Parses EditModuleRequest into list of S7Cli command-line arguments
        /// </summary>
        /// <param name="req">Request message</param>
        /// <param name="arguments">Output command-line arguments for S7Cli</param>
        /// <param name="log">List of log messages</param>
        private void ParseModuleProperties(EditModuleRequest req, ref List<string> arguments, ref List<string> log)
        {
            foreach (var entry in req.Poperties)
            {
                if (entry.Key == "IPAddress")
                    arguments.Add("--ipAddress");
                else if (entry.Key == "SubnetMask")
                    arguments.Add("--subnetMask");
                else if (entry.Key == "RouterAddress")
                    arguments.Add("--routerAddress");
                else if (entry.Key == "MACAddress")
                    arguments.Add("--macAddress");
                else if (entry.Key == "IpActive")
                    arguments.Add("--ipActive");
                else if (entry.Key == "RouterActive")
                    arguments.Add("--routerActive");
                else
                {
                    log.Add($"[WRN] Could not parse module property {entry.Key}={entry.Value}");
                    continue; 
                }
                arguments.Add(entry.Value.ToString());
            }
        }

        private StatusReply EditModuleImpl(EditModuleRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "editModule", "--project", req.Project };
            ParseModuleProperties(req, ref arguments, ref log);
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> EditModule(EditModuleRequest req, ServerCallContext context)
        {
            return Task.FromResult(EditModuleImpl(req));
        }

        // Online commands

        private StatusReply StartProgramImpl(ProgramRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "startProgram",
                "--force", "--project", req.Project, "--station", req.Station, "--module", req.Module, "--program", req.Program };
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> StartProgram(ProgramRequest req, ServerCallContext context)
        {
            return Task.FromResult(StartProgramImpl(req));
        }

        private StatusReply StopProgramImpl(ProgramRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "stopProgram",
                "--force", "--project", req.Project, "--station", req.Station, "--module", req.Module, "--program", req.Program };
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> StopProgram(ProgramRequest req, ServerCallContext context)
        {
            return Task.FromResult(StopProgramImpl(req));
        }

        private StatusReply DownloadProgramBlocksImpl(DownloadProgramBlocksRequest req)
        {
            var log = new List<string>();
            var arguments = new List<string> { "downloadProgramBlocks",
                "--force", "--project", req.Project, "--station", req.Station, "--module", req.Module, "--program", req.Program };
            if (req.Overwrite) arguments.Add("--overwrite");
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> DownloadProgramBlocks(DownloadProgramBlocksRequest req, ServerCallContext context)
        {
            return Task.FromResult(DownloadProgramBlocksImpl(req));
        }
    }

    class Program
    {
        static int ServerPort;
        static string S7CliPath;
        static bool S7CliVerbose;

        /// <summary>
        /// Reads variables from App.config into static class variables
        /// </summary>
        /// <returns>0 on success, 1 otherwise</returns>
        public static int ReadAppConfig()
        {
            var serverPort = ConfigurationManager.AppSettings["ServerPort"];
            S7CliPath = ConfigurationManager.AppSettings["S7CliPath"];
            var s7CliVerbose = ConfigurationManager.AppSettings["S7CliVerbose"];
            if (serverPort == null || S7CliPath == null || s7CliVerbose == null)
            {
                Console.WriteLine("[ERR] Invalid Server.config:\nMissing keys.");
                return 1;
            }
            if (!File.Exists(S7CliPath))
            {
                Console.WriteLine("[ERR] Invalid Server.config:\n" +
                    $"Could not find path to S7Cli.exe {S7CliPath}");
                return 1;
            }

            try
            {
                Program.ServerPort = int.Parse(serverPort);
                S7CliVerbose = bool.Parse(s7CliVerbose);
            }
            catch (Exception exc)
            {
                Console.WriteLine($"[ERR] Invalid Server.config:\n{exc}");
                return 1;
            }
            return 0;
        }

        public static void Main()
        {
            if (ReadAppConfig() != 0) return;

            Server server = new Server
            {
                Services = { Step7.BindService(new Step7Impl(S7CliPath, S7CliVerbose)) },
                Ports = { new ServerPort("localhost", ServerPort, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine($"Step7 server listening on port {ServerPort}");

            try
            {
                Console.Read();
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Step7 server closing: {exc}");
            }
            server.ShutdownAsync().Wait();
        }
    }
}
