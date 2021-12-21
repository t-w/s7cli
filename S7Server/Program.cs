using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Configuration;

using Serilog;
using Serilog.Sinks.ListOfString;
using Grpc.Core;
using Google.Protobuf;

using S7Service;
using S7Lib;

namespace Step7Server
{
    class Step7Impl : Step7.Step7Base
    {
        private readonly ILogger Logger;

        // TODO Block creation of S7 Handle with semaphore?
        //static SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Constructor
        /// </summary>
        public Step7Impl()
        {
            // TODO Configure server minimum log level
            Logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Debug().CreateLogger();
        }

        // Creates logger for messages to be sent back to the client
        private void CreateClientLogger(out ILogger logger, out List<string> messages)
        {
            messages = new List<string>();
            // TODO Configure client minimum log level
            logger = new LoggerConfiguration().WriteTo.StringList(messages).MinimumLevel.Debug().CreateLogger();
        }

        private S7Handle CreateS7Handle(ILogger logger)
        {
            return new S7Handle(logger: logger);
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
            if (log != null) reply.Log.AddRange(log);
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
            if (list != null) reply.Items.AddRange(list);
            return reply;
        }

        // Server commands

        private void RunCommand(IS7Handle s7Handle, IMessage request)
        {
            Logger.Debug($"Running command {request.GetType()} {request}");

            switch (request)
            {
                case CreateProjectRequest req:
                    CreateProjectImpl(s7Handle, req);
                    break;
                case CreateLibraryRequest req:
                    CreateLibraryImpl(s7Handle, req);
                    break;
                case RegisterProjectRequest req:
                    RegisterProjectImpl(s7Handle, req);
                    break;
                case RemoveProjectRequest req:
                    RemoveProjectImpl(s7Handle, req);
                    break;

                case ImportSourceRequest req:
                    ImportSourceImpl(s7Handle, req);
                    break;
                case ImportSourcesDirRequest req:
                    ImportSourcesDirImpl(s7Handle, req);
                    break;
                case ImportLibSourcesRequest req:
                    ImportLibSourcesImpl(s7Handle, req);
                    break;
                case ExportSourceRequest req:
                    ExportSourceImpl(s7Handle, req);
                    break;
                case ExportAllSourcesRequest req:
                    ExportAllSourcesImpl(s7Handle, req);
                    break;

                case CompileSourceRequest req:
                    CompileSourceImpl(s7Handle, req);
                    break;
                case CompileSourcesRequest req:
                    CompileSourcesImpl(s7Handle, req);
                    break;
                case ImportLibBlocksRequest req:
                    ImportLibBlocksImpl(s7Handle, req);
                    break;
                case ImportSymbolsRequest req:
                    ImportSymbolsImpl(s7Handle, req);
                    break;
                case ExportSymbolsRequest req:
                    ExportSymbolsImpl(s7Handle, req);
                    break;
                case CompileAllStationsRequest req:
                    CompileAllStationsImpl(s7Handle, req);
                    break;

                case EditModuleRequest req:
                    EditModuleImpl(s7Handle, req);
                    break;

                case StartProgramRequest req:
                    StartProgramImpl(s7Handle, req);
                    break;
                case StopProgramRequest req:
                    StopProgramImpl(s7Handle, req);
                    break;
                case DownloadProgramBlocksRequest req:
                    DownloadProgramBlocksImpl(s7Handle, req);
                    break;

                default:
                    // The output of list commands is discarded when running a batch of commands.
                    // However a list command also produces output to the clientLogger.
                    // If it's not a valid list command, throw and argument exception.
                    RunListCommand(s7Handle, request);
                    break;
            }
        }

        private List<string> RunListCommand(IS7Handle s7Handle, IMessage listRequest)
        {
            switch (listRequest)
            {
                case ListProjectsRequest req:
                    return ListProjectsImpl(s7Handle, req);
                case ListProgramsRequest req:
                    return ListProgramsImpl(s7Handle, req);
                case ListContainersRequest req:
                    return ListContainersImpl(s7Handle, req);
                case ListStationsRequest req:
                    return ListStationsImpl(s7Handle, req);
                default:
                    throw new ArgumentException($"Unsupported request {listRequest}", nameof(listRequest));
            }
        }

        /// <summary>
        /// Handles a single request for list command
        /// </summary>
        /// <param name="listRequest">Request message for list command</param>
        /// <returns>Command result</returns>
        public ListReply HandleListRequest(IMessage listRequest)
        {
            CreateClientLogger(out ILogger clientLogger, out List<string> messages);
            List<string> output = null;
            bool success = true;

            using (var handle = CreateS7Handle(clientLogger))
            {
                try
                {
                    output = RunListCommand(handle, listRequest);
                }
                catch (Exception exc)
                {
                    clientLogger.Error(exc, "Could not complete request {Request}.", listRequest);
                    success = false;
                }
            }
            return CreateListReply(success, messages, output);
        }

