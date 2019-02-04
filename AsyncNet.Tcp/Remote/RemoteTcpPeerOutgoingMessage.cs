using System.Threading;
using System.Threading.Tasks;
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
            this.SendTaskCompletionSource = new TaskCompletionSource<bool>();
        }

        public RemoteTcpPeer RemoteTcpPeer { get; }

        public AsyncNetBuffer Buffer { get; }

        public CancellationToken CancellationToken { get; }

        public TaskCompletionSource<bool> SendTaskCompletionSource { get; }
    }
}
