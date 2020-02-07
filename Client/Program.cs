using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PortTunneler.Client
{
    internal static class Program
    {
        public static IPEndPoint Server;
        private static TcpClient _connectionClient;
        
        public static readonly Dictionary<PortType, Func<IPEndPoint, PortConnection>> ConnectionConstructors =
            new Dictionary<PortType, Func<IPEndPoint, PortConnection>>
            {
                {PortType.Tcp, address => new TcpPortConnection(address)}
            };


        private static async Task Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: PortTunneler server protocol ip [port]");
                return;
            }

            var server = IPEndPoint.Parse(args[0]);
            if (server.Port == 0) server.Port = 2020;
            var ip = IPEndPoint.Parse(args[2]);
            int port;
            if (args.Length == 3)
            {
                port = ip.Port;
                if (port == 0)
                {
                    Console.WriteLine("A port needs to be specified in the IP or as an argument");
                    return;
                }
            }
            else
                port = int.Parse(args[3]);

            Server = server;
            try
            {
                _connectionClient = new TcpClient(AddressFamily.InterNetworkV6)
                {
                    Client =
                    {
                        DualMode = true
                    }
                };
                _connectionClient.Connect(server);
                var stream = _connectionClient.GetStream();
                var writer = new StreamWriter(stream);
                var reader = new StreamReader(stream);
                var protocol = (PortType) Enum.Parse(typeof(PortType),
                    args[1][0].ToString().ToUpper() + args[1].Substring(1).ToLower());
                await writer.WriteLineAsync(protocol.ToString());
                writer.Flush();
                await writer.WriteLineAsync(port.ToString());
                writer.Flush();
                var response = await reader.ReadLineAsync();
                writer.Close();
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

                var handler = ConnectionConstructors[protocol](ip);
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        await handler.SendToLocal(await handler.ReceiveFromRemote());
                    }
                }).ConfigureAwait(false);
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        await handler.SendToRemote(await handler.ReceiveFromLocal());
                    }
                }).ConfigureAwait(false);
                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }
}
