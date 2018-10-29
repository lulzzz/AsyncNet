using System;

namespace AsyncNet.Core
{
    public class FrameArrivedEventArgs : EventArgs
    {
        public FrameArrivedEventArgs(FrameArrivedData frameArrivedData)
        {
            this.FrameArrivedData = frameArrivedData;
        }

        public FrameArrivedData FrameArrivedData { get; }
    }
}
