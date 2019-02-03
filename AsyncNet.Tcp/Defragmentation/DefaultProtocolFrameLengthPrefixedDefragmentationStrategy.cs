namespace AsyncNet.Tcp.Defragmentation
{
    public class DefaultProtocolFrameLengthPrefixedDefragmentationStrategy : ILengthPrefixedDefragmentationStrategy
    {
        public virtual int FrameHeaderLength { get; protected set; } = sizeof(short);

        public virtual int GetFrameLength(byte[] data)
        {
            if (data.Length < sizeof(short))
                return 0;

            return (data[0] << 8) | (data[1]);
        }
    }
}
