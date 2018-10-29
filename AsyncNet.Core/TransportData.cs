namespace AsyncNet.Core
{
    public class TransportData : ITransportContext
    {
        public TransportData(IRemotePeer remotePeer, byte[] frameData)
        {
            this.RemotePeer = remotePeer;
            this.FrameData = frameData;
        }

        public IRemotePeer RemotePeer { get; }

        public byte[] FrameData { get; }
    }
}
