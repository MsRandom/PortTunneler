using System.Net.Sockets;

namespace PortTunneler.Utils
{
    public abstract class PortConnection
    {
        //This is the underlying connection between the client tunnel and the server tunnel, required in all port connections
        public TcpClient? Connection { get; set; }
        protected Protocol Protocol { get; }

        protected PortConnection(Protocol protocol)
        {
            Protocol = protocol;
        }
    }
}
