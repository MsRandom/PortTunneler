using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PortTunneler.Client;
using PortTunneler.Server;
using PortTunneler.Utils;

namespace PortTunneler
{
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class Protocols
    {
        public static readonly List<Protocol> Types = new List<Protocol>();
        private static readonly Protocol Tcp = Add(new Protocol("tcp", TcpPortListener.Create, TcpPortClient.Create));
        private static readonly Protocol Udp = Add(new Protocol("udp", UdpPortListener.Create, UdpPortClient.Create));
        //private static readonly Protocol TcpAndUdp = Add(new Protocol("tcp/udp", TcpUdpPortListener.Create, TcpUdpPortClient.Create));

        private static Protocol Add(Protocol protocol)
        {
            Types.Add(protocol);
            return protocol;
        }
    }
}
