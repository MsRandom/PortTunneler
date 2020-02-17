using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PortTunneler
{
    public static class NetworkUtils
    {
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
    }
}
