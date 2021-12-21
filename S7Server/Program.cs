using System;
using System.Configuration;

using Grpc.Core;

using S7Service;

namespace S7Server
{
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
                Services = { Step7.BindService(new S7Impl()) },
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
