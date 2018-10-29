using System;

namespace AsyncNet.Core
{
    public class ConnectionEstablishedEventArgs : EventArgs
    {
        public ConnectionEstablishedEventArgs(ConnectionEstablishedData connectionEstablishedData)
        {
            this.ConnectionEstablishedData = connectionEstablishedData;
        }

        public ConnectionEstablishedData ConnectionEstablishedData { get; }
    }
}
