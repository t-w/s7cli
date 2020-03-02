using System;
using System.Threading.Tasks;
using Grpc.Core;
using Step7Service;

namespace Step7Server
{
    class Step7Impl : Step7.Step7Base
    {
        private StatusReply CreateProjectImpl(CreateProjectRequest req)
        {
            var command = new S7_cli.S7Command();
            command.createProject(req.ProjectName, req.ProjectDir);
            return new StatusReply { ExitCode = 0 };
        }

        public override Task<StatusReply> CreateProject(CreateProjectRequest req, ServerCallContext context)
        {
            return Task.FromResult(CreateProjectImpl(req));
        }

        public override Task<StatusReply> ImportLibSources(ImportLibRequest req, ServerCallContext context)
        {
            var command = new S7_cli.S7Command();
            command.importLibSources(req.Project, req.LibraryName, req.LibraryProgram, req.Program, req.Force);
            return Task.FromResult(new StatusReply { ExitCode = 0 });
        }

        public override Task<StatusReply> ImportLibBlocks(ImportLibRequest req, ServerCallContext context)
        {
            var command = new S7_cli.S7Command();
            command.importLibBlocks(req.Project, req.LibraryName, req.LibraryProgram, req.Program, req.Force);
            return Task.FromResult(new StatusReply { ExitCode = 0 });
        }

        public override Task<StatusReply> ImportSymbols(ImportSymbolsRequest req, ServerCallContext context)
        {
            var command = new S7_cli.S7Command();
            command.importSymbols(req.Project, req.Symbols, req.Program);
            return Task.FromResult(new StatusReply { ExitCode = 0 });
        }

        public override Task<StatusReply> ImportSourcesDir(ImportSourcesDirRequest req, ServerCallContext context)
        {
            var command = new S7_cli.S7Command();
            command.importSourcesDir(req.Project, req.Program, req.SourcesDir, req.Force);
            return Task.FromResult(new StatusReply { ExitCode = 0 });
        }

        public override Task<StatusReply> CompileSources(CompileSourcesRequest req, ServerCallContext context)
        {
            string[] sources = new string[] { };
            req.Sources.CopyTo(sources, 0);
            var command = new S7_cli.S7Command();
            command.compileSources(req.Project, req.Program, sources);
            return Task.FromResult(new StatusReply { ExitCode = 0 });
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
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}
