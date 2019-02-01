using System;

namespace AsyncNet.Udp.Error.SystemEvent
{
    public class UdpSendErrorEventArgs : EventArgs
    {
        public UdpSendErrorEventArgs(UdpSendErrorData udpSendErrorData)
        {
            this.UdpSendErrorData = udpSendErrorData;
        }

        public UdpSendErrorData UdpSendErrorData { get; }
    }
}
