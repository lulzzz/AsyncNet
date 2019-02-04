using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Core.Extensions;
using AsyncNet.Tcp.Remote;

namespace AsyncNet.Tcp.Defragmentation
{
    /// <summary>
    /// Length prefixed protocol frame defragmenter
    /// </summary>
    public class LengthPrefixedDefragmenter : IProtocolFrameDefragmenter<ILengthPrefixedDefragmentationStrategy>
    {
        private static readonly Lazy<LengthPrefixedDefragmenter> @default = new Lazy<LengthPrefixedDefragmenter>(() => new LengthPrefixedDefragmenter(new DefaultProtocolFrameLengthPrefixedDefragmentationStrategy()));

        /// <summary>
        /// Default length prefixed defragmenter using <see cref="DefaultProtocolFrameLengthPrefixedDefragmentationStrategy"/>
        /// </summary>
        public static LengthPrefixedDefragmenter Default => @default.Value;

        /// <summary>
        /// Current length prefixed defragmentation strategy
        /// </summary>
        public virtual ILengthPrefixedDefragmentationStrategy DefragmentationStrategy { get; set; }

        /// <summary>
        /// Constructs length prefixed defragmenter that is using <paramref name="strategy"/> for defragmentation strategy
        /// </summary>
        /// <param name="strategy"></param>
        public LengthPrefixedDefragmenter(ILengthPrefixedDefragmentationStrategy strategy)
        {
            this.DefragmentationStrategy = strategy;
        }

        /// <summary>
        /// Reads one frame from the stream
        /// </summary>
        /// <param name="remoteTcpPeer">Remote peer</param>
        /// <param name="leftOvers">Any left overs from previous call or null</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Frame result</returns>
        public virtual async Task<ReadFrameResult> ReadFrameAsync(IRemoteTcpPeer remoteTcpPeer, byte[] leftOvers, CancellationToken cancellationToken)
        {
            bool open;
            int frameLength;
            var readBuffer = new byte[this.DefragmentationStrategy.FrameHeaderLength];

            open = await remoteTcpPeer.TcpStream.ReadUntilBufferIsFullAsync(readBuffer, 0, readBuffer.Length, cancellationToken)
                .ConfigureAwait(false);

            if (!open)
            {
                return ReadFrameResult.StreamClosedResult;
            }

            frameLength = this.DefragmentationStrategy.GetFrameLength(readBuffer);

            if (frameLength < 1)
            {
                return ReadFrameResult.FrameDroppedResult;
            }

            var frameBuffer = new byte[frameLength];
            Array.Copy(readBuffer, 0, frameBuffer, 0, readBuffer.Length);

            open = await remoteTcpPeer.TcpStream.ReadUntilBufferIsFullAsync(frameBuffer, readBuffer.Length, frameLength - readBuffer.Length, cancellationToken)
                .ConfigureAwait(false);

            if (!open)
            {
                return ReadFrameResult.StreamClosedResult;
            }

            return new ReadFrameResult(frameBuffer);
        }
    }
}
