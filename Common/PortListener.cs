using System.Threading.Tasks;

namespace PortTunneler.Utils
{
    public abstract class PortListener : PortConnection
    {
        public int Port { get; }

        protected PortListener(Protocol protocol, int port) : base(protocol)
        {
            Port = port;
        }
        
        public override string ToString()
        {
            return $"{Port}@{Protocol}";
        }
    }
}
