using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using PortTunneler.Utils;

namespace PortTunneler
{
    public class ServerTunnel : SidedTunnel
    {
        public static readonly Dictionary<int, PortListener> Listeners = new Dictionary<int, PortListener>();
        private static TcpListener? _connectionListener;

        public override async Task Run(string[] args)
        {
            var listeners = new FileInfo("listeners.json");
            var exists = listeners.Exists;
            var fileStream = listeners.Open(FileMode.OpenOrCreate);
            var fileReader = new StreamReader(fileStream);
            var fileWriter = new StreamWriter(fileStream);
            if (exists)
            {
                foreach (var (key, value) in JsonSerializer.Deserialize<Dictionary<string, string>>(
                    await fileReader.ReadToEndAsync()))
                {
                    var port = int.Parse(key);
                    if (Protocol.TryParse(value, out var protocol) && protocol != null)
                        Listeners[port] = protocol.CreateServer(protocol, port);
                }
            }
            else
            {
                await fileWriter.WriteAsync("[]");
                await fileWriter.FlushAsync();
            }

            var p = args.Length == 1 ? int.Parse(args[0]) : 2020;
            _connectionListener = TcpListener.Create(p);
            _connectionListener.Start();
            Console.WriteLine($"Listening to connections at {_connectionListener.LocalEndpoint}");
            await _connectionListener.CreateTcpServer(async client =>
            {
                var stream = client.GetStream();
                var reader = new StreamReader(stream);
                var writer = new BinaryWriter(stream);
                var connection = await reader.ReadLineAsync();
                if (connection == null) return;
                var variables = connection.Split('@');
                if (variables.Length <= 1 || !int.TryParse(variables[0], out var port) ||
                    !Protocol.TryParse(variables[1], out var protocol)) return;
                if (protocol == null) return;
                PortListener listener;
                if (Listeners.ContainsKey(port))
                {
                    listener = Listeners[port];
                    if (listener.Connection != null && listener.Connection.Connected)
                    {
                        Console.WriteLine(
                            $"Client attempted to use listener {listener} which is already in use. Connection was rejected.");
                        writer.Write('E');
                        writer.Flush();
                        return;
                    }

                    writer.Write('W');
                }
                else
                {
                    listener = Listeners[port] = protocol.CreateServer(protocol, port);
                    /*var deserialized = JsonSerializer.Deserialize<List<int>>(fileReader.ReadToEnd());
                        deserialized.Add(port);
                        await fileWriter.WriteLineAsync(JsonSerializer.Serialize(deserialized));
                        await fileWriter.FlushAsync();*/
                    writer.Write('I');
                }

                writer.Flush();
                listener.Connection = client;
                listener.Connect().Continue();
                Console.WriteLine(
                    $"Client connected and added to listener {listener}.");
            }, () =>
            {
                _connectionListener?.Stop();
                Console.WriteLine("Listener Stopped");
            });
        }
    }
}
