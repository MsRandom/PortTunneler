namespace PortTunneler.Utils
{
    public abstract class PortListener : PortConnection
    {
        private int Port { get; }

        protected PortListener(Protocol protocol, int port) : base(protocol)
        {
            Port = port;
        }
        
        //Connects the listener without blocking the thread
        public abstract void Connect();
        
        public override string ToString()
        {
            return $"{Port}@{Protocol}";
        }

        public override bool Equals(object? obj)
        {
            return obj is PortListener listener && listener.Port == Port && listener.Protocol.Equals(Protocol);
        }

        public override int GetHashCode()
        {
            return Port ^ Protocol.GetHashCode();
        }
    }
}
