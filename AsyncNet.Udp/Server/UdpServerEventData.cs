using System.Net;
using System.Net.Sockets;

namespace AsyncNet.Udp.Server
{
    public class UdpServerEventData
    {
        public UdpServerEventData(
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
