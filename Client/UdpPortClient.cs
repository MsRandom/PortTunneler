using System.Net;
using System.Threading.Tasks;
using PortTunneler.Utils;

namespace PortTunneler.Client
{
    public class UdpPortClient : PortClient
    {
        public UdpPortClient(Protocol protocol, IPEndPoint endPoint) : base(protocol, endPoint)
        {
        }

        public override Task Connect()
        {
            throw new System.NotImplementedException();
        }
    }
}