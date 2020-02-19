using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PortTunneler.Server
{
    public class PortListener
    {
        private readonly Dictionary<ushort, TcpClient> _clients = new Dictionary<ushort, TcpClient>();
        private readonly TcpListener _listener;
        public TcpClient? Connection { get; set; }
        private bool _startedReading;

        public PortListener(int port)
        {
            _listener = TcpListener.Create(port);
        }

        public async Task Connect()
        {
            _listener.Start();
            var active = true;
            try
            {
                listen:
                do
                {
                    if (_listener.Pending())
                    {
                        var client = await _listener.AcceptTcpClientAsync();
                        if (_clients.Count >= ushort.MaxValue || Connection == null) continue;
                        var id = (ushort) _clients.Count;
                        _clients[id] = client;

                        var bytes = Encoding.UTF8.GetBytes(NetworkUtils.NewClient);
                        await Connection.GetStream().WriteAsync(bytes, 0, bytes.Length);
                        await Connection.GetStream().FlushAsync();
                        bytes = BitConverter.GetBytes(id);
                        await Connection.GetStream().WriteAsync(bytes, 0, bytes.Length);
                        await Connection.GetStream().FlushAsync();
                        HandleTraffic(id, client).Continue();
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
                _listener?.Stop();
            }
        }
        
        private Task HandleTraffic(ushort id, TcpClient client)
        {
            var first = false;
            if (!_startedReading)
            {
                _startedReading = true;
                first = true;
            }
            while (Connection.Connected)
            {
                WriteToLocal(id, client).Continue();
                if(first) WriteToRemote().Continue();
            }
            return Task.CompletedTask;
        }
        
        private async Task WriteToLocal(ushort id, TcpClient client)
        {
            var bytes = BitConverter.GetBytes(id).Concat(await client.GetStream().ReadBytesAsync()).ToArray();
            await Connection.GetStream().WriteAsync(bytes, 0, bytes.Length);
            await Connection.GetStream().FlushAsync();
        }
        
        private async Task WriteToRemote()
        {
            var bytes = await Connection.GetStream().ReadBytesAsync();
            if (bytes.Length > 2)
            {
                var list = new List<byte>(bytes);
                var shorts = new[] {bytes[0], bytes[1]};
                list.RemoveRange(0, 2);
                bytes = list.ToArray();
                var client = _clients[BitConverter.ToUInt16(shorts, 0)];
                
                if (client.Connected)
                {
                    await client.GetStream().WriteAsync(bytes, 0, bytes.Length);
                    await client.GetStream().FlushAsync();
                }
            }
        }
    }
}
