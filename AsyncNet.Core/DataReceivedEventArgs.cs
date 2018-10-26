using System;

namespace AsyncNet.Core
{
    public class DataReceivedEventArgs : EventArgs
    {
        public DataReceivedEventArgs(TransportData transportData)
        {
            this.TransportData = transportData;
        }

        public TransportData TransportData { get; }
    }
}
