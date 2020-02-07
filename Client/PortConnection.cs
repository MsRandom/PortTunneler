using System.Net;
using System.Threading.Tasks;

namespace PortTunneler.Client
{
    public abstract class PortConnection
    {
        public IPEndPoint Address { get; }
        
        protected PortConnection(IPEndPoint address)
        {
            Address = address;
        }

        public abstract Task SendToLocal(byte[] data);
        public abstract Task SendToRemote(byte[] data);
        public abstract Task<byte[]> ReceiveFromLocal();
        public abstract Task<byte[]> ReceiveFromRemote();
    }
}
