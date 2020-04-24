using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using PortTunneler.Utils;

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
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                NetworkUtils.ProgramActive = false;
                eventArgs.Cancel = true;
            };
            Protocols.Types.ForEach(Protocol.Init);
            await Sides[args[0]].Run(args[1..]);
        }
    }
}
