using System;

namespace AsyncNet.Udp.Server.SystemEvent
{
    public class UdpServerStoppedEventArgs : EventArgs
    {
        public UdpServerStoppedEventArgs(UdpServerStoppedData udpServerStoppedData)
        {
            this.UdpServerStoppedData = udpServerStoppedData;
        }

        public UdpServerStoppedData UdpServerStoppedData { get; }
    }
}
