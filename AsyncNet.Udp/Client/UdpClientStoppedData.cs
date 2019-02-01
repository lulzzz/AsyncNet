namespace AsyncNet.Udp.Client
{
    public class UdpClientStoppedData : UdpClientEventData
    {
        public UdpClientStoppedData(string serverHostname, int serverPort) : base(serverHostname, serverPort)
        {
        }
    }
}
