namespace AsyncNet.Udp.Client
{
    public class UdpClientStartedData : UdpClientEventData
    {
        public UdpClientStartedData(string serverHostname, int serverPort) : base(serverHostname, serverPort)
        {
        }
    }
}
