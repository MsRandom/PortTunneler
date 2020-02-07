using System.Net.Sockets;
using System.Threading.Tasks;

namespace PortTunneler.Server
{
    public abstract class PortListener
    {
        public abstract PortType Protocol { get; }
        public int Port { get; }
        public TcpClient? Connection { get; set; }

        protected PortListener(int port)
        {
            Port = port;
        }

        public abstract Task Connect();

        public override string ToString()
        {
            return $"{Port}@{Protocol.ToString().ToUpper()}";
        }

        public override int GetHashCode()
        {
            return Port ^ (int)Protocol;
        }

        public override bool Equals(object? obj)
        {
            return obj is PortListener p && p.Port == Port && p.Protocol == Protocol && p.Connection == Connection;
        }
    }
}
