using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Core.Exceptions;
using AsyncNet.Core.Extensions;
using AsyncNet.Tcp.Remote;

namespace AsyncNet.Tcp.Defragmentation
{
    public class LengthPrefixedDefragmenter : IProtocolFrameDefragmenter
    {
        private readonly ILengthPrefixedDefragmentationStrategy strategy;
        private readonly int frameHeaderLength;

        public LengthPrefixedDefragmenter(ILengthPrefixedDefragmentationStrategy strategy)
        {
            this.strategy = strategy;
            this.frameHeaderLength = strategy.FrameHeaderLength;
        }

        public async Task<ReadFrameResult> ReadFrameAsync(RemoteTcpPeer remoteTcpPeer, byte[] leftOvers, CancellationToken cancellationToken)
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
