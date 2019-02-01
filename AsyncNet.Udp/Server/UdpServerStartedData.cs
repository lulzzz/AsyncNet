using System.Net;

namespace AsyncNet.Udp.Server
{
    public class UdpServerStartedData : UdpServerEventData
    {
        public UdpServerStartedData(IPAddress serverAddress, int serverPort) : base(serverAddress, serverPort)
        {
        }
    }
}
