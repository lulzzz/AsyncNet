using System.Net;

namespace AsyncNet.Core
{
    public class ServerEventData
    {
        public ServerEventData(
            IPAddress serverAddress,
            int serverPort)
        {
            this.ServerAddress = serverAddress;
            this.ServerPort = serverPort;
        }

        public IPAddress ServerAddress { get; }

        public int ServerPort { get; }
    }
}
