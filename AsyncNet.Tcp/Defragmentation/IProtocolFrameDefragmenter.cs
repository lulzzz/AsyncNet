using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Tcp.Remote;

namespace AsyncNet.Tcp.Defragmentation
{
    /// <summary>
    /// An interface for protocol frame defragmenter. You can implement this interface to support any defragmentation / deframing mechanism
    /// </summary>
    public interface IProtocolFrameDefragmenter
    {
        /// <summary>
        /// Reads one frame from the stream
        /// </summary>
        /// <param name="remoteTcpPeer">Remote peer</param>
        /// <param name="leftOvers">Any left overs from previous call or null</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns>Frame result</returns>
        Task<ReadFrameResult> ReadFrameAsync(
            IRemoteTcpPeer remoteTcpPeer,
            byte[] leftOvers,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// An interface for protocol frame defragmenter. You can implement this interface to support any defragmentation / deframing mechanism
    /// </summary>
    /// <typeparam name="TStrategy"></typeparam>
    public interface IProtocolFrameDefragmenter<TStrategy> : IProtocolFrameDefragmenter
    {
        /// <summary>
        /// Current defragmentation strategy
        /// </summary>
        TStrategy DefragmentationStrategy { get; set; }
    }
}
