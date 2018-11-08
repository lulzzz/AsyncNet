using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Core;

namespace AsyncNet.Tcp.Defragmentation
{
    public class MixedDefragmenter : IProtocolFrameDefragmenter
    {
        private static readonly byte[] emptyArray = new byte[0];
        private readonly IMixedDefragmentationStrategy strategy;
        private readonly int readBufferLength;

        public MixedDefragmenter(IMixedDefragmentationStrategy strategy)
        {
            this.strategy = strategy;
            this.readBufferLength = strategy.ReadBufferLength;
        }

        public async Task<ReadFrameResult> ReadFrameAsync(RemoteTcpPeer remoteTcpPeer, byte[] leftOvers, CancellationToken cancellationToken)
        {
            byte[] frameBuffer;
            int readLength;
            int frameLength = 0;
            int dataLength = leftOvers?.Length ?? 0;

            frameBuffer = leftOvers = leftOvers ?? MixedDefragmenter.emptyArray;

            if (dataLength > 0)
            {
                try
                {
                    frameLength = this.strategy.GetFrameLength(frameBuffer, dataLength);
                }
                catch (Exception ex)
                {
                    throw new AsyncNetUnhandledException(nameof(this.strategy.GetFrameLength), ex);
                }
            }

            while (frameLength == 0)
            {
                try
                {
                    frameBuffer = new byte[dataLength + this.readBufferLength];

                    if (dataLength > 0)
                    {
                        Array.Copy(leftOvers, 0, frameBuffer, 0, dataLength);
                    }

                    leftOvers = frameBuffer;
                }
                catch (Exception ex)
                {
                    throw new AsyncNetUnhandledException(nameof(this.strategy.GetFrameLength), ex);
                }

                readLength = await remoteTcpPeer.TcpStream.ReadWithRealCancellationAsync(frameBuffer, dataLength, this.readBufferLength, cancellationToken)
                    .ConfigureAwait(false);

                if (readLength < 1)
                {
                    return ReadFrameResult.StreamClosedResult;
                }

                dataLength += readLength;

                try
                {
                    frameLength = this.strategy.GetFrameLength(frameBuffer, dataLength);
                }
                catch (Exception ex)
                {
                    throw new AsyncNetUnhandledException(nameof(this.strategy.GetFrameLength), ex);
                }

                if (frameLength < 0)
                {
                    return ReadFrameResult.FrameDroppedResult;
                }
            }

            if (dataLength < frameLength)
            {
                if (frameBuffer.Length < frameLength)
                {
                    try
                    {
                        frameBuffer = new byte[frameLength];
                        Array.Copy(leftOvers, 0, frameBuffer, 0, dataLength);
                    }
                    catch (Exception ex)
                    {
                        throw new AsyncNetUnhandledException(nameof(this.strategy.GetFrameLength), ex);
                    }
                }

                var open = await remoteTcpPeer.TcpStream.ReadUntilBufferIsFullAsync(frameBuffer, dataLength, frameLength - dataLength, cancellationToken)
                    .ConfigureAwait(false);

                if (!open)
                {
                    return ReadFrameResult.StreamClosedResult;
                }

                dataLength = frameLength;
                leftOvers = null;
            }
            else if (dataLength > frameLength)
            {
                var frameData = new byte[frameLength];
                var leftOversLength = dataLength - frameLength;

                leftOvers = new byte[leftOversLength];

                Array.Copy(frameBuffer, 0, frameData, 0, frameLength);
                Array.Copy(frameBuffer, frameLength, leftOvers, 0, leftOversLength);
            }
            else
            {
                if (frameBuffer.Length > dataLength)
                {
                    frameBuffer = new byte[dataLength];
                    Array.Copy(leftOvers, 0, frameBuffer, 0, dataLength);
                }

                leftOvers = null;
            }

            return new ReadFrameResult(frameBuffer, leftOvers);
        }
    }
}
