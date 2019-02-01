namespace AsyncNet.Tcp.Remote
{
    public class TcpFrameArrivedData
    {
        public TcpFrameArrivedData(RemoteTcpPeer remoteTcpPeer, byte[] frameData)
        {
            this.RemoteTcpPeer = remoteTcpPeer;
            this.FrameData = frameData;
        }

        public RemoteTcpPeer RemoteTcpPeer { get; }

        public byte[] FrameData { get; }
    }
}
