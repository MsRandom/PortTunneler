using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PortTunneler.Utils;

namespace PortTunneler.Client
{
    public class TcpPortClient : PortClient
    {
        private readonly Dictionary<ushort, TcpClient> _clients = new Dictionary<ushort, TcpClient>();
        
        private TcpPortClient(Protocol protocol, IPEndPoint endPoint) : base(protocol, endPoint) {}
        
        public override async Task HandleTraffic()
        {
            if(Connection == null) return;
            if (Connection.GetStream().DataAvailable)
            {
                var bytes = new byte[6];
                await Connection.GetStream().ReadAsync(bytes, 0, bytes.Length);
                var connection = Encoding.UTF8.GetString(bytes);
                if (connection == NetworkUtils.RemClient)
                {
                    await Close();
                    return;
                }
                if (connection != NetworkUtils.NewClient && connection != NetworkUtils.EndClient &&
                    connection != NetworkUtils.RecClient) return;
                bytes = new byte[2];
                await Connection.GetStream().ReadAsync(bytes, 0, bytes.Length);
                var id = BitConverter.ToUInt16(bytes, 0);
                switch (connection)
                {
                    case NetworkUtils.NewClient:
                        var client = new TcpClient();
                        await client.ConnectAsync(EndPoint.Address, EndPoint.Port);
                        _clients[id] = client;
                        break;
                    case NetworkUtils.EndClient:
                        var removed = _clients[id];
                        removed.Close();
                        _clients.Remove(id);
                        break;
                    case NetworkUtils.RecClient:
                        var rec = _clients[id];
                        bytes = await Connection.GetStream().ReadSized();
                        if(bytes == null) return;
                        await rec.GetStream().WriteAsync(bytes, 0, bytes.Length);
                        await rec.GetStream().FlushAsync();
                        break;
                }
            }
            foreach (var (id, client) in _clients)
            {
                if (!client.GetStream().DataAvailable) continue;
                var bytes = await client.GetStream().ReadBytesAsync();
                await Connection.GetStream().WriteSized(BitConverter.GetBytes(id).Concat(bytes).ToArray());
            }
        }

        public override Task Close()
        {
            foreach (var (_, client) in _clients)
            {
                client.Close();
            }

            Connection?.Close();
            return Task.CompletedTask;
        }

        public static PortClient Create(Protocol protocol, IPEndPoint endPoint) => new TcpPortClient(protocol, endPoint);
    }
}
