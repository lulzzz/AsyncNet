using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNet.Core
{
    public interface IRemotePeer : IDisposable
    {
        IPEndPoint IPEndPoint { get; }

        Task<bool> SendAsync(byte[] data);

        Task<bool> SendAsync(byte[] data, CancellationToken cancellationToken);

        Task<bool> SendAsync(byte[] data, int offset, int count);

        Task<bool> SendAsync(byte[] data, int offset, int count, CancellationToken cancellationToken);
    }
}
