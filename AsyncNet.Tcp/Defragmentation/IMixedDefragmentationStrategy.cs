namespace AsyncNet.Tcp.Defragmentation
{
    /// <summary>
    /// Implement this interface if you want to support your custom frame defragmentation / deframing strategy
    /// </summary>
    public interface IMixedDefragmentationStrategy
    {
        /// <summary>
        /// Default length of buffer for read operation
        /// </summary>
        int ReadBufferLength { get; }

        /// <summary>
        /// Read type <see cref="MixedDefragmentationStrategyReadType"/>
        /// </summary>
        MixedDefragmentationStrategyReadType ReadType { get; }

        /// <summary>
        /// Entire frame length including any delimiters / headers / prefixes / suffixes
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="dataLength">Total data length in buffer</param>
        /// <returns>Frame length or 0 if frame length could not be determined, -1 if frame should be dropped</returns>
        int GetFrameLength(byte[] buffer, int dataLength);
    }
}
