namespace AsyncNet.Tcp.Client
{
    public class TcpClientStartedData : TcpClientEventData
    {
        public TcpClientStartedData(string serverHostname, int serverPort) : base(serverHostname, serverPort)
        {
        }
    }
}
