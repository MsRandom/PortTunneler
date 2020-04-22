using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

        public override async Task Connect()
        {
            _listener.Start();

            if (Connection != null)
            {
                ThreadPool.QueueUserWorkItem(async state =>
                {
                    while (true)
                    {
                        var client = await _listener.AcceptTcpClientAsync();
                        if (_clients.Count >= ushort.MaxValue || Connection == null) return;
                        var id = (ushort) _clients.Count;
                        _clients[id] = client;
                        await WriteData(NetworkUtils.NewClient, id);
                        ThreadPool.QueueUserWorkItem(async callback => await WriteToLocal(id, client));
                    }
                });

                ThreadPool.QueueUserWorkItem(async state => await WriteToRemote());

            }
        }
        
        private async Task WriteToLocal(ushort id, TcpClient client)
        {
            while (true)
            {
                if (client.Connected)
                {
                    if (Connection == null || !client.GetStream().DataAvailable) continue;
                    var bytes = await client.GetStream().ReadBytesAsync();
                    await WriteData(NetworkUtils.RecClient, id);
                    await Connection.GetStream().WriteSized(bytes);
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

        private async Task WriteToRemote()
        {
            while (true)
            {
                if (Connection == null || !Connection.GetStream().DataAvailable) continue;
                var bytes = await Connection.GetStream().ReadSized();
                if (bytes == null) return;
                var shorts = new[] {bytes[0], bytes[1]};
                var client = _clients[BitConverter.ToUInt16(shorts, 0)];
                if (!client.Connected) return;
                await client.GetStream().WriteAsync(bytes, 2, bytes.Length - 2);
                await client.GetStream().FlushAsync();
            }
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
