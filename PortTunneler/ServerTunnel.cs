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
        private readonly Dictionary<int, PortListener> _listeners = new Dictionary<int, PortListener>();
        private TcpListener? _connectionListener;

        public override async Task Run(string[] args)
        {
            const string listeners = "listeners.json";
            if (File.Exists(listeners))
            {
                foreach (var (key, value) in JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(listeners)))
                {
                    var port = int.Parse(key);
                    if (Protocol.TryParse(value, out var protocol) && protocol != null)
                        _listeners[port] = protocol.CreateServer(protocol, port);
                }
            }
            else
                await File.WriteAllTextAsync(listeners, "{}");

            var p = args.Length == 1 ? int.Parse(args[0]) : 2020;
            _connectionListener = TcpListener.Create(p);
            _connectionListener.Start();
            Console.WriteLine($"Listening to connections at {_connectionListener.LocalEndpoint}, enter 'stop' to close the listener.");
            var active = true;
            Task.Run(() =>
            {
                do
                {
                    var line = Console.ReadLine();
                    if (!string.IsNullOrEmpty(line) && line.ToLower().Contains("stop")) active = false;
                } while (active);
            }).Continue();
            while (active)
            {
                try
                {
                    foreach (var (_, value) in _listeners)
                    {
                        await value.HandleTraffic();
                    }

                    if (!_connectionListener.Pending()) continue;
                    var client = await _connectionListener.AcceptTcpClientAsync();
                    var stream = client.GetStream();
                    var reader = new StreamReader(stream);
                    var connection = await reader.ReadLineAsync();
                    if (connection == null) continue;
                    var variables = connection.Split('@');
                    if (variables.Length <= 1 || !int.TryParse(variables[0], out var port) ||
                        !Protocol.TryParse(variables[1], out var protocol)) continue;
                    if (protocol == null) continue;
                    PortListener listener;
                    char code;
                    if (_listeners.ContainsKey(port))
                    {
                        listener = _listeners[port];
                        if (listener.Connection != null && listener.Connection.Connected)
                        {
                            Console.WriteLine(
                                $"Client attempted to use listener {listener} which is already in use. Connection was rejected.");
                            await stream.WriteAsync(BitConverter.GetBytes('E'), 0, 1);
                            await stream.FlushAsync();
                            continue;
                        }

                        code = 'W';
                    }
                    else
                    {
                        listener = _listeners[port] = protocol.CreateServer(protocol, port);
                        var deserialized =
                            JsonSerializer.Deserialize<Dictionary<string, string>>(
                                await File.ReadAllTextAsync(listeners));
                        deserialized[port.ToString()] = protocol.ToString();
                        await File.WriteAllTextAsync(listeners, JsonSerializer.Serialize(deserialized));
                        code = 'I';
                    }

                    await stream.WriteAsync(BitConverter.GetBytes(code), 0, 1);
                    await stream.FlushAsync();
                    listener.Connection = client;
                    listener.Connect();
                    Console.WriteLine(
                        $"Client connected and added to listener {listener}.");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Caught an exception, exiting.");
                    Console.Error.WriteLine(e);
                    active = false;
                }
            }

            _connectionListener?.Stop();
            foreach (var (_, value) in _listeners)
            {
                await value.Close();
            }
            Console.WriteLine("Listener Stopped, press any key to continue...");
            Console.ReadKey();
        }
    }
}
