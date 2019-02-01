using System;

namespace AsyncNet.Tcp.Client.SystemEvent
{
    public class TcpClientStoppedEventArgs : EventArgs
    {
        public TcpClientStoppedEventArgs(TcpClientStoppedData tcpClientStoppedData)
        {
            this.TcpClientStoppedData = tcpClientStoppedData;
        }

        public TcpClientStoppedData TcpClientStoppedData { get; }
    }
}
