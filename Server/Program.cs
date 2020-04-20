using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Serilog;
using Serilog.Sinks.ListOfString;
using System.Configuration;
using System.Diagnostics;
using System.Threading;

using Grpc.Core;
using S7Service;

using S7Lib;
using System.ComponentModel;

namespace Step7Server
{
    class Step7Impl : Step7.Step7Base
    {
        private string S7CliPath;
        private bool S7CliVerbose;
        static SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="s7CliPath">Path to S7Cli.exe</param>
        /// <param name="s7CliVerbose">Whether to call S7Cli with verbose flag</param>
        public Step7Impl(string s7CliPath, bool s7CliVerbose)
        {
            S7CliPath = s7CliPath;
            S7CliVerbose = s7CliVerbose;
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
        private int LaunchS7Cli(ref List<string> log, string arguments, int timeout = 5*60)
        {
            var exitCode = -1;
            var flags = (S7CliVerbose? "-v" : "");
            Semaphore.Wait(millisecondsTimeout: timeout * 1000);
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = S7CliPath,
                    Arguments = $"{arguments} {flags}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                var process = Process.Start(processStartInfo);
                while (!process.StandardOutput.EndOfStream)
                    log.Add(process.StandardOutput.ReadLine());
                process.WaitForExit();
                exitCode = process.ExitCode;
            }
            finally
            {
                Semaphore.Release();
            }
            return exitCode;
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
            var arguments = $"createProject --name {req.ProjectName} --dir {req.ProjectDir}";
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
            var arguments = $"createLibrary --name {req.ProjectName} --dir {req.ProjectDir}";
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
            var arguments = $"registerProject --projectFilePath {req.ProjectFilePath}";
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
            var arguments = $"removeProject --force --project {req.Project}";
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
            var arguments = $"importSource " +
                $"--project {req.Project} --program {req.Program} --source {req.Source}";
            if (req.Overwrite) arguments += " --overwrite";
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
            var arguments = $"importSourcesDir " +
                $"--project {req.Project} --program {req.Program} --sourcesDir {req.SourcesDir}";
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
            var arguments = $"exportAllSources " +
                $"--project {req.Project} --program {req.Program} --sourcesDir {req.SourcesDir}";
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
            var arguments = $"importLibSources " +
                $"--project {req.Project} --library {req.Library} --projProgram {req.ProjProgram} --libProgram {req.LibProgram}";
            if (req.Overwrite) arguments += " --overwrite";
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
            var arguments = $"importLibBlocks " +
                $"--project {req.Project} --library {req.Library} --projProgram {req.ProjProgram} --libProgram {req.LibProgram}";
            if (req.Overwrite) arguments += " --overwrite";
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
            var arguments = $"importSymbols " +
                $"--project {req.Project} --program {req.Program} --symbolFile {req.SymbolFile} --flag {req.Flag}";
            if (req.AllowConflicts) arguments += " --allowConflicts";
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
            var arguments = $"exportSymbols " +
                $"--project {req.Project} --program {req.Program} --symbolFile {req.SymbolFile}";
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
            var arguments = $"compileSource " +
                $"--project {req.Project} --program {req.Program} --source {req.Source}";
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
            var arguments = $"compileSources " +
                $"--project {req.Project} --program {req.Program} --source {string.Join(",", sources)}";
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
            var arguments = $"compileAllStations --project {req.Project}";
            if (req.AllowFail) arguments += " --allowFail";
            var rv = LaunchS7Cli(ref log, arguments);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> CompileAllStations(CompileAllStationsRequest req, ServerCallContext context)
        {
            return Task.FromResult(CompileAllStationsImpl(req));
        }

        // Online commands

        private StatusReply StartProgramImpl(ProgramRequest req)
        {
            var log = new List<string>();
            var arguments = $"startProgram --force" +
                $"--project {req.Project} --station {req.Station} --rack {req.Rack} --module {req.Module}";
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
            var arguments = $"stopProgram --force" +
                $"--project {req.Project} --station {req.Station} --rack {req.Rack} --module {req.Module}";
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
            var arguments = $"downloadProgramBlocks --force" +
                $"--project {req.Project} --station {req.Station} --rack {req.Rack} --module {req.Module}";
            if (req.Overwrite) arguments += " --overwrite";
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
        public static void ReadAppConfig()
        {
            var serverPort = ConfigurationManager.AppSettings["ServerPort"];
            S7CliPath = ConfigurationManager.AppSettings["S7CliPath"];
            var s7CliVerbose = ConfigurationManager.AppSettings["S7CliVerbose"];
            if (serverPort == null || S7CliPath == null || s7CliVerbose == null)
            {
                throw new Exception("Configuration error in App.config: missing keys.");
            }
            try
            {
                Program.ServerPort = int.Parse(serverPort);
                S7CliVerbose = bool.Parse(s7CliVerbose);
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Error reading App.config: {exc}");
                throw;
            }
        }

        public static void Main()
        {
            ReadAppConfig();

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
