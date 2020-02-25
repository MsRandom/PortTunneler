using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PortTunneler
{
    public static class NetworkUtils
    {
        public const string NewClient = "TunnNC";
        public const string EndClient = "TunnEC";
        public const string RecClient = "TunnDC";

        public static async Task<byte[]> ReadBytesAsync(this NetworkStream stream)
        {
            if (!stream.CanRead) return new byte[0];
            await using var ms = new MemoryStream();
            var bytes = new byte[2048];
            //block the thread until data is available
            int read;
            while ((read = await stream.ReadAsync(bytes, 0, bytes.Length)) == 0) {}
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
        
        //Meant to be used as a way of "Fire and Forget", awaits the task without blocking the current thread and adds error handling, not sure how well it works
        public static async void Continue(this Task task) => await task.ContinueWith(t =>
        {
            if (t?.Exception != null) throw t.Exception;
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}
