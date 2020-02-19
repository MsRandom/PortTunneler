using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PortTunneler
{
    public static class NetworkUtils
    {
        //"Tunnel New Connection" used to identify new clients in the PortTunneler.Client in 8 bytes
        public const string NewClient = "TunnelNC";
        
        public static async Task<byte[]> ReadBytesAsync(this NetworkStream stream)
        {
            var resp = new byte[1024];
            await using var memStream = new MemoryStream();
            do
            {
                var bytes = await stream.ReadAsync(resp, 0, resp.Length);
                await memStream.WriteAsync(resp, 0, bytes);
            } while (stream.DataAvailable);
            return memStream.ToArray();
        }

        public static async void Continue(this Task task) => await task;
    }
}
