using System;

namespace AsyncNet.Udp.Client.SystemEvent
{
    public class UdpClientReadyEventArgs : EventArgs
    {
        public UdpClientReadyEventArgs(UdpClientReadyData udpClientReadyData)
        {
            this.UdpClientReadyData = udpClientReadyData;
        }

        public UdpClientReadyData UdpClientReadyData { get; }
    }
}
