using System.Net;
using System.Threading.Tasks;

namespace PortTunneler.Utils
{
    public abstract class PortClient : PortConnection
    {
        protected IPEndPoint EndPoint { get; }

        protected PortClient(Protocol protocol, IPEndPoint endPoint) : base(protocol)
        {
            EndPoint = endPoint;
        }
        
        public abstract Task Connect();
    }
}
