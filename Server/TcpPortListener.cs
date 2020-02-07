//using System;
//using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PortTunneler.Server
{
    public class TcpPortListener : PortListener
    {
        public override PortType Protocol { get; } = PortType.Tcp;
        private readonly TcpListener _listener;

        public TcpPortListener(int port) : base(port)
        {
            _listener = TcpListener.Create(port);
        }

        public override async Task Connect()
        {
            await Task.Run(async () =>
            {
                _listener.Start();
                var active = true;
                try
                {
                    listen:
                    do
                    {
                        if (_listener.Pending())
                        {
                            var client = await _listener.AcceptTcpClientAsync();
                            await Task.Run(async () =>
                            {
                                while (true)
                                {
                                    await Connection.GetStream().WriteAsync(await client.GetStream().ReadBytesAsync());
                                    Connection.GetStream().Flush();
                                }
                            }).ConfigureAwait(false);
                            await Task.Run(async () =>
                            {
                                while (true)
                                {
                                    await client.GetStream().WriteAsync(await Connection.GetStream().ReadBytesAsync());
                                    client.GetStream().Flush();
                                }
                            }).ConfigureAwait(false);
                        }
                        else
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
                    _listener?.Stop();
                }
            }).ConfigureAwait(false);
        }
    }
}
