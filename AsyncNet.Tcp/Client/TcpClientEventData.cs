namespace AsyncNet.Tcp.Client
{
    public class TcpClientEventData
    {
        public TcpClientEventData(string serverHostname, int serverPort)
        {
            this.ServerHostname = serverHostname;
            this.ServerPort = serverPort;
        }

        public string ServerHostname { get; }

        public int ServerPort { get; }
    }
}
