using System.Threading;

namespace AsyncNet.Tcp
{
    public class RemoteTcpPeerOutgoingMessage
    {
        public RemoteTcpPeerOutgoingMessage(
            RemoteTcpPeer remoteTcpPeer, 
            CancellationToken cancellationToken, 
            OutgoingMessage outgoingMessage)
        {
            this.RemoteTcpPeer = remoteTcpPeer;
            this.OutgoingMessage = outgoingMessage;
            this.CancellationToken = cancellationToken;
        }

        public RemoteTcpPeer RemoteTcpPeer { get; }

        public CancellationToken CancellationToken { get; }

        public OutgoingMessage OutgoingMessage { get; }
    }
}
