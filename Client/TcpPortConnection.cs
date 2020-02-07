using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PortTunneler.Client
{
    public class TcpPortConnection : PortConnection
    {
        private readonly TcpClient _local;
        private readonly TcpClient _remote;
        
        public TcpPortConnection(IPEndPoint address) : base(address)
        {
            _local = new TcpClient();
            _remote = new TcpClient();
            _local.Connect(address);
            _remote.Connect(Program.Server);
        }

        public override async Task SendToLocal(byte[] data)
        {
            await _local.GetStream().WriteAsync(data);
            _local.GetStream().Flush();
        }

        public override async Task SendToRemote(byte[] data)
        {
            await _remote.GetStream().WriteAsync(data);
            _remote.GetStream().Flush();
        }

        public override async Task<byte[]> ReceiveFromLocal()
        {
            return await _local.GetStream().ReadBytesAsync();
        }

        public override async Task<byte[]> ReceiveFromRemote()
        {
            return await _remote.GetStream().ReadBytesAsync();
        }
    }
}
