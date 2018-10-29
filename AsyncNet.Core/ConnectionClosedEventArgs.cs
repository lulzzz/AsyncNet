using System;

namespace AsyncNet.Core
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
