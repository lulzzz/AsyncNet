using System.Threading;
using AsyncNet.Core;

namespace AsyncNet.Tcp.Remote
{
    public class RemoteTcpPeerOutgoingMessage
    {
        public RemoteTcpPeerOutgoingMessage(
            RemoteTcpPeer remoteTcpPeer,
            AsyncNetBuffer buffer,
            CancellationToken cancellationToken)
        {
            this.RemoteTcpPeer = remoteTcpPeer;
            this.Buffer = buffer;
            this.CancellationToken = cancellationToken;
        }

        public RemoteTcpPeer RemoteTcpPeer { get; }

        public AsyncNetBuffer Buffer { get; }

        public CancellationToken CancellationToken { get; }
    }
}
