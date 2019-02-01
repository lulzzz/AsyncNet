using System;

namespace AsyncNet.Tcp.Server.SystemEvent
{
    public class TcpServerStartedEventArgs : EventArgs
    {
        public TcpServerStartedEventArgs(TcpServerStartedData tcpServerStartedData)
        {
            this.TcpServerStartedData = tcpServerStartedData;
        }

        public TcpServerStartedData TcpServerStartedData { get; }
    }
}
