namespace AsyncNet.Tcp.Defragmentation
{
    public class DefaultProtocolFrameDefragmentationStrategy : IMixedDefragmentationStrategy
    {
        public int ReadBufferLength => 4096;

        public int GetFrameLength(byte[] buffer, int dataLength)
        {
            return dataLength;
        }
    }
}
