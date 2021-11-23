using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Configuration;

using Serilog;
using Serilog.Core;
using Serilog.Sinks.ListOfString;
using Serilog.Events;
using Grpc.Core;
using Google.Protobuf;

using S7Service;
using S7Lib;


namespace Step7Server
{
    class Step7Impl : Step7.Step7Base
    {
        private readonly Logger Log;
        private readonly bool S7CliVerbose;

        // TODO Block creation of S7 Handle with semaphore?
        //static SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="s7CliPath">Path to S7Cli.exe</param>
        /// <param name="s7CliVerbose">Whether to call S7Cli with verbose flag</param>
        public Step7Impl(bool s7CliVerbose)
        {
            S7CliVerbose = s7CliVerbose;
            Log = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        }

        private S7Handle CreateS7Handle(ref List<string> log)
        {
            var logLevel = new LoggingLevelSwitch();
            var logger = new LoggerConfiguration().MinimumLevel.ControlledBy(logLevel).WriteTo.StringList(log).CreateLogger();
            logLevel.MinimumLevel = S7CliVerbose ? LogEventLevel.Verbose : LogEventLevel.Information;
            var api = new S7Handle(log: logger);
            return api;
        }

        /// <summary>
        /// Creates a reply message with exit code and logs
        /// </summary>
        /// <param name="success">Whether command was successful</param>
        /// <param name="log">Command logs</param>
        /// <returns></returns>
        private StatusReply CreateStatusReply(bool success, List<string> log)
        {
            var reply = new StatusReply { ExitCode = success ? 0 : 1 };
            reply.Log.AddRange(log);
            return reply;
        }

        /// <summary>
        /// Creates a reply message with exit code, logs and the return value of List* commands
        /// </summary>
        /// <param name="success">Whether command was successful</param>
        /// <param name="log">Command logs</param>
        /// <param name="list">Return value of List* command</param>
        /// <returns></returns>
        private ListReply CreateListReply(bool success, List<string> log, List<string> list)
        {
            var reply = new ListReply { Status = CreateStatusReply(success, log) };
            reply.Items.AddRange(list);
            return reply;
        }

        // Server commands

        private IMessage RunCommand(S7Handle s7Handle, List<string> log, IMessage request)
        {
            Log.Debug($"Running command {request}");

            switch (request)
            {
                case ListProjectsRequest req:
                    return ListProjectsImpl(s7Handle, log, req);
                case ListProgramsRequest req:
                    return ListProgramsImpl(s7Handle, log, req);
                case ListContainersRequest req:
                    return ListContainersImpl(s7Handle, log, req);
                case ListStationsRequest req:
                    return ListStationsImpl(s7Handle, log, req);

                case CreateProjectRequest req:
                    return CreateProjectImpl(s7Handle, log, req);
                case CreateLibraryRequest req:
                    return CreateLibraryImpl(s7Handle, log, req);
                case RegisterProjectRequest req:
                    return RegisterProjectImpl(s7Handle, log, req);
                case RemoveProjectRequest req:
                    return RemoveProjectImpl(s7Handle, log, req);

                case ImportSourceRequest req:
                    return ImportSourceImpl(s7Handle, log, req);
                case ImportSourcesDirRequest req:
                    return ImportSourcesDirImpl(s7Handle, log, req);
                case ImportLibSourcesRequest req:
                    return ImportLibSourcesImpl(s7Handle, log, req);
                case ExportSourceRequest req:
                    return ExportSourceImpl(s7Handle, log, req);
                case ExportAllSourcesRequest req:
                    return ExportAllSourcesImpl(s7Handle, log, req);

                case CompileSourceRequest req:
                    return CompileSourceImpl(s7Handle, log, req);
                case CompileSourcesRequest req:
                    return CompileSourcesImpl(s7Handle, log, req);
                case ImportLibBlocksRequest req:
                    return ImportLibBlocksImpl(s7Handle, log, req);
                case ImportSymbolsRequest req:
                    return ImportSymbolsImpl(s7Handle, log, req);
                case ExportSymbolsRequest req:
                    return ExportSymbolsImpl(s7Handle, log, req);
                case CompileAllStationsRequest req:
                    return CompileAllStationsImpl(s7Handle, log, req);

                case EditModuleRequest req:
                    return EditModuleImpl(s7Handle, log, req);

                case StartProgramRequest req:
                    return StartProgramImpl(s7Handle, log, req);
                case StopProgramRequest req:
                    return StopProgramImpl(s7Handle, log, req);
                case DownloadProgramBlocksRequest req:
                    return DownloadProgramBlocksImpl(s7Handle, log, req);

                default:
                    Log.Error($"Unsupported request {request}");
                    return CreateStatusReply(false, log);
                    //throw new ArgumentException($"Unsupported request {request}", nameof(request));
            }
        }

