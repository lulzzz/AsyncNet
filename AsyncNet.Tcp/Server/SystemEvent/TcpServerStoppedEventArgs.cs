using System;

namespace AsyncNet.Tcp.Server.SystemEvent
{
    public class TcpServerStoppedEventArgs : EventArgs
    {
        public TcpServerStoppedEventArgs(TcpServerStoppedData tcpServerStoppedData)
        {
            this.TcpServerStoppedData = tcpServerStoppedData;
        }

        public TcpServerStoppedData TcpServerStoppedData { get; }
    }
}
