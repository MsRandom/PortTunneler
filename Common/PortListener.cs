using System;

namespace PortTunneler.Utils
{
    public abstract class PortListener : PortConnection, IDisposable
    {
        public int Port { get; }
        protected bool Active { get; set; }

        protected PortListener(Protocol protocol, int port) : base(protocol)
        {
            Port = port;
        }

        public abstract void Start();
        
        public abstract void Dispose();

        public override string ToString()
        {
            return $"{Port}@{Protocol}";
        }
        
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is PortListener other && Port == other.Port && Protocol == other.Protocol;
        }

        public override int GetHashCode()
        {
            var port = Port;
            var protocol = Protocol.GetHashCode();
            if (port == protocol) port = (port + 17) * 37;
            return port ^ protocol;
        }
        
        public static bool operator ==(PortListener listener, PortListener other) => listener.Equals(other);
        public static bool operator !=(PortListener listener, PortListener other) => !(listener == other);
    }
}
