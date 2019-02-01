using System;

namespace AsyncNet.Tcp.Client.SystemEvent
{
    public class TcpClientStartedEventArgs : EventArgs
    {
        public TcpClientStartedEventArgs(TcpClientStartedData tcpClientStartedData)
        {
            this.TcpClientStartedData = tcpClientStartedData;
        }

        public TcpClientStartedData TcpClientStartedData { get; }
    }
}
