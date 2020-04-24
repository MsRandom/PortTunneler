using System.Net;
using System.Threading.Tasks;
using PortTunneler.Utils;

namespace PortTunneler.Client
{
    public class UdpPortClient : PortClient
    {
        private UdpPortClient(Protocol protocol, IPEndPoint endPoint) : base(protocol, endPoint) {}

        public override Task Listen()
        {
            throw new System.NotImplementedException();
        }
        
        public static PortClient Create(Protocol protocol, IPEndPoint endPoint) => new UdpPortClient(protocol, endPoint);
    }
}