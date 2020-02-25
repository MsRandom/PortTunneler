using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace PortTunneler
{
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class Program
    {
        public static readonly Dictionary<string, SidedTunnel> Sides = new Dictionary<string, SidedTunnel>();
        private static SidedTunnel _client = new ClientTunnel();
        private static SidedTunnel _server = new ServerTunnel();

        private static async Task Main(string[] args)
        {
            await Sides[args[0]].Run(args.Skip(0).ToArray());
        }
    }
}
