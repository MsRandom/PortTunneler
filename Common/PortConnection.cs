using System.Net.Sockets;
using System.Threading.Tasks;

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
        
        //Handles all traffic in the main program loop without blocking the thread
        public abstract Task HandleTraffic();

        public abstract Task Close();
    }
}
