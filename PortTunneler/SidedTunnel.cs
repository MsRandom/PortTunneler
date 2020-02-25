using System.Threading.Tasks;

namespace PortTunneler
{
    public abstract class SidedTunnel
    {
        protected SidedTunnel()
        {
            Program.Sides[GetType().Name.Replace("Tunnel", "").ToLower()] = this;
        }

        public abstract Task Run(string[] args);
    }
}
