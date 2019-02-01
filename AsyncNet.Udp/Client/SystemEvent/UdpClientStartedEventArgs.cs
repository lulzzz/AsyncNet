using System;

namespace AsyncNet.Udp.Client.SystemEvent
{
    public class UdpClientStartedEventArgs : EventArgs
    {
        public UdpClientStartedEventArgs(UdpClientStartedData udpClientStartedData)
        {
            this.UdpClientStartedData = udpClientStartedData;
        }

        public UdpClientStartedData UdpClientStartedData { get; }
    }
}
