using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Serilog;
using Serilog.Sinks.ListOfString;

using Grpc.Core;
using Step7Service;

using S7Lib;


namespace Step7Server
{
    class Step7Impl : Step7.Step7Base
    {
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
            var ctx = CreateApiContext(ref log);
            var rv = Api.CreateProject(ctx, req.ProjectName, req.ProjectDir);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> CreateProject(CreateProjectRequest req, ServerCallContext context)
        {
            return Task.FromResult(CreateProjectImpl(req));
        }

        private StatusReply CreateLibraryImpl(CreateLibraryRequest req)
        {
            var log = new List<string>();
            var ctx = CreateApiContext(ref log);
            var rv = Api.CreateLibrary(ctx, req.ProjectName, req.ProjectDir);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> CreateLibrary(CreateLibraryRequest req, ServerCallContext context)
        {
            return Task.FromResult(CreateLibraryImpl(req));
        }

        private StatusReply RegisterProjectImpl(RegisterProjectRequest req)
        {
            var log = new List<string>();
            var ctx = CreateApiContext(ref log);
            var rv = Api.RegisterProject(ctx, req.ProjectFilePath);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> RegisterProject(RegisterProjectRequest req, ServerCallContext context)
        {
            return Task.FromResult(RegisterProjectImpl(req));
        }

        private StatusReply RemoveProjectImpl(RemoveProjectRequest req)
        {
            var log = new List<string>();
            var ctx = CreateApiContext(ref log);
            var rv = Api.RemoveProject(ctx, req.Project);
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
            var ctx = CreateApiContext(ref log);
            var rv = Api.ImportSource(ctx, req.Project, req.Program, req.Source, req.Overwrite);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> ImportSource(ImportSourceRequest req, ServerCallContext context)
        {
            return Task.FromResult(ImportSourceImpl(req));
        }

        private StatusReply ImportSourcesDirImpl(ImportSourcesDirRequest req)
        {
            var log = new List<string>();
            var ctx = CreateApiContext(ref log);
            var rv = Api.ImportSourcesDir(ctx, req.Project, req.Program, req.SourcesDir, req.Overwrite);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> ImportSourcesDir(ImportSourcesDirRequest req, ServerCallContext context)
        {
            return Task.FromResult(ImportSourcesDirImpl(req));
        }

        private StatusReply ExportAllSourcesImpl(ExportAllSourcesRequest req)
        {
            var log = new List<string>();
            var ctx = CreateApiContext(ref log);
            var rv = Api.ExportAllSources(ctx, req.Project, req.Program, req.SourcesDir);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> ExportAllSources(ExportAllSourcesRequest req, ServerCallContext context)
        {
            return Task.FromResult(ExportAllSourcesImpl(req));
        }

        private StatusReply ImportLibSourcesImpl(ImportLibSourcesRequest req)
        {
            var log = new List<string>();
            var ctx = CreateApiContext(ref log);
            var rv = Api.ImportLibSources(ctx,
                library: req.Library, libProgram: req.LibProgram,
                project: req.Project, projProgram: req.ProjProgram, req.Overwrite);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> ImportLibSources(ImportLibSourcesRequest req, ServerCallContext context)
        {
            return Task.FromResult(ImportLibSourcesImpl(req));
        }

        private StatusReply ImportLibBlocksImpl(ImportLibBlocksRequest req)
        {
            var log = new List<string>();
            var ctx = CreateApiContext(ref log);
            var rv = Api.ImportLibBlocks(ctx,
                library: req.Library, libProgram: req.LibProgram,
                project: req.Project, projProgram: req.ProjProgram, req.Overwrite);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> ImportLibBlocks(ImportLibBlocksRequest req, ServerCallContext context)
        {
            return Task.FromResult(ImportLibBlocksImpl(req));
        }

        private StatusReply ImportSymbolsImpl(ImportSymbolsRequest req)
        {
            var log = new List<string>();
            var ctx = CreateApiContext(ref log);
            var rv = Api.ImportSymbols(ctx, req.Project, req.Program, req.SymbolFile, req.Flag, req.AllowConflicts);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> ImportSymbols(ImportSymbolsRequest req, ServerCallContext context)
        {
            return Task.FromResult(ImportSymbolsImpl(req));
        }

        private StatusReply ExportSymbolsImpl(ExportSymbolsRequest req)
        {
            var log = new List<string>();
            var ctx = CreateApiContext(ref log);
            var rv = Api.ExportSymbols(ctx, req.Project, req.Program, req.SymbolFile);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> ExportSymbols(ExportSymbolsRequest req, ServerCallContext context)
        {
            return Task.FromResult(ExportSymbolsImpl(req));
        }

        private StatusReply CompileSourceImpl(CompileSourceRequest req)
        {
            var log = new List<string>();
            var ctx = CreateApiContext(ref log);
            var rv = Api.CompileSource(ctx, req.Project, req.Program, req.Source);
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
            var ctx = CreateApiContext(ref log);
            var rv = Api.CompileSources(ctx, req.Project, req.Program, sources);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> CompileSources(CompileSourcesRequest req, ServerCallContext context)
        {
            return Task.FromResult(CompileSourcesImpl(req));
        }

        private StatusReply CompileAllStationsImpl(CompileAllStationsRequest req)
        {
            var log = new List<string>();
            var ctx = CreateApiContext(ref log);
            var rv = Api.CompileAllStations(ctx, req.Project, req.AllowFail);
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
            var ctx = CreateApiContext(ref log);
            var rv = Online.StartProgram(ctx, req.Project, req.Station, req.Rack, req.Module);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> StartProgram(ProgramRequest req, ServerCallContext context)
        {
            return Task.FromResult(StartProgramImpl(req));
        }

        private StatusReply StopProgramImpl(ProgramRequest req)
        {
            var log = new List<string>();
            var ctx = CreateApiContext(ref log);
            var rv = Online.StopProgram(ctx, req.Project, req.Station, req.Rack, req.Module);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> StopProgram(ProgramRequest req, ServerCallContext context)
        {
            return Task.FromResult(StopProgramImpl(req));
        }

        private StatusReply DownloadProgramBlocksImpl(DownloadProgramBlocksRequest req)
        {
            var log = new List<string>();
            var ctx = CreateApiContext(ref log);
            var rv = Online.DownloadProgramBlocks(ctx, req.Project, req.Station, req.Rack, req.Module, req.Overwrite);
            return CreateStatusReply(rv, ref log);
        }

        public override Task<StatusReply> DownloadProgramBlocks(DownloadProgramBlocksRequest req, ServerCallContext context)
        {
            return Task.FromResult(DownloadProgramBlocksImpl(req));
        }
    }

    class Program
    {
        const int Port = 50051;

        public static void Main(string[] args)
        {
            Server server = new Server
            {
                Services = { Step7.BindService(new Step7Impl()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("Step7 server listening on port " + Port);
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
