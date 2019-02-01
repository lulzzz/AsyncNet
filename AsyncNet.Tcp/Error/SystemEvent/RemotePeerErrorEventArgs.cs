using System;

namespace AsyncNet.Tcp.Error.SystemEvent
{
    public class RemoteTcpPeerErrorEventArgs : EventArgs
    {
        public RemoteTcpPeerErrorEventArgs(RemoteTcpPeerErrorData errorData)
        {
            this.ErrorData = errorData;
        }

        public RemoteTcpPeerErrorData ErrorData { get; }
    }
}
