using System.Net;
using AsyncNet.Core;

namespace AsyncNet.Udp.Remote
{
    public class UdpOutgoingPacket
    {
        public UdpOutgoingPacket(
            IPEndPoint remoteEndPoint,
            AsyncNetBuffer buffer)
        {
            this.RemoteEndPoint = remoteEndPoint;
            this.Buffer = buffer;
        }

        public IPEndPoint RemoteEndPoint { get; }

        public AsyncNetBuffer Buffer { get; }
    }
}
