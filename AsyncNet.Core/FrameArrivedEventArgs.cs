using System;

namespace AsyncNet.Core
{
    public class FrameArrivedEventArgs : EventArgs
    {
        public FrameArrivedEventArgs(TransportData transportData)
        {
            this.TransportData = transportData;
        }

        public TransportData TransportData { get; }
    }
}
