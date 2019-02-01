namespace AsyncNet.Tcp.Client
{
    public class TcpClientStoppedData : TcpClientEventData
    {
        public TcpClientStoppedData(string serverHostname, int serverPort) : base(serverHostname, serverPort)
        {
        }
    }
}
