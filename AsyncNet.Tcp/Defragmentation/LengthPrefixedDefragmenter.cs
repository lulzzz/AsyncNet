using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Core;

namespace AsyncNet.Tcp.Defragmentation
{
    public class LengthPrefixedDefragmenter : IProtocolFrameDefragmenter
    {
        private readonly ILengthPrefixedDefragmentationStrategy strategy;
        private readonly byte[] readBuffer;

        public LengthPrefixedDefragmenter(ILengthPrefixedDefragmentationStrategy strategy)
        {
            this.strategy = strategy;
            this.readBuffer = new byte[strategy.FrameHeaderLength];
        }

        public async Task<ReadFrameResult> ReadFrameAsync(Stream stream, byte[] leftOvers, CancellationToken cancellationToken)
        {
            bool open;
            int frameLength;

            open = await stream.ReadUntilBufferIsFullAsync(this.readBuffer, 0, this.readBuffer.Length, cancellationToken)
                .ConfigureAwait(false);

            if (!open)
            {
                return ReadFrameResult.StreamClosedResult;
            }

            try
            {
                frameLength = this.strategy.GetFrameLength(this.readBuffer);
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
            Array.Copy(this.readBuffer, 0, frameBuffer, 0, this.readBuffer.Length);

            open = await stream.ReadUntilBufferIsFullAsync(frameBuffer, this.readBuffer.Length, frameLength - this.readBuffer.Length, cancellationToken)
                .ConfigureAwait(false);

            if (!open)
            {
                return ReadFrameResult.StreamClosedResult;
            }

            return new ReadFrameResult(frameBuffer);
        }
    }
}
