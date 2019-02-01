using System;

namespace AsyncNet.Tcp.Remote.SystemEvent
{
    public class TcpFrameArrivedEventArgs : EventArgs
    {
        public TcpFrameArrivedEventArgs(TcpFrameArrivedData tcpFrameArrivedData)
        {
            this.TcpFrameArrivedData = tcpFrameArrivedData;
        }

        public TcpFrameArrivedData TcpFrameArrivedData { get; }
    }
}
