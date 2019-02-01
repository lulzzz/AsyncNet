using AsyncNet.Core.Remote;
using AsyncNet.Tcp.Remote;

namespace AsyncNet.Tcp.Connection
{
    public class ConnectionClosedData : ITcpRemoteContext
    {
        public ConnectionClosedData(IRemoteTcpPeer remoteTcpPeer, ConnectionCloseReason connectionCloseReason)
        {
            this.RemoteTcpPeer = remoteTcpPeer;
            this.ConnectionCloseReason = connectionCloseReason;
        }

        public IRemoteTcpPeer RemoteTcpPeer { get; }

        public ConnectionCloseReason ConnectionCloseReason { get; }
    }
}
