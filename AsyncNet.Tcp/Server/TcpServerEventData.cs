using System.Net;

namespace AsyncNet.Tcp.Server
{
    public class TcpServerEventData
    {
        public TcpServerEventData(
            IPAddress serverAddress,
            int serverPort)
        {
            this.ServerAddress = serverAddress;
            this.ServerPort = serverPort;
        }

        public IPAddress ServerAddress { get; }

        public int ServerPort { get; }
    }
}
