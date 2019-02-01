using System;
using AsyncNet.Core.Error;

namespace AsyncNet.Udp.Error.SystemEvent
{
    public class UdpClientErrorEventArgs : EventArgs
    {
        public UdpClientErrorEventArgs(ErrorData errorData)
        {
            this.ErrorData = errorData;
        }

        public ErrorData ErrorData { get; }
    }
}
