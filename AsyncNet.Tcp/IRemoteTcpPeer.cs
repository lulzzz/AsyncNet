using AsyncNet.Core;

namespace AsyncNet.Tcp
{
    public interface IRemoteTcpPeer : IRemotePeer
    {
        void Disconnect();
    }
}
