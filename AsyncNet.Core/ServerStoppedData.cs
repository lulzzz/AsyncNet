using System.Net;

namespace AsyncNet.Core
{
    public class ServerStoppedData : ServerEventData
    {
        public ServerStoppedData(IPAddress serverAddress, int serverPort) : base(serverAddress, serverPort)
        {
        }
    }
}
