using System;
using System.Threading.Tasks;
using Grpc.Core;
using Step7Service;

// TODO: Partition S7Cli in S7Lib and S7CLi and then remove dependency in S7Cli
// TODO: Refactor S7CommandStatus so that it;s a regular enum and an attribute in S7Command

namespace Step7Server
{
    class Step7Impl : Step7.Step7Base
    {
        // Command implementation

        private StatusReply CreateProjectImpl(CreateProjectRequest req)
        {
            var command = new S7_cli.S7Command();
            command.createProject(req.ProjectName, req.ProjectDir);
            return new StatusReply { ExitCode = S7_cli.S7CommandStatus.get_status() };
        }

        private StatusReply RegisterProjectImpl(RegisterProjectRequest req)
        {
            var command = new S7_cli.S7Command();
            command.registerProject(req.ProjectFilePath);
            return new StatusReply { ExitCode = S7_cli.S7CommandStatus.get_status() };
        }

        private StatusReply ImportLibSourcesImpl(ImportLibRequest req)
        {
            var command = new S7_cli.S7Command();
            command.importLibSources(req.Project, req.LibraryName, req.LibraryProgram, req.Program, req.Force);
            return new StatusReply { ExitCode = S7_cli.S7CommandStatus.get_status() };
        }

        private StatusReply ImportLibBlocksImpl(ImportLibRequest req)
        {
            var command = new S7_cli.S7Command();
            command.importLibBlocks(req.Project, req.LibraryName, req.LibraryProgram, req.Program, req.Force);
            return new StatusReply { ExitCode = S7_cli.S7CommandStatus.get_status() };
        }

        private StatusReply ImportSymbolsImpl(ImportSymbolsRequest req)
        {
            var command = new S7_cli.S7Command();
            command.importSymbols(req.Project, req.Symbols, req.Program);
            return new StatusReply { ExitCode = S7_cli.S7CommandStatus.get_status() };
        }

        private StatusReply ImportSourcesDirImpl(ImportSourcesDirRequest req)
        {
            var command = new S7_cli.S7Command();
            command.importSourcesDir(req.Project, req.Program, req.SourcesDir, req.Force);
            return new StatusReply { ExitCode = S7_cli.S7CommandStatus.get_status() };
        }

        private StatusReply CompileSourcesImpl(CompileSourcesRequest req)
        {
            var sources = new string[req.Sources.Count];
            req.Sources.CopyTo(sources, 0);
            var command = new S7_cli.S7Command();
            command.compileSources(req.Project, req.Program, sources);
            return new StatusReply { ExitCode = S7_cli.S7CommandStatus.get_status() };
        }

        private StatusReply RemoveBlocksImpl(RemoveBlocksRequest req)
        {
            var blocks = new string[req.Blocks.Count];
            req.Blocks.CopyTo(blocks, 0);
            var command = new S7_cli.S7Command();
            command.removeBlocks(req.Project, req.Program, blocks);
            return new StatusReply { ExitCode = S7_cli.S7CommandStatus.get_status() };
        }

        private StatusReply CompileHwImpl(CompileHwRequest req)
        {
            var command = new S7_cli.S7Command();
            command.compileAllStations(req.Project);
            return new StatusReply { ExitCode = S7_cli.S7CommandStatus.get_status() };
        }

        private StatusReply DownloadImpl(DownloadRequest req)
        {
            var command = new S7_cli.S7Command();
            command.downloadStation(projectPathOrName: req.Project, stationName: "", stationType: "", allStations: true, force: true);
            return new StatusReply { ExitCode = S7_cli.S7CommandStatus.get_status() };
        }

        // Public command interface using Tasks

        public override Task<StatusReply> CreateProject(CreateProjectRequest req, ServerCallContext context)
        {
            return Task.FromResult(CreateProjectImpl(req));
        }

        public override Task<StatusReply> RegisterProject(RegisterProjectRequest req, ServerCallContext context)
        {
            return Task.FromResult(RegisterProjectImpl(req));
        }

        public override Task<StatusReply> ImportLibSources(ImportLibRequest req, ServerCallContext context)
        {
            return Task.FromResult(ImportLibSourcesImpl(req));
        }

        public override Task<StatusReply> ImportLibBlocks(ImportLibRequest req, ServerCallContext context)
        {
            return Task.FromResult(ImportLibBlocksImpl(req));
        }

        public override Task<StatusReply> ImportSymbols(ImportSymbolsRequest req, ServerCallContext context)
        {
            return Task.FromResult(ImportSymbolsImpl(req));
        }

        public override Task<StatusReply> ImportSourcesDir(ImportSourcesDirRequest req, ServerCallContext context)
        {
            return Task.FromResult(ImportSourcesDirImpl(req));
        }

        public override Task<StatusReply> CompileSources(CompileSourcesRequest req, ServerCallContext context)
        {
            return Task.FromResult(CompileSourcesImpl(req));
        }

        public override Task<StatusReply> RemoveBlocks(RemoveBlocksRequest req, ServerCallContext context)
        {
            return Task.FromResult(RemoveBlocksImpl(req));
        }

        public override Task<StatusReply> CompileHw(CompileHwRequest req, ServerCallContext context)
        {
            return Task.FromResult(CompileHwImpl(req));
        }

        public override Task<StatusReply> Download(DownloadRequest req, ServerCallContext context)
        {
            return Task.FromResult(DownloadImpl(req));
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

            // TODO: Fix infinite loop
            try
            {
                while (true) { Console.Read(); }
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Step7 server closing: {exc}");
            }
            server.ShutdownAsync().Wait();
        }
    }
}
