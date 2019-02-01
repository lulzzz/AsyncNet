using System;

namespace AsyncNet.Udp.Client.SystemEvent
{
    public class UdpClientStoppedEventArgs : EventArgs
    {
        public UdpClientStoppedEventArgs(UdpClientStoppedData udpClientStoppedData)
        {
            this.UdpClientStoppedData = udpClientStoppedData;
        }

        public UdpClientStoppedData UdpClientStoppedData { get; }
    }
}
