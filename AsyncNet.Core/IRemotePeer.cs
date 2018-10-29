using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNet.Core
{
    public interface IRemotePeer : IDisposable
    {
        event EventHandler<FrameArrivedEventArgs> FrameArrived;

        event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        IObservable<FrameArrivedData> WhenFrameArrived { get; }

        IObservable<ConnectionClosedData> WhenConnectionClosed { get; }

        IPEndPoint IPEndPoint { get; }

        /// <summary>
        /// Any object assigned to this property will be disposed with the remote peer
        /// </summary>
        IDisposable CustomObject { get; set; }

        Task<bool> SendAsync(byte[] data);

        Task<bool> SendAsync(byte[] data, CancellationToken cancellationToken);

        Task<bool> SendAsync(byte[] data, int offset, int count);

        Task<bool> SendAsync(byte[] data, int offset, int count, CancellationToken cancellationToken);

        bool Post(byte[] data);

        bool Post(byte[] data, int offset, int count);

        void Disconnect(ConnectionCloseReason reason);
    }
}
