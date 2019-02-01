using AsyncNet.Tcp.Remote;

namespace AsyncNet.Core.Remote
{
    public interface ITcpRemoteContext
    {
        IRemoteTcpPeer RemoteTcpPeer { get; }
    }
}
