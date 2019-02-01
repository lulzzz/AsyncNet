namespace AsyncNet.Udp.Client
{
    public class UdpClientEventData
    {
        public UdpClientEventData(string serverHostname, int serverPort)
        {
            this.ServerHostname = serverHostname;
            this.ServerPort = serverPort;
        }

        public string ServerHostname { get; }

        public int ServerPort { get; }
    }
}
