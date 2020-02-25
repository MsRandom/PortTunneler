using System.Net.Sockets;
using System.Threading.Tasks;
using PortTunneler.Utils;

namespace PortTunneler.Server
{
    public class TcpPortListener : PortListener
    {
        private readonly TcpListener _listener;

        private TcpPortListener(Protocol protocol, int port) : base(protocol, port)
        {
            _listener = TcpListener.Create(port);
        }
        
        public override async Task Connect()
        {
            _listener.Start();
            
        }
        
        public static PortListener Create(Protocol protocol, int port) => new TcpPortListener(protocol, port);
    }
}
