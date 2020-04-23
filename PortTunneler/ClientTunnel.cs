using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using PortTunneler.Utils;

namespace PortTunneler
{
    public class ClientTunnel : SidedTunnel
    {
        public override async Task Run(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: PortTunneler client [server] [protocol] [ip] [port]");
                return;
            }

            var server = IPEndPoint.Parse(args[0]);
            if (server.Port == 0) server.Port = 2020;
            if (!Protocol.TryParse(args[1], out var protocol))
            {
                Console.WriteLine("Invalid protocol was used.");
                return;
            }
            
            if (!IPEndPoint.TryParse(args[2], out var ip))
            {
                Console.WriteLine("Invalid ip endpoint for the local server.");
                return;
            }
            
            int port;
            if (args.Length == 3)
            {
                port = ip.Port;
                if (port == 0)
                {
                    Console.WriteLine("A port needs to be specified in the IP or as an argument.");
                    return;
                }
            }
            else {
                if (!int.TryParse(args[3], out port))
                {
                    Console.WriteLine("Invalid port specified.");
                    return;
                }
            }

            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
                throw new ArgumentOutOfRangeException(nameof(port), port,
                    "Port is too " + (port < IPEndPoint.MinPort ? "small" : "big"));

            try
            {
                var connectionClient = new TcpClient();
                await connectionClient.ConnectAsync(server.Address, server.Port);
                var stream = connectionClient.GetStream();
                var writer = new StreamWriter(stream);
                await writer.WriteLineAsync($"{port}@{protocol}");
                await writer.FlushAsync();
                var bytes = new byte[2];
                await stream.ReadAsync(bytes, 0, 2);
                var response = BitConverter.ToChar(bytes, 0);
                switch (response)
                {
                    case 'W':
                        Console.WriteLine(
                            "Warning: You are using a port which had been used before, if it was not you who previously used the port, please stop using it and refrain from doing so in the future.");
                        break;
                    case 'E':
                        throw new ArgumentException("Port is already in use.");
                    default:
                    {
                        Console.WriteLine("Connected.");
                        break;
                    }
                }

                if (protocol != null)
                {
                    var client = protocol.CreateClient(protocol, ip);
                    client.Connection = connectionClient;
                    await client.Connect();
                    Console.WriteLine("Connection ended, press any key to continue...");
                    Console.ReadKey();
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                Console.WriteLine("Caught an exception, connection ended.");
            }
        }
    }
}
