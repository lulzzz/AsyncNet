using System.Net;

namespace AsyncNet.Tcp.Server
{
    public class TcpServerStartedData : TcpServerEventData
    {
        public TcpServerStartedData(IPAddress serverAddress, int serverPort) : base(serverAddress, serverPort)
        {
        }
    }
}