        /// <summary>
        /// Handles several requests sequentially
        /// </summary>
        /// <remarks>Creates a single S7Handle which is used for all requests.</remarks>
        /// <param name="requests">List or request messages</param>
        /// <returns>Summary of the execution of the requests</returns>
        public StatusReply HandleRequests(List<IMessage> requests)
        {
            Logger.Debug("Processing {NumRequests} request(s).", requests.Count);

            CreateClientLogger(out ILogger clientLogger, out List<string> messages);
            bool success = true;

            using (var handle = CreateS7Handle(clientLogger))
            {
                foreach (var request in requests)
                {
                    try
                    {
                        RunCommand(handle, request);
                    }
                    catch (Exception exc)
                    {
                        clientLogger.Error(exc, "Could not complete request {Request}.", request);
                        success = false;
                        break;
                    }
                }
            }

            return CreateStatusReply(success, messages);
        }

        #region Server Commands

        #region List Commands

        private List<string> ListProjectsImpl(IS7Handle s7Handle, ListProjectsRequest req)
        {
            return new List<string>(s7Handle.ListProjects().Values);
        }

        public override Task<ListReply> ListProjects(ListProjectsRequest req, ServerCallContext context)
        {
            return Task.FromResult(HandleListRequest(req));
        }

        private List<string> ListProgramsImpl(IS7Handle s7Handle, ListProgramsRequest req)
        {
            return s7Handle.ListPrograms(project: req.Project);
        }

        public override Task<ListReply> ListPrograms(ListProgramsRequest req, ServerCallContext context)
        {
            return Task.FromResult(HandleListRequest(req));
        }

        private List<string> ListContainersImpl(IS7Handle s7Handle, ListContainersRequest req)
        {
            return s7Handle.ListContainers(project: req.Project);
        }

        public override Task<ListReply> ListContainers(ListContainersRequest req, ServerCallContext context)
        {
            return Task.FromResult(HandleListRequest(req));
        }

        private List<string> ListStationsImpl(IS7Handle s7Handle, ListStationsRequest req)
        {
            return s7Handle.ListStations(project: req.Project);
        }

        public override Task<ListReply> ListStations(ListStationsRequest req, ServerCallContext context)
        {
            return Task.FromResult(HandleListRequest(req));
        }

        #endregion

        #region Project Commands

        private void CreateProjectImpl(IS7Handle s7Handle, CreateProjectRequest req)
        {
            s7Handle.CreateProject(req.ProjectName, req.ProjectDir);
        }

