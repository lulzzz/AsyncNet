namespace AsyncNet.Udp.Client.SystemEvent
{
    public class UdpClientReadyData : UdpClientEventData
    {
        public UdpClientReadyData(AsyncNetUdpClient client, string serverHostname, int serverPort) : base(serverHostname, serverPort)
        {
            this.Client = client;
        }

        public AsyncNetUdpClient Client { get; }
    }
}
