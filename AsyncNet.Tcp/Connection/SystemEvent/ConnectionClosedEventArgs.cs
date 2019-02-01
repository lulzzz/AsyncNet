using System;

namespace AsyncNet.Tcp.Connection.SystemEvent
{
    public class ConnectionClosedEventArgs : EventArgs
    {
        public ConnectionClosedEventArgs(ConnectionClosedData connectionClosedData)
        {
            this.ConnectionClosedData = connectionClosedData;
        }

        public ConnectionClosedData ConnectionClosedData { get; }
    }
}
