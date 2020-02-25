using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
                    "Port is too " + (port < 1024 ? "small" : "big"));

            try
            {
                var connectionClient = new TcpClient();
                connectionClient.Connect(server);
                var stream = connectionClient.GetStream();
                var reader = new BinaryReader(stream);
                var writer = new StreamWriter(stream);
                await writer.WriteLineAsync($"{port}@{protocol}");
                await writer.FlushAsync();
                var response = reader.ReadChar();
                switch (response)
                {
                    case 'W':
                        Console.WriteLine(
                            "Warning: You are using a port which had been used before, if it was not you who previously used the port, please stop and refrain from using it in the future.");
                        break;
                    case 'E':
                        throw new ArgumentException("Port is already in use.");
                    default:
                    {
                        Console.WriteLine("Connected.");
                        break;
                    }
                }

                var handler = protocol.CreateClient(protocol, ip);
                await handler.Connect();

                /*while (connectionClient.Connected)
                {
                    bytes = new byte[6];
                    await stream.ReadAsync(bytes, 0, bytes.Length);
                    var connection = Encoding.UTF8.GetString(bytes);
                    if (connection != NetworkUtils.NewClient && connection != NetworkUtils.EndClient &&
                        connection != NetworkUtils.RecClient) continue;
                    bytes = new byte[2];
                    await stream.ReadAsync(bytes, 0, bytes.Length);
                    var id = BitConverter.ToUInt16(bytes, 0);
                    switch (connection)
                    {
                        case NetworkUtils.NewClient:
                            var client = new TcpClient();
                            client.Connect(ip);
                            Clients[id] = client;
                            HandleTraffic(id, client).Continue();
                            break;
                        case NetworkUtils.EndClient:
                            var removed = Clients[id];
                            removed.Close();
                            Clients.Remove(id);
                            break;
                        case NetworkUtils.RecClient:
                            bytes = await stream.ReadBytesAsync();
                            var rec = Clients[id];
                            await rec.GetStream().WriteAsync(bytes, 0, bytes.Length);
                            await rec.GetStream().FlushAsync();
                            break;

                    }
                }*/
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }
}
