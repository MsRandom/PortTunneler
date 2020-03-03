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

        public override void Connect()
        {
            _listener.Start();
        }

        public override async Task HandleTraffic()
        {
            if (Connection != null)
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

                    if (Connection.GetStream().DataAvailable) await WriteToRemote();
                    foreach (var (id, client) in _clients)
                    {
                        if (client.Connected)
                        {
                            if (client.GetStream().DataAvailable)
                                await WriteToLocal(id, client);
                        }
                        else
                        {
                            await WriteData(NetworkUtils.EndClient, id);
                            _clients.Remove(id, out var c);
                            if (c != null && client == c)
                                c.Close();
                            break;
                        }
                    }
                }
                catch
                {
                    await Close();
                }
            }
        }

        public override async Task Close()
        {
            if (Connection != null)
            {
                if (Connection.Connected)
                {
                    try
                    {
                        await Write(Encoding.UTF8.GetBytes(NetworkUtils.EndClient));
                    }
                    catch
                    {
                        //ignored
                    }
                }
                foreach (var (_, client) in _clients)
                {
                    client.Close();
                }

                Connection.Close();
                Connection = null;
            }
            _listener.Stop();
        }

        private async Task WriteToLocal(ushort id, TcpClient client)
        {
            if (Connection == null) return;
            var bytes = await client.GetStream().ReadBytesAsync();
            await WriteData(NetworkUtils.RecClient, id);
            await Connection.GetStream().WriteSized(bytes);
        }

        private async Task WriteToRemote()
        {
            if (Connection == null) return;
            var bytes = await Connection.GetStream().ReadSized();
            if(bytes == null) return;
            var shorts = new[] {bytes[0], bytes[1]};
            var client = _clients[BitConverter.ToUInt16(shorts, 0)];
            if (!client.Connected) return;
            await client.GetStream().WriteAsync(bytes, 2, bytes.Length - 2);
            await client.GetStream().FlushAsync();
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
