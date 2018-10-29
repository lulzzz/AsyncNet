namespace AsyncNet.Core
{
    public class FrameArrivedData : ITransportContext
    {
        public FrameArrivedData(IRemotePeer remotePeer, byte[] frameData)
        {
            this.RemotePeer = remotePeer;
            this.FrameData = frameData;
        }

        public IRemotePeer RemotePeer { get; }

        public byte[] FrameData { get; }
    }
}
