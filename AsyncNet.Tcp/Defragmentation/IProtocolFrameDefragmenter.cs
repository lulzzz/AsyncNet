using System.Threading;
using System.Threading.Tasks;

namespace AsyncNet.Tcp.Defragmentation
{
    /// <summary>
    /// Frame defragmenter. You can implement this interface to support any defragmentation / deframing
    /// </summary>
    public interface IProtocolFrameDefragmenter
    {
        /// <summary>
        /// Reads one frame from the stream
        /// </summary>
        /// <param name="remoteTcpPeer">Remote peer</param>
        /// <param name="leftOvers">Any left overs from previous call or null</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Frame result</returns>
        Task<ReadFrameResult> ReadFrameAsync(
            RemoteTcpPeer remoteTcpPeer,
            byte[] leftOvers,
            CancellationToken cancellationToken);
    }
}
