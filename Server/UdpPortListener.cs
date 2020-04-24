using PortTunneler.Utils;

namespace PortTunneler.Server
{
    public class UdpPortListener : PortListener
    {
        private UdpPortListener(Protocol protocol, int port) : base(protocol, port)
        {
        }

        public override void Start()
        {
            throw new System.NotImplementedException();
        }

        public override void Dispose()
        {
            throw new System.NotImplementedException();
        }
        
        public static PortListener Create(Protocol protocol, int port) => new UdpPortListener(protocol, port);
    }
}
