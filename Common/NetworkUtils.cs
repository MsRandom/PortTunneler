using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PortTunneler
{
    public static class NetworkUtils
    {
        public static async Task<byte[]> ReadBytesAsync(this NetworkStream stream)
        {
            var resp = new byte[short.MaxValue];
            var memStream = new MemoryStream();
            int bytes;
            do
            {
                bytes = await stream.ReadAsync(resp, 0, resp.Length);
                await memStream.WriteAsync(resp, 0, bytes);
            } while (bytes > 0);
            return memStream.ToArray();
        }
    }
}
