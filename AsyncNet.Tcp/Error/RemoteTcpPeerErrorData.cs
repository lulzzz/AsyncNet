using System;
using AsyncNet.Core.Error;
using AsyncNet.Core.Remote;
using AsyncNet.Tcp.Remote;

namespace AsyncNet.Tcp.Error
{
    public class RemoteTcpPeerErrorData : ErrorData, ITcpRemoteContext
    {
        public RemoteTcpPeerErrorData(IRemoteTcpPeer remoteTcpPeer, Exception exception) : base(exception)
        {
            this.RemoteTcpPeer = remoteTcpPeer;
        }

        public IRemoteTcpPeer RemoteTcpPeer { get; }
    }
}
