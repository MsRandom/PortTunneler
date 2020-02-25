using PortTunneler.Client;
using PortTunneler.Server;
using PortTunneler.Utils;

namespace PortTunneler
{
    public static class Protocols
    {
        public static readonly Protocol Tcp = new Protocol("tcp", TcpPortListener.Create, TcpPortClient.Create);
        //public static readonly Protocol Udp = new Protocol("udp", UdpPortListener.Create, UdpPortClient.Create);
        //public static readonly Protocol TcpAndUdp = new Protocol("tcp/udp", TcpUdpPortListener.Create, TcpUdpPortClient.Create);
    }
}
