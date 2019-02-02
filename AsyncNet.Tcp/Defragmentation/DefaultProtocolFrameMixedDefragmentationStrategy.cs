namespace AsyncNet.Tcp.Defragmentation
{
    public class DefaultProtocolFrameMixedDefragmentationStrategy : IMixedDefragmentationStrategy
    {
        public virtual int ReadBufferLength { get; protected set; } = 4096;

        public virtual int GetFrameLength(byte[] buffer, int dataLength)
        {
            return dataLength;
        }
    }
}
