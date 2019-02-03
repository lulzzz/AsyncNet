namespace AsyncNet.Tcp.Defragmentation
{
    public class ReadFrameResult
    {
        public ReadFrameResult(byte[] frameData)
        {
            this.ReadFrameStatus = ReadFrameStatus.Success;
            this.FrameData = frameData;
            this.LeftOvers = null;
        }

        public ReadFrameResult(byte[] frameData, byte[] leftOvers)
        {
            this.ReadFrameStatus = ReadFrameStatus.Success;
            this.FrameData = frameData;
            this.LeftOvers = leftOvers;
        }

        public ReadFrameResult(ReadFrameStatus readFrameStatus)
        {
            this.ReadFrameStatus = readFrameStatus;
            this.FrameData = null;
            this.LeftOvers = null;
        }

        public static ReadFrameResult StreamClosedResult { get; } = new ReadFrameResult(ReadFrameStatus.StreamClosed);

        public static ReadFrameResult FrameDroppedResult { get; } = new ReadFrameResult(ReadFrameStatus.FrameDropped);

        public ReadFrameStatus ReadFrameStatus { get; }

        public byte[] FrameData { get; }

        public byte[] LeftOvers { get; }
    }
}
