using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PortTunneler.Client
{
    internal static class Program
    {
        public static TcpClient ConnectionClient;
        
        private static async Task Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: PortTunneler server ip [port]");
                return;
            }

            var server = IPEndPoint.Parse(args[0]);
            if (server.Port == 0) server.Port = 2020;
            var ip = IPEndPoint.Parse(args[1]);
            int port;
            if (args.Length == 2)
            {
                port = ip.Port;
                if (port == 0)
                {
                    Console.WriteLine("A port needs to be specified in the IP or as an argument");
                    return;
                }
            }
            else
                port = int.Parse(args[2]);

            try
            {
                ConnectionClient = new TcpClient(AddressFamily.InterNetworkV6)
                {
                    Client =
                    {
                        DualMode = true
                    }
                };
                ConnectionClient.Connect(server);
                var stream = ConnectionClient.GetStream();
                var writer = new StreamWriter(stream);
                var reader = new StreamReader(stream);
                await writer.WriteLineAsync(port.ToString());
                writer.Flush();
                var response = await reader.ReadLineAsync();
                switch (response)
                {
                    case "W":
                        Console.WriteLine(
                            "Warning: You are using a port which had been used before, if it was not you who previously used the port, please stop and refrain from using it in the future.");
                        break;
                    case "E":
                        throw new Exception("Port is already in use.");
                    default:
                    {
                        if (response != "I")
                            throw new Exception(
                                "Failed to get success status from server. Assuming connection failure.");
                        Console.WriteLine("Connected.");
                        break;
                    }
                }
                
                //handle traffic between client and server
                
                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }
}
