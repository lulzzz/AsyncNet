using System;
using AsyncNet.Core.Error;

namespace AsyncNet.Udp.Error.SystemEvent
{
    public class UdpServerErrorEventArgs : EventArgs
    {
        public UdpServerErrorEventArgs(ErrorData errorData)
        {
            this.ErrorData = errorData;
        }

        public ErrorData ErrorData { get; }
    }
}
