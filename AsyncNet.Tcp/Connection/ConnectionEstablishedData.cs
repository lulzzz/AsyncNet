using AsyncNet.Core.Remote;
using AsyncNet.Tcp.Remote;

namespace AsyncNet.Tcp.Connection
{
    public class ConnectionEstablishedData : ITcpRemoteContext
    {
        public ConnectionEstablishedData(IRemoteTcpPeer remoteTcpPeer)
        {
            this.RemoteTcpPeer = remoteTcpPeer;
        }

        public IRemoteTcpPeer RemoteTcpPeer { get; }
    }
}
