using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using PortTunneler.Utils;

namespace PortTunneler.Server
{
    public class TcpPortListener : PortListener
    {
        private readonly Dictionary<ushort, TcpClient> _clients = new Dictionary<ushort, TcpClient>();
        private readonly Dictionary<TcpClient, (BinaryReader, BinaryWriter)> _streams = new Dictionary<TcpClient, (BinaryReader, BinaryWriter)>();
        private readonly TcpListener _listener;
        private BinaryReader _reader;
        private BinaryWriter _writer;
        private bool _startedReading;

        private TcpPortListener(Protocol protocol, int port) : base(protocol, port)
        {
            _listener = TcpListener.Create(port);
        }
        
        public override async Task Connect()
        {
            var stream = Connection?.GetStream();
            if (stream != null)
            {
                _reader = new BinaryReader(stream);
                _writer = new BinaryWriter(stream);
                _listener.Start();
                await _listener.CreateTcpServer(client =>
                {
                    if (_clients.Count >= ushort.MaxValue || Connection == null) return Task.CompletedTask;
                    var id = (ushort) _clients.Count;
                    var s = client.GetStream();
                    _clients[id] = client;
                    _streams[client] = (new BinaryReader(s), new BinaryWriter(s));
                    WriteData(NetworkUtils.NewClient, id);
                    HandleTraffic(id, client).Continue();
                    return Task.CompletedTask;
                }, () => _listener.Stop());
            }
        }
        
        private Task HandleTraffic(ushort id, TcpClient client)
        {
            if (!_startedReading)
            {
                _startedReading = true;
                WriteToRemote().Continue();
            }
            while (Connection.Connected)
            {
                if(_clients.ContainsValue(client) && client.Connected) WriteToLocal(id, client);
                else
                {
                    WriteData(NetworkUtils.EndClient, id);
                    _clients.Remove(id);
                    client.Close();
                    break;
                }
            }
            return Task.CompletedTask;
        }

        private void WriteToLocal(ushort id, TcpClient client)
        {
            var bytes = _streams[client].Item1.ReadBytes(0);
            WriteData(NetworkUtils.RecClient, id);
            _writer.Write(bytes);
            _writer.Flush();
        }

        private Task WriteToRemote()
        {
            while (Connection.Connected)
            {
                var bytes = _reader.ReadBytes(0);
                var shorts = new[] {bytes[0], bytes[1]};
                var client = _clients[BitConverter.ToUInt16(shorts, 0)];
                if (!client.Connected) continue;
                var writer = _streams[client].Item2;
                writer.Write(bytes, 2, bytes.Length - 2);
                writer.Flush();
            }
            return Task.CompletedTask;
        }

        private void WriteData(string type, ushort id)
        {
            _writer.Write(type);
            _writer.Flush();
            _writer.Write(id);
            _writer.Flush();
        }

        public static PortListener Create(Protocol protocol, int port) => new TcpPortListener(protocol, port);
    }
}
