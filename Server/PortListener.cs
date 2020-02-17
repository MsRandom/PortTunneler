using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PortTunneler.Server
{
    public class PortListener
    {
        private readonly TcpListener _listener;
        public int Port { get; }
        public TcpClient? Connection { get; set; }

        public PortListener(int port)
        {
            _listener = TcpListener.Create(port);
            Port = port;
        }

        public async Task Connect()
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
                        //do something to redirect the client
                        //if(Connection.Client.RemoteEndPoint is IPEndPoint endpoint) client.Connect(endpoint);
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
        }
    }
}
