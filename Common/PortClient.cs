using System.Net;

namespace PortTunneler.Utils
{
    public abstract class PortClient : PortConnection
    {
        public IPEndPoint EndPoint { get; }

        protected PortClient(Protocol protocol, IPEndPoint endPoint) : base(protocol)
        {
            EndPoint = endPoint;
        }
        
        public override string ToString()
        {
            return $"{EndPoint}@{Protocol}";
        }
    }
}
