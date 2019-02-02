using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Core.Exceptions;
using AsyncNet.Core.Extensions;
using AsyncNet.Tcp.Remote;

namespace AsyncNet.Tcp.Defragmentation
{
    /// <summary>
    /// Length prefixed protocol frame defragmenter
    /// </summary>
    public class LengthPrefixedDefragmenter : IProtocolFrameDefragmenter
    {
        private readonly ILengthPrefixedDefragmentationStrategy strategy;
        private readonly int frameHeaderLength;

        /// <summary>
        /// Constructs length prefixed defragmenter that is using <paramref name="strategy"/> for defragmentation strategy
        /// </summary>
        /// <param name="strategy"></param>
        public LengthPrefixedDefragmenter(ILengthPrefixedDefragmentationStrategy strategy)
        {
            this.strategy = strategy;
            this.frameHeaderLength = strategy.FrameHeaderLength;
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
            var readBuffer = new byte[this.frameHeaderLength];

            open = await remoteTcpPeer.TcpStream.ReadUntilBufferIsFullAsync(readBuffer, 0, readBuffer.Length, cancellationToken)
                .ConfigureAwait(false);

            if (!open)
            {
                return ReadFrameResult.StreamClosedResult;
            }

            try
            {
                frameLength = this.strategy.GetFrameLength(readBuffer);
            }
            catch (Exception ex)
            {
                throw new AsyncNetUnhandledException(nameof(this.strategy.GetFrameLength), ex);
            }

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
