using System.Net;

namespace PortTunneler.Utils
{
    public abstract class PortClient : PortConnection
    {
        protected IPEndPoint EndPoint { get; }

        protected PortClient(Protocol protocol, IPEndPoint endPoint) : base(protocol)
        {
            EndPoint = endPoint;
        }
    }
}
