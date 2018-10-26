using System.Net;

namespace AsyncNet.Core
{
    public class ServerStartedData : ServerEventData
    {
        public ServerStartedData(IPAddress serverAddress, int serverPort) : base(serverAddress, serverPort)
        {
        }
    }
}
