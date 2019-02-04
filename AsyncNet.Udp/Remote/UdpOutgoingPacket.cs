using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Core;

namespace AsyncNet.Udp.Remote
{
    public class UdpOutgoingPacket
    {
        public UdpOutgoingPacket(
            IPEndPoint remoteEndPoint,
            AsyncNetBuffer buffer,
            CancellationToken cancellationToken)
        {
            this.RemoteEndPoint = remoteEndPoint;
            this.Buffer = buffer;
            this.SendTaskCompletionSource = new TaskCompletionSource<bool>();
            this.CancellationToken = cancellationToken;
        }

        public IPEndPoint RemoteEndPoint { get; }

        public AsyncNetBuffer Buffer { get; }

        public CancellationToken CancellationToken { get; }

        public TaskCompletionSource<bool> SendTaskCompletionSource { get; }
    }
}
