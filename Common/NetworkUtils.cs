using System;
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

        //Meant to be used as a way of "Fire and Forget", awaits the task without blocking the current thread and adds error handling, not sure how well it works
        public static async void Continue(this Task task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                throw;
            }
        }

        //Is completely blocking if awaited, used to listen to connections with a callback for reaching them
        public static async Task CreateTcpServer(this TcpListener listener, Func<TcpClient, Task> connection, Action end)
        {
            await CreateActiveListener(async () => await connection(await listener.AcceptTcpClientAsync()), end);
        }
        
        public static async Task CreateActiveListener(Func<Task> callback, Action end)
        {
            try
            {
                var active = true;
                listen:
                do
                {
                    try
                    {
                        await callback();
                    }
                    catch
                    {
                        active = false;
                    }
                } while (active);

                await Task.Delay(100);
                active = true;
                goto listen;
            }
            finally
            {
                end();
            }
        }
    }
}
