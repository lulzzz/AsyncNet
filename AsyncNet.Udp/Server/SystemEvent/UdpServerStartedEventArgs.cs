using System;

namespace AsyncNet.Udp.Server.SystemEvent
{
    public class UdpServerStartedEventArgs : EventArgs
    {
        public UdpServerStartedEventArgs(UdpServerStartedData udpServerStartedData)
        {
            this.UdpServerStartedData = udpServerStartedData;
        }

        public UdpServerStartedData UdpServerStartedData { get; }
    }
}
