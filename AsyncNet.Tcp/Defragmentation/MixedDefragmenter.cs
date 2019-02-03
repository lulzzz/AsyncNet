using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Core.Exceptions;
using AsyncNet.Core.Extensions;
using AsyncNet.Tcp.Remote;

namespace AsyncNet.Tcp.Defragmentation
{
    /// <summary>
    /// Mixed protocol frame defragmenter
    /// </summary>
    public class MixedDefragmenter : IProtocolFrameDefragmenter<IMixedDefragmentationStrategy>
    {
        private static readonly byte[] emptyArray = new byte[0];
        private static readonly Lazy<MixedDefragmenter> @default = new Lazy<MixedDefragmenter>(() => new MixedDefragmenter(new DefaultProtocolFrameMixedDefragmentationStrategy()));

        /// <summary>
        /// Default mixed defragmenter using <see cref="DefaultProtocolFrameMixedDefragmentationStrategy"/>
        /// </summary>
        public static MixedDefragmenter Default => @default.Value;

        /// <summary>
        /// Current mixed defragmentation strategy
        /// </summary>
        public virtual IMixedDefragmentationStrategy DefragmentationStrategy { get; set; }

        /// <summary>
        /// Constructs mixed frame defragmenter that is using <paramref name="strategy"/> for defragmentation strategy
        /// </summary>
        /// <param name="strategy"></param>
        public MixedDefragmenter(IMixedDefragmentationStrategy strategy)
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
            byte[] frameBuffer;
            int readLength;
            int frameLength = 0;
            int dataLength = leftOvers?.Length ?? 0;

            frameBuffer = leftOvers = leftOvers ?? MixedDefragmenter.emptyArray;

            if (dataLength > 0)
            {
                try
                {
                    frameLength = this.DefragmentationStrategy.GetFrameLength(frameBuffer, dataLength);
                }
                catch (Exception ex)
                {
                    throw new AsyncNetUnhandledException(nameof(this.DefragmentationStrategy.GetFrameLength), ex);
                }
            }

            while (frameLength == 0)
            {
                if (this.DefragmentationStrategy.ReadType == MixedDefragmentationStrategyReadType.ReadFully)
                {
                    if (this.DefragmentationStrategy.ReadBufferLength > dataLength)
                    {
                        frameBuffer = new byte[this.DefragmentationStrategy.ReadBufferLength];
                    }
                    else
                    {
                        frameBuffer = new byte[dataLength];
                    }
                }
                else
                {
                    frameBuffer = new byte[dataLength + this.DefragmentationStrategy.ReadBufferLength];
                }

                if (dataLength > 0)
                {
                    Array.Copy(leftOvers, 0, frameBuffer, 0, dataLength);
                }

                leftOvers = frameBuffer;

                if (this.DefragmentationStrategy.ReadType == MixedDefragmentationStrategyReadType.ReadNewFully)
                {
                    var open = await remoteTcpPeer.TcpStream.ReadUntilBufferIsFullAsync(frameBuffer, dataLength, this.DefragmentationStrategy.ReadBufferLength, cancellationToken)
                        .ConfigureAwait(false);

                    if (!open)
                    {
                        readLength = 0;
                    }
                    else
                    {
                        readLength = this.DefragmentationStrategy.ReadBufferLength;
                    }
                }
                else if (this.DefragmentationStrategy.ReadType == MixedDefragmentationStrategyReadType.ReadFully)
                {
                    var open = await remoteTcpPeer.TcpStream.ReadUntilBufferIsFullAsync(frameBuffer, 0, this.DefragmentationStrategy.ReadBufferLength, cancellationToken)
                        .ConfigureAwait(false);

                    if (!open)
                    {
                        readLength = 0;
                    }
                    else
                    {
                        readLength = this.DefragmentationStrategy.ReadBufferLength;
                    }
                }
                else
                {
                    readLength = await remoteTcpPeer.TcpStream.ReadWithRealCancellationAsync(frameBuffer, dataLength, this.DefragmentationStrategy.ReadBufferLength, cancellationToken)
                        .ConfigureAwait(false);
                }

                if (readLength < 1)
                {
                    return ReadFrameResult.StreamClosedResult;
                }

                dataLength += readLength;

                try
                {
                    frameLength = this.DefragmentationStrategy.GetFrameLength(frameBuffer, dataLength);
                }
                catch (Exception ex)
                {
                    throw new AsyncNetUnhandledException(nameof(this.DefragmentationStrategy.GetFrameLength), ex);
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
                    frameBuffer = new byte[frameLength];
                    Array.Copy(leftOvers, 0, frameBuffer, 0, dataLength);
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
