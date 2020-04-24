using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PortTunneler.Utils;

namespace PortTunneler.Server
{
    public class TcpPortListener : PortListener
    {
        private readonly Dictionary<ushort, TcpClient> _clients = new Dictionary<ushort, TcpClient>();
        private readonly TcpListener _listener;

        private TcpPortListener(Protocol protocol, int port) : base(protocol, port)
        {
            _listener = TcpListener.Create(port);
        }

        public override void Start()
        {
            Active = true;
            _listener.Start();

            if (Connection == null) return;
            Task.Run(async () =>
            {
                while (Active)
                {
                    try
                    {
                        if (_listener.Pending())
                        {
                            var client = await _listener.AcceptTcpClientAsync();
                            if (_clients.Count >= ushort.MaxValue || Connection == null) return;
                            var id = (ushort) _clients.Count;
                            _clients[id] = client;
                            await WriteData(NetworkUtils.NewClient, id);
                        }
                        
                        if (Connection == null) continue;
                        
                        if (Connection.GetStream().DataAvailable)
                        {
                            var bytes = await Connection.GetStream().ReadSized();
                            if (bytes == null) return;
                            if (bytes.Length == NetworkUtils.EndClient.Length &&
                                Encoding.UTF8.GetString(bytes) == NetworkUtils.EndClient)
                            {
                                Dispose();
                                return;
                            }

                            var shorts = bytes[..2]!;
                            var client = _clients[BitConverter.ToUInt16(shorts, 0)];
                            if (!client.Connected) return;
                            await client.GetStream().WriteAsync(bytes, 2, bytes.Length - 2);
                            await client.GetStream().FlushAsync();
                        }

                        foreach (var (id, client) in _clients)
                        {
                            if (client.Connected)
                            {
                                if (!client.GetStream().DataAvailable) continue;
                                var bytes = await client.GetStream().ReadBytesAsync();
                                await WriteData(NetworkUtils.RecClient, id);
                                await Connection.GetStream().WriteSized(bytes);
                            }
                            else
                            {
                                await WriteData(NetworkUtils.EndClient, id);
                                _clients.Remove(id, out var c);
                                if (c == null || client != c) continue;
                                c.GetStream().Close();
                                c.Close();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{this} caught an exception.");
                        Console.Error.WriteLine(e);
                        Dispose();
                    }
                }
            });
        }

        public override void Dispose()
        {
            Console.WriteLine($"{this} stopped.");
            Active = false;
            foreach (var (_, client) in _clients)
            {
                client.GetStream().Close();
                client.Close();
            }
            _clients.Clear();
            _listener.Stop();
            NetworkUtils.OnListenerEnded(this);
            if (Connection == null) return;
            Connection.GetStream().Close();
            Connection.Close();
        }

        private async Task WriteData(string type, ushort id)
        {
            await Write(Encoding.UTF8.GetBytes(type));
            await Write(BitConverter.GetBytes(id));
        }

        private async Task Write(byte[] bytes)
        {
            if (Connection == null) return;
            await Connection.GetStream().WriteAsync(bytes, 0, bytes.Length);
            await Connection.GetStream().FlushAsync();
        } 
        
        public static PortListener Create(Protocol protocol, int port) => new TcpPortListener(protocol, port);
    }
}
