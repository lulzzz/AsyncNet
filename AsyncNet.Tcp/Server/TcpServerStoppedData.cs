using System.Net;

namespace AsyncNet.Tcp.Server
{
    public class TcpServerStoppedData : TcpServerEventData
    {
        public TcpServerStoppedData(IPAddress serverAddress, int serverPort) : base(serverAddress, serverPort)
        {
        }
    }
}
