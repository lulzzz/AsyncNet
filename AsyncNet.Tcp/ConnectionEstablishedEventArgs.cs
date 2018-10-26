using System;

namespace AsyncNet.Tcp
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
