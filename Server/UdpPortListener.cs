using PortTunneler.Utils;

namespace PortTunneler.Server
{
    public class UdpPortListener : PortListener
    {
        public UdpPortListener(Protocol protocol, int port) : base(protocol, port)
        {
        }

        public override void Connect()
        {
            throw new System.NotImplementedException();
        }
    }
}
