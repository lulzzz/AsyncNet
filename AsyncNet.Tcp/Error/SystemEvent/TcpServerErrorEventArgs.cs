using System;
using AsyncNet.Core.Error;

namespace AsyncNet.Tcp.Error.SystemEvent
{
    public class TcpServerErrorEventArgs : EventArgs
    {
        public TcpServerErrorEventArgs(ErrorData errorData)
        {
            this.ErrorData = errorData;
        }

        public ErrorData ErrorData { get; }
    }
}