        private List<IMessage> CompleteRequests(List<IMessage> requests)
        {
            var log = new List<string>();
            var replies = new List<IMessage>();

            using (var handle = CreateS7Handle(ref log))
            {
                foreach (var request in requests)
                {
                    replies.Add(RunCommand(handle, log, request));
                }
                // Clear logs in between commands
                // TODO Flush logs?
                log.Clear();
            }

            return replies;
        }

        private ListReply RunListCommandHelper(IMessage req)
        {
            var requests = new List<IMessage>() { req };
            var replies = CompleteRequests(requests);
            // TODO Check if replies[0] has the correct data type?
            return (ListReply)replies[0];
        }

        private StatusReply RunCommandHelper(IMessage req)
        {
            var requests = new List<IMessage>() { req };
            var replies = CompleteRequests(requests);
            // TODO Check if replies[0] has the correct data type?
            return (StatusReply)replies[0];
        }

        #region Server Commands

        #region List Commands

        private ListReply ListProjectsImpl(S7Handle s7Handle, List<string> log, ListProjectsRequest req)
        {
            bool success = true;
            List<string> projectList = null;
            try { projectList = new List<string>(s7Handle.ListProjects().Values); }
            catch { success = false; }
            return CreateListReply(success, log, projectList);
        }

        public override Task<ListReply> ListProjects(ListProjectsRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunListCommandHelper(req));
        }

        private ListReply ListProgramsImpl(S7Handle s7Handle, List<string> log, ListProgramsRequest req)
        {
            bool success = true;
            List<string> programList = null;
            try { programList = s7Handle.ListPrograms(project: req.Project); }
            catch { success = false; }
            return CreateListReply(success, log, programList);
        }

