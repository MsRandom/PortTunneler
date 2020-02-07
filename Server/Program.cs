using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace PortTunneler.Server
{
    internal static class Program
    {
        public static readonly Dictionary<int, PortListener> Listeners = new Dictionary<int, PortListener>();
        public static IPEndPoint? EndPoint { get; private set; }
        private static TcpListener? _connectionListener;

        public static readonly Dictionary<PortType, Func<int, PortListener>> ListenerConstructors =
            new Dictionary<PortType, Func<int, PortListener>>
            {
                {PortType.Tcp, port => new TcpPortListener(port)}
            };

        private static async Task Main(string[] args)
        {
            var listeners = new FileInfo("listeners.json");
            if (listeners.Exists)
            {
                foreach (var (port, protocol) in JsonSerializer.Deserialize<Dictionary<string, PortType>>(listeners
                    .OpenText()
                    .ReadToEnd()))
                {
                    var parsed = int.Parse(port);
                    Listeners[parsed] = ListenerConstructors[protocol](parsed);
                }
            }
            else
            {
                var writer = listeners.CreateText();
                writer.Write("{}");
                writer.Close();
            }
            
            var active = true;
            try
            {
                var p = args.Length == 1 ? int.Parse(args[0]) : 2020;
                var endPoint = new IPEndPoint(Socket.OSSupportsIPv6 ? IPAddress.IPv6Any : IPAddress.Any, p);
                _connectionListener = TcpListener.Create(p);
                _connectionListener.Start();
                EndPoint = endPoint;
                Console.WriteLine($"Listening to connections at {endPoint}");
                listen:
                    do
                    {
                        if (_connectionListener.Pending())
                        {
                            var client = await _connectionListener.AcceptTcpClientAsync();
                            var stream = client.GetStream();
                            var reader = new StreamReader(stream);
                            var writer = new StreamWriter(stream);
                            var line = await reader.ReadLineAsync();
                            if (string.IsNullOrEmpty(line) || !Enum.TryParse<PortType>(line, out var protocol)) continue;
                            line = await reader.ReadLineAsync();
                            if(string.IsNullOrEmpty(line) || !int.TryParse(line, out var port)) continue;
                            var failed = false;
                            PortListener listener;
                            if (Listeners.ContainsKey(port))
                            {
                                listener = Listeners[port];
                                if (listener.Connection != null && listener.Connection.Connected)
                                {
                                    writer.WriteLine("E");
                                    failed = true;
                                }
                                else
                                    writer.WriteLine("W");
                            }
                            else
                            {
                                listener = Listeners[port] = ListenerConstructors[protocol](port);
                                var deserialized = JsonSerializer.Deserialize<Dictionary<string, PortType>>(
                                    listeners
                                        .OpenText()
                                        .ReadToEnd());
                                deserialized[port.ToString()] = protocol;
                                //var fileWriter = new StreamWriter(listeners.OpenWrite());
                                //await fileWriter.WriteLineAsync(JsonSerializer.Serialize(deserialized));
                                //fileWriter.Close();
                                writer.WriteLine("I");
                            }

                            writer.Close();

                            if (failed)
                                Console.WriteLine(
                                    $"Client attempted to use listener {listener} which is already in use. Connection was rejected.");
                            else
                            {
                                listener.Connection = client;
                                listener.Connect();
                                Console.WriteLine(
                                    $"Client connected and added to listener {listener}.");
                            }
                        }
                        else
                        {
                            active = false;
                        }
                    } while (active);
                    
                    await Task.Delay(100); 
                    active = true; 
                    goto listen;
            }
            finally
            {
                _connectionListener?.Stop();
                Console.WriteLine("Listener Stopped");
            }
        }
    }
}
