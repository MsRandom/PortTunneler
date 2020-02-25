using System.Net;
using System.Threading.Tasks;
using PortTunneler.Utils;

namespace PortTunneler.Client
{
    public class TcpPortClient : PortClient
    {
        private TcpPortClient(Protocol protocol, IPEndPoint endPoint) : base(protocol, endPoint) {}
        
        public override async Task Connect()
        {
            
        }
        
        public static PortClient Create(Protocol protocol, IPEndPoint endPoint) => new TcpPortClient(protocol, endPoint);
    }
}
