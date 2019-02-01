using System;

namespace AsyncNet.Udp.Remote.SystemEvent
{
    public class UdpPacketArrivedEventArgs : EventArgs
    {
        public UdpPacketArrivedEventArgs(UdpPacketArrivedData udpPacketArrivedData)
        {
            this.UdpPacketArrivedData = udpPacketArrivedData;
        }

        public UdpPacketArrivedData UdpPacketArrivedData { get; }
    }
}
