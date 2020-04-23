using System;
using System.Collections.Generic;
using System.Net;

namespace PortTunneler.Utils
{
    public class Protocol
    {
        private static readonly Dictionary<string, Protocol> Registry = new Dictionary<string, Protocol>();
        
        private string Name { get; }
        public Func<Protocol, int, PortListener> CreateServer { get; }
        public Func<Protocol, IPEndPoint, PortClient> CreateClient { get; }

        public Protocol(string name, Func<Protocol, int, PortListener> serverCreator, Func<Protocol, IPEndPoint, PortClient> clientCreator)
        {
            Name = name;
            CreateServer = serverCreator;
            CreateClient = clientCreator;
        }
        
        public static void Init(Protocol protocol)
        {
            Registry[protocol.Name] = protocol;
        }
        
        public override string ToString()
        {
            return Name;
        }
        
        public override bool Equals(object? obj)
        {
            return obj is Protocol protocol && Name.Equals(protocol.Name);
        }
        
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        
        public static bool TryParse(string name, out Protocol? protocol)
        {
            if (Registry.ContainsKey(name))
            {
                protocol = Registry[name];
                return true;
            }

            protocol = null;
            return false;
        }
    }
}
