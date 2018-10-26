using System;

namespace AsyncNet.Tcp
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
