using System.Threading;

namespace AsyncNet.Tcp
{
    public class RemoteTcpPeerOutgoingMessage
    {
        public RemoteTcpPeerOutgoingMessage(
            RemoteTcpPeer remoteTcpPeer, 
            CancellationToken cancellationToken, 
            IOBuffer ioBuffer)
        {
            this.RemoteTcpPeer = remoteTcpPeer;
            this.IOBuffer = ioBuffer;
            this.CancellationToken = cancellationToken;
        }

        public RemoteTcpPeer RemoteTcpPeer { get; }

        public CancellationToken CancellationToken { get; }

        public IOBuffer IOBuffer { get; }
    }
}
