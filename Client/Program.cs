using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PortTunneler.Client
{
    internal static class Program
    {
        private static readonly Dictionary<ushort, TcpClient> Clients = new Dictionary<ushort, TcpClient>();
        private static TcpClient _connectionClient;
        private static bool _startedReading;
        
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

            if(port < 1024 || port > 49151) throw new ArgumentOutOfRangeException(nameof(port), port, "Port is too " + (port < 1024 ? "small" : "big"));
            
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
                var bytes = BitConverter.GetBytes((ushort) port);
                await stream.WriteAsync(bytes, 0, bytes.Length);
                await stream.FlushAsync();
                bytes = new byte[2];
                await stream.ReadAsync(bytes, 0, bytes.Length);
                var response = BitConverter.ToChar(bytes, 0);
                switch (response)
                {
                    case 'W':
                        Console.WriteLine(
                            "Warning: You are using a port which had been used before, if it was not you who previously used the port, please stop and refrain from using it in the future.");
                        break;
                    case 'E':
                        throw new Exception("Port is already in use.");
                    default:
                    {
                        if (response != 'I')
                            throw new Exception(
                                "Failed to get success status from server. Assuming connection failure.");
                        Console.WriteLine("Connected.");
                        break;
                    }
                }

                while (_connectionClient.Connected)
                {
                    bytes = new byte[8];
                    await stream.ReadAsync(bytes, 0, bytes.Length);
                    var connection = Encoding.UTF8.GetString(bytes);
                    if (connection != NetworkUtils.NewClient) continue;
                    bytes = new byte[2];
                    await stream.ReadAsync(bytes, 0, bytes.Length);
                    var id = BitConverter.ToUInt16(bytes, 0);
                    var client = new TcpClient();
                    client.Connect(ip);
                    Clients[id] = client;
                    HandleTraffic(id, client).Continue();
                }
                
                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        private static Task HandleTraffic(ushort id, TcpClient client)
        {
            var first = false;
            if (!_startedReading)
            {
                _startedReading = true;
                first = true;
            }
            while (_connectionClient.Connected)
            {
                WriteToRemote(id, client).Continue();
                if(first) WriteToLocal().Continue();
            }
            return Task.CompletedTask;
        }
        
        private static async Task WriteToLocal()
        {
            var bytes = await _connectionClient.GetStream().ReadBytesAsync();
            if (bytes.Length > 2)
            {
                var list = new List<byte>(bytes);
                var shorts = new[] {bytes[0], bytes[1]};
                list.RemoveRange(0, 2);
                bytes = list.ToArray();
                var client = Clients[BitConverter.ToUInt16(shorts, 0)];
                await client.GetStream().WriteAsync(bytes, 0, bytes.Length);
                await client.GetStream().FlushAsync();
            }
        }
        
        private static async Task WriteToRemote(ushort id, TcpClient client)
        {
            var bytes = BitConverter.GetBytes(id).Concat(await client.GetStream().ReadBytesAsync()).ToArray();
            await _connectionClient.GetStream().WriteAsync(bytes, 0, bytes.Length);
            await _connectionClient.GetStream().FlushAsync();
        }
    }
}
