using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Tcp.Connection;
using AsyncNet.Tcp.Connection.SystemEvent;
using AsyncNet.Tcp.Remote.SystemEvent;

namespace AsyncNet.Tcp.Remote
{
    public interface IRemoteTcpPeer : IDisposable
    {
        event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;

        IObservable<ConnectionClosedData> WhenConnectionClosed { get; }

        IObservable<TcpFrameArrivedData> WhenFrameArrived { get; }

        /// <summary>
        /// Your custom object will be disposed with remote peer
        /// </summary>
        IDisposable CustomObject { get; set; }

        IPEndPoint IPEndPoint { get; }

        Stream TcpStream { get; }

        /// <summary>
        /// You should use <see cref="TcpStream"/> instead of TcpClient.GetStream()
        /// </summary>
        TcpClient TcpClient { get; }

        void Disconnect(ConnectionCloseReason reason);

        bool Post(byte[] data);

        bool Post(byte[] data, int offset, int count);

        Task<bool> SendAsync(byte[] data);

        Task<bool> SendAsync(byte[] data, CancellationToken cancellationToken);

        Task<bool> SendAsync(byte[] data, int offset, int count);

        Task<bool> SendAsync(byte[] data, int offset, int count, CancellationToken cancellationToken);
    }
}
