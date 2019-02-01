using System.Net;
using System.Net.Sockets;

namespace AsyncNet.Udp.Server
{
    public class UdpServerStoppedData : UdpServerEventData
    {
        public UdpServerStoppedData(IPAddress serverAddress, int serverPort) : base(serverAddress, serverPort)
        {
        }
    }
}
