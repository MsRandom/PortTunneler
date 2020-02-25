using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using PortTunneler.Utils;

namespace PortTunneler.Client
{
    public class TcpPortClient : PortClient
    {
        private readonly Dictionary<ushort, TcpClient> _clients = new Dictionary<ushort, TcpClient>();
        private readonly Dictionary<TcpClient, (BinaryReader, BinaryWriter)> _streams = new Dictionary<TcpClient, (BinaryReader, BinaryWriter)>();
        private BinaryReader _reader;
        private BinaryWriter _writer;
        
        private TcpPortClient(Protocol protocol, IPEndPoint endPoint) : base(protocol, endPoint) {}
        
        public override async Task Connect()
        {
            var stream = Connection?.GetStream();
            if (stream != null)
            {
                _reader = new BinaryReader(stream);
                _writer = new BinaryWriter(stream);
                await NetworkUtils.CreateActiveListener(async () =>
                {
                    var connection = _reader.ReadString();
                    if (connection != NetworkUtils.NewClient && connection != NetworkUtils.EndClient &&
                        connection != NetworkUtils.RecClient) return;
                    var id = _reader.ReadUInt16();
                    switch (connection)
                    {
                        case NetworkUtils.NewClient:
                            var client = new TcpClient();
                            client.Connect(EndPoint);
                            var s = client.GetStream();
                            _clients[id] = client;
                            _streams[client] = (new BinaryReader(s), new BinaryWriter(s));
                            HandleTraffic(id, client).Continue();
                            break;
                        case NetworkUtils.EndClient:
                            var removed = _clients[id];
                            removed.Close();
                            _clients.Remove(id);
                            break;
                        case NetworkUtils.RecClient:
                            var rec = _clients[id];
                            var bytes = _reader.ReadBytes(0);
                            _streams[rec].Item2.Write(bytes);
                            await rec.GetStream().WriteAsync(bytes, 0, bytes.Length);
                            await rec.GetStream().FlushAsync();
                            break;
                    }
                }, () => Console.WriteLine("Connection ended."));
            }
        }

        private Task HandleTraffic(ushort id, TcpClient client)
        {
            while (client.Connected && Connection != null && Connection.Connected)
            {
                var bytes = _streams[client].Item1.ReadBytes(0);
                _writer.Write(BitConverter.GetBytes(id).Concat(bytes).ToArray());
                _writer.Flush();
            }
            return Task.CompletedTask;
        }
        
        public static PortClient Create(Protocol protocol, IPEndPoint endPoint) => new TcpPortClient(protocol, endPoint);
    }
}