        public override Task<ListReply> ListPrograms(ListProgramsRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunListCommandHelper(req));
        }

        private ListReply ListContainersImpl(S7Handle s7Handle, List<string> log, ListContainersRequest req)
        {
            bool success = true;
            List<string> programList = null;
            try { programList = s7Handle.ListContainers(project: req.Project); }
            catch { success = false; }
            return CreateListReply(success, log, programList);
        }

        public override Task<ListReply> ListContainers(ListContainersRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunListCommandHelper(req));
        }

        private ListReply ListStationsImpl(S7Handle s7Handle, List<string> log, ListStationsRequest req)
        {
            bool success = true;
            List<string> programList = null;
            try { programList = s7Handle.ListStations(project: req.Project); }
            catch { success = false; }
            return CreateListReply(success, log, programList);
        }

        public override Task<ListReply> ListStations(ListStationsRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunListCommandHelper(req));
        }

        #endregion

        #region Project Commands

        private StatusReply CreateProjectImpl(S7Handle s7Handle, List<string> log, CreateProjectRequest req)
        {
            bool success = true;
            try { s7Handle.CreateProject(req.ProjectName, req.ProjectDir); }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> CreateProject(CreateProjectRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        private StatusReply CreateLibraryImpl(S7Handle s7Handle, List<string> log, CreateLibraryRequest req)
        {
            bool success = true;
            try { s7Handle.CreateLibrary(projectName: req.ProjectName, projectDir: req.ProjectDir); }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> CreateLibrary(CreateLibraryRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        private StatusReply RegisterProjectImpl(S7Handle s7Handle, List<string> log, RegisterProjectRequest req)
        {
            bool success = true;
            try { s7Handle.RegisterProject(projectFilePath: req.ProjectFilePath); }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> RegisterProject(RegisterProjectRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        private StatusReply RemoveProjectImpl(S7Handle s7Handle, List<string> log, RemoveProjectRequest req)
        {
            bool success = true;
            try { s7Handle.RemoveProject(project: req.Project); }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> RemoveProject(RemoveProjectRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        #endregion

        #region Import/Export Commands

        private StatusReply ImportSourceImpl(S7Handle s7Handle, List<string> log, ImportSourceRequest req)
        {
            bool success = true;
            try
            {
                s7Handle.ImportSource(project: req.Project, program: req.Program, source: req.Source,
                                      overwrite: req.Overwrite);
            }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> ImportSource(ImportSourceRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        private StatusReply ImportSourcesDirImpl(S7Handle s7Handle, List<string> log, ImportSourcesDirRequest req)
        {
            bool success = true;
            try
            {
                s7Handle.ImportSourcesDir(project: req.Project, program: req.Program, sourcesDir: req.SourcesDir,
                                          overwrite: req.Overwrite);
            }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> ImportSourcesDir(ImportSourcesDirRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        private StatusReply ExportSourceImpl(S7Handle s7Handle, List<string> log, ExportSourceRequest req)
        {
            bool success = true;
            try
            {
                s7Handle.ExportSource(project: req.Project, program: req.Program, source: req.Source,
                                      sourcesDir: req.SourcesDir);
            }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> ExportSource(ExportSourceRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        private StatusReply ExportAllSourcesImpl(S7Handle s7Handle, List<string> log, ExportAllSourcesRequest req)
        {
            bool success = true;
            try { s7Handle.ExportAllSources(project: req.Project, program: req.Program, sourcesDir: req.SourcesDir); }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> ExportAllSources(ExportAllSourcesRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        private StatusReply ImportLibSourcesImpl(S7Handle s7Handle, List<string> log, ImportLibSourcesRequest req)
        {
            bool success = true;
            try
            {
                s7Handle.ImportLibSources(library: req.Library, libProgram: req.LibProgram, project: req.Project,
                                          projProgram: req.ProjProgram, overwrite: req.Overwrite);
            }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> ImportLibSources(ImportLibSourcesRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        private StatusReply ImportLibBlocksImpl(S7Handle s7Handle, List<string> log, ImportLibBlocksRequest req)
        {
            bool success = true;
            try
            {
                s7Handle.ImportLibBlocks(library: req.Library, libProgram: req.LibProgram, project: req.Project,
                                         projProgram: req.ProjProgram, overwrite: req.Overwrite);
            }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> ImportLibBlocks(ImportLibBlocksRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        // TODO - Refactor? perhaps it makes no sense to encode/decode enum to 2 flags
        private void GetSymbolImportFlag(ImportSymbolsRequest.Types.SymbolImportFlag flag,
                                         out bool overwrite, out bool nameLeading)
        {
            switch (flag)
            {
                case (ImportSymbolsRequest.Types.SymbolImportFlag.Insert):
                    overwrite = false; nameLeading = false; break;
                case (ImportSymbolsRequest.Types.SymbolImportFlag.OverwriteAddress):
                    overwrite = true; nameLeading = true; break;
                case (ImportSymbolsRequest.Types.SymbolImportFlag.OverwriteName):
                    overwrite = true; nameLeading = false; break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag));
            }
        }

        private StatusReply ImportSymbolsImpl(S7Handle s7Handle, List<string> log, ImportSymbolsRequest req)
        {
            bool success = true;
            try
            {
                GetSymbolImportFlag(req.Flag, out bool overwrite, out bool nameLeading);
                s7Handle.ImportSymbols(project: req.Project, programPath: req.ProgramPath, symbolFile: req.SymbolFile,
                                       overwrite: overwrite, nameLeading: nameLeading, allowConflicts: req.AllowConflicts);
            }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> ImportSymbols(ImportSymbolsRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        private StatusReply ExportSymbolsImpl(S7Handle s7Handle, List<string> log, ExportSymbolsRequest req)
        {
            bool success = true;
            try
            {
                s7Handle.ExportSymbols(project: req.Project, programPath: req.ProgramPath, symbolFile: req.SymbolFile,
                                       overwrite: req.Overwrite);
            }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> ExportSymbols(ExportSymbolsRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        #endregion

        #region Build Commands

        private StatusReply CompileSourceImpl(S7Handle s7Handle, List<string> log, CompileSourceRequest req)
        {
            bool success = true;
            try
            {
                s7Handle.CompileSource(project: req.Project, program: req.Program, sourceName: req.Source);
            }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> CompileSource(CompileSourceRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        private StatusReply CompileSourcesImpl(S7Handle s7Handle, List<string> log, CompileSourcesRequest req)
        {
            bool success = true;
            try
            {
                s7Handle.CompileSources(project: req.Project, program: req.Program,
                                        sources: new List<string>(req.Sources));
            }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> CompileSources(CompileSourcesRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        private StatusReply CompileAllStationsImpl(S7Handle s7Handle, List<string> log, CompileAllStationsRequest req)
        {
            bool success = true;
            try { s7Handle.CompileAllStations(project: req.Project, allowFail: req.AllowFail); }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> CompileAllStations(CompileAllStationsRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        #endregion

        #region Other Commands

        // Produces module properties dictionary from Dictionary<string, string>
        private Dictionary<string, object> ParseModuleProperties(Dictionary<string, string> input)
        {
            var output = new Dictionary<string, object>();
            var stringFields = new List<string>() { "IPAddress", "SubnetMask", "RouterAddress", "MACAddress" };
            var booleanFields = new List<string>() { "IPActive", "RouterActive" };

            foreach (var stringField in stringFields)
                if (input.ContainsKey(stringField))
                    output.Add(stringField, input[stringField]);

            foreach (var booleanField in booleanFields)
                if (input.ContainsKey(booleanField))
                    output.Add(booleanField, bool.Parse(booleanField));

            return output;
        }

        private StatusReply EditModuleImpl(S7Handle s7Handle, List<string> log, EditModuleRequest req)
        {
            bool success = true;
            try
            {
                var moduleProperties = ParseModuleProperties(new Dictionary<string, string>(req.Properties));
                s7Handle.EditModule(project: req.Project, station: req.Station, rack: req.Station,
                                    modulePath: req.Module, properties: moduleProperties);
            }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> EditModule(EditModuleRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        #endregion

        #region Online Commands

        // TODO - Implement some protection? Maybe using context object?

        private StatusReply StartProgramImpl(S7Handle s7Handle, List<string> log, StartProgramRequest req)
        {
            bool success = true;
            try
            {
                s7Handle.StartProgram(project: req.Project, station: req.Station,
                                      module: req.Module, program: req.Program);
            }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> StartProgram(StartProgramRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        private StatusReply StopProgramImpl(S7Handle s7Handle, List<string> log, StopProgramRequest req)
        {
            bool success = true;
            try
            {
                s7Handle.StopProgram(project: req.Project, station: req.Station,
                                     module: req.Module, program: req.Program);
            }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> StopProgram(StopProgramRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        private StatusReply DownloadProgramBlocksImpl(S7Handle s7Handle, List<string> log, DownloadProgramBlocksRequest req)
        {
            bool success = true;
            try
            {
                s7Handle.DownloadProgramBlocks(project: req.Project, station: req.Station, module: req.Module,
                                               program: req.Program, overwrite: req.Overwrite);
            }
            catch { success = false; }
            return CreateStatusReply(success, log);
        }

        public override Task<StatusReply> DownloadProgramBlocks(DownloadProgramBlocksRequest req, ServerCallContext context)
        {
            return Task.FromResult(RunCommandHelper(req));
        }

        #endregion

        #endregion
    }

    class Program
    {
        static int ServerPort;
        static bool S7CliVerbose;

        /// <summary>
        /// Reads variables from App.config into static class variables
        /// </summary>
        /// <returns>0 on success, 1 otherwise</returns>
        public static int ReadAppConfig()
        {
            try
            {
                ServerPort = int.Parse(ConfigurationManager.AppSettings["ServerPort"]);
                S7CliVerbose = bool.Parse(ConfigurationManager.AppSettings["S7CliVerbose"]);
            }
            catch (Exception exc)
            {
                throw new Exception("Invalid Server.exe.config file: expected ServerPort (int) and S7CliVerbose (bool).", exc);
            }
            return 0;
        }

        public static void Main()
        {
            ReadAppConfig();
            Server server = new Server
            {
                Services = { Step7.BindService(new Step7Impl(S7CliVerbose)) },
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
