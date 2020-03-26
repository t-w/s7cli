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
        // TODO: Command implementation
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
