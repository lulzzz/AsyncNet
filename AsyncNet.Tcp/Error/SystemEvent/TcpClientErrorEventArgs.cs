using System;
using AsyncNet.Core.Error;

namespace AsyncNet.Tcp.Error.SystemEvent
{
    public class TcpClientErrorEventArgs : EventArgs
    {
        public TcpClientErrorEventArgs(ErrorData errorData)
        {
            this.ErrorData = errorData;
        }

        public ErrorData ErrorData { get; }
    }
}
