using System.Net;

namespace AsyncNet.Udp.Remote
{
    public class UdpPacketArrivedData
    {
        public UdpPacketArrivedData(IPEndPoint remoteEndPoint, byte[] packetData)
        {
            this.RemoteEndPoint = remoteEndPoint;
            this.PacketData = packetData;
        }

        public IPEndPoint RemoteEndPoint { get; }

        public byte[] PacketData { get; }
    }
}