        public override Task<StatusReply> CreateProject(CreateProjectRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        private void CreateLibraryImpl(IS7Handle s7Handle, CreateLibraryRequest req)
        {
            s7Handle.CreateLibrary(projectName: req.ProjectName, projectDir: req.ProjectDir);
        }

        public override Task<StatusReply> CreateLibrary(CreateLibraryRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        private void RegisterProjectImpl(IS7Handle s7Handle, RegisterProjectRequest req)
        {
            s7Handle.RegisterProject(projectFilePath: req.ProjectFilePath);
        }

        public override Task<StatusReply> RegisterProject(RegisterProjectRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        private void RemoveProjectImpl(IS7Handle s7Handle, RemoveProjectRequest req)
        {
            s7Handle.RemoveProject(project: req.Project);
        }

        public override Task<StatusReply> RemoveProject(RemoveProjectRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        #endregion

        #region Import/Export Commands

        private void ImportSourceImpl(IS7Handle s7Handle, ImportSourceRequest req)
        {
            s7Handle.ImportSource(project: req.Project, program: req.Program, source: req.Source,
                                  overwrite: req.Overwrite);
        }

        public override Task<StatusReply> ImportSource(ImportSourceRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        private void ImportSourcesDirImpl(IS7Handle s7Handle, ImportSourcesDirRequest req)
        {
            s7Handle.ImportSourcesDir(project: req.Project, program: req.Program, sourcesDir: req.SourcesDir,
                overwrite: req.Overwrite);
        }

        public override Task<StatusReply> ImportSourcesDir(ImportSourcesDirRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        private void ExportSourceImpl(IS7Handle s7Handle, ExportSourceRequest req)
        {
            s7Handle.ExportSource(project: req.Project, program: req.Program, source: req.Source,
                                  sourcesDir: req.SourcesDir);
        }

        public override Task<StatusReply> ExportSource(ExportSourceRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        private void ExportAllSourcesImpl(IS7Handle s7Handle, ExportAllSourcesRequest req)
        {
            s7Handle.ExportAllSources(project: req.Project, program: req.Program, sourcesDir: req.SourcesDir);
        }

        public override Task<StatusReply> ExportAllSources(ExportAllSourcesRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        private void ImportLibSourcesImpl(IS7Handle s7Handle, ImportLibSourcesRequest req)
        {
            s7Handle.ImportLibSources(library: req.Library, libProgram: req.LibProgram, project: req.Project,
                                      projProgram: req.ProjProgram, overwrite: req.Overwrite);
        }

        public override Task<StatusReply> ImportLibSources(ImportLibSourcesRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        private void ImportLibBlocksImpl(IS7Handle s7Handle, ImportLibBlocksRequest req)
        {
            s7Handle.ImportLibBlocks(library: req.Library, libProgram: req.LibProgram, project: req.Project,
                                     projProgram: req.ProjProgram, overwrite: req.Overwrite);
        }

        public override Task<StatusReply> ImportLibBlocks(ImportLibBlocksRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
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

        private void ImportSymbolsImpl(IS7Handle s7Handle, ImportSymbolsRequest req)
        {
            GetSymbolImportFlag(req.Flag, out bool overwrite, out bool nameLeading);
            s7Handle.ImportSymbols(project: req.Project, programPath: req.ProgramPath, symbolFile: req.SymbolFile,
                                   overwrite: overwrite, nameLeading: nameLeading, allowConflicts: req.AllowConflicts);
        }

        public override Task<StatusReply> ImportSymbols(ImportSymbolsRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        private void ExportSymbolsImpl(IS7Handle s7Handle, ExportSymbolsRequest req)
        {
            s7Handle.ExportSymbols(project: req.Project, programPath: req.ProgramPath, symbolFile: req.SymbolFile,
                                   overwrite: req.Overwrite);
        }

        public override Task<StatusReply> ExportSymbols(ExportSymbolsRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        #endregion

        #region Build Commands

        private void CompileSourceImpl(IS7Handle s7Handle, CompileSourceRequest req)
        {
            s7Handle.CompileSource(project: req.Project, program: req.Program, sourceName: req.Source);
        }

        public override Task<StatusReply> CompileSource(CompileSourceRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        private void CompileSourcesImpl(IS7Handle s7Handle, CompileSourcesRequest req)
        {
            s7Handle.CompileSources(project: req.Project, program: req.Program,
                                    sources: new List<string>(req.Sources));
        }

        public override Task<StatusReply> CompileSources(CompileSourcesRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        private void CompileAllStationsImpl(IS7Handle s7Handle, CompileAllStationsRequest req)
        {
            s7Handle.CompileAllStations(project: req.Project, allowFail: req.AllowFail);
        }

        public override Task<StatusReply> CompileAllStations(CompileAllStationsRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
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

        private void EditModuleImpl(IS7Handle s7Handle, EditModuleRequest req)
        {
            var moduleProperties = ParseModuleProperties(new Dictionary<string, string>(req.Properties));
            s7Handle.EditModule(project: req.Project, station: req.Station, rack: req.Station,
                                modulePath: req.Module, properties: moduleProperties);
        }

        public override Task<StatusReply> EditModule(EditModuleRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        #endregion

        #region Online Commands

        // TODO - Implement some protection? Maybe using server call context object?

        private void StartProgramImpl(IS7Handle s7Handle, StartProgramRequest req)
        {
            s7Handle.StartProgram(project: req.Project, station: req.Station,
                                  module: req.Module, program: req.Program);
        }

        public override Task<StatusReply> StartProgram(StartProgramRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        private void StopProgramImpl(IS7Handle s7Handle, StopProgramRequest req)
        {
            s7Handle.StopProgram(project: req.Project, station: req.Station,
                                 module: req.Module, program: req.Program);
        }

        public override Task<StatusReply> StopProgram(StopProgramRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        private void DownloadProgramBlocksImpl(IS7Handle s7Handle, DownloadProgramBlocksRequest req)
        {
            s7Handle.DownloadProgramBlocks(project: req.Project, station: req.Station, module: req.Module,
                program: req.Program, overwrite: req.Overwrite);
        }

        public override Task<StatusReply> DownloadProgramBlocks(DownloadProgramBlocksRequest req, ServerCallContext context)
        {
            var requests = new List<IMessage> { req };
            return Task.FromResult(HandleRequests(requests));
        }

        #endregion

        #endregion
    }

    class Program
    {
        static int ServerPort;

        /// <summary>
        /// Reads variables from App.config into static class variables
        /// </summary>
        public static void ReadAppConfig()
        {
            try
            {
                ServerPort = int.Parse(ConfigurationManager.AppSettings["ServerPort"]);
            }
            catch (Exception exc)
            {
                throw new Exception("Invalid Server.exe.config file: expected ServerPort (int)", exc);
            }
        }

        public static void Main()
        {
            ReadAppConfig();
            Server server = new Server
            {
                Services = { Step7.BindService(new Step7Impl()) },
                Ports = { new ServerPort("localhost", ServerPort, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine($"Listening on port {ServerPort}. " +
                              $"Press Return to close the server.");

            try
            {
                Console.Read();
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Closing: {exc}");
            }
            server.ShutdownAsync().Wait();
        }
    }
}
