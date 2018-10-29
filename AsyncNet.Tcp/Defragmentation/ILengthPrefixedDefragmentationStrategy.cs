namespace AsyncNet.Tcp.Defragmentation
{
    /// <summary>
    /// Implement this interface if you want to support your custom length-prefixed frame defragmentation strategy
    /// </summary>
    public interface ILengthPrefixedDefragmentationStrategy
    {
        /// <summary>
        /// Minimum number of bytes to read from stream so <see cref="GetEntireFrameLength(byte[])"/> wiil be able to determine frame length
        /// </summary>
        int FrameHeaderLength { get; }

        /// <summary>
        /// Entire frame length including header(s)
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Entire frame length including header or 0 if frame should be dropped</returns>
        int GetFrameLength(byte[] data);
    }
}
