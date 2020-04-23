using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PortTunneler
{
    public static class NetworkUtils
    {
        //TCP events sent and read as 6 byte strings
        public const string NewClient = "TunnNC";
        public const string EndClient = "TunnEC";
        public const string RecClient = "TunnDC";

        //Read all data currently available without blocking, use stream.DataAvailable to make sure this is needed before using it to prevent confusing bugs 
        public static async Task<byte[]> ReadBytesAsync(this NetworkStream stream)
        {
            if (!stream.CanRead) return new byte[0];
            await using var ms = new MemoryStream();
            var bytes = new byte[2048];
            var read = await stream.ReadAsync(bytes, 0, bytes.Length);
            
            if (read > bytes.Length)
            {
                var b = new byte[read];
                bytes.CopyTo(b, 0);
                await stream.ReadAsync(b, bytes.Length - 1, read - bytes.Length);
                bytes = b;
            }

            await ms.WriteAsync(bytes, 0, read);
            return ms.ToArray();
        }

        public static async Task<byte[]?> ReadSized(this NetworkStream stream)
        {
            var bytes = new byte[2];
            await stream.ReadAsync(bytes, 0, 2);
            var size = BitConverter.ToInt16(new[] {bytes[0], bytes[1]}, 0);
            if (size <= 2) return null;
            bytes = new byte[size];
            await stream.ReadAsync(bytes, 0, size);
            return bytes;
        }
        
        public static async ValueTask WriteSized(this NetworkStream stream, byte[] data)
        {
            if (data.Length <= ushort.MaxValue - 2)
            {
                var bytes = BitConverter.GetBytes(Convert.ToInt16(data.Length)).Concat(data).ToArray();
                await stream.WriteAsync(bytes, 0, bytes.Length);
                await stream.FlushAsync();
            }
            else throw new ArgumentOutOfRangeException(nameof(data.Length), data.Length, "Attempted to write data that was too big.");
        }
    }
}
