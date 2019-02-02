using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AsyncNet.Core;
using AsyncNet.Tcp.Connection;
using AsyncNet.Tcp.Connection.SystemEvent;
using AsyncNet.Tcp.Defragmentation;
using AsyncNet.Tcp.Remote.SystemEvent;

namespace AsyncNet.Tcp.Remote
{
    /// <summary>
    /// An implementation of remote tcp peer
    /// </summary>
    public class RemoteTcpPeer : IRemoteTcpPeer
    {
        private readonly ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue;
        private readonly CancellationTokenSource cancellationTokenSource;

        private ConnectionCloseReason connectionCloseReason;
        private Func<IRemoteTcpPeer, IProtocolFrameDefragmenter> protocolFrameDefragmenterFactory;

        public RemoteTcpPeer(
            Func<IRemoteTcpPeer, IProtocolFrameDefragmenter> protocolFrameDefragmenterFactory,
            TcpClient tcpClient,
            ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue,
            CancellationTokenSource cts)
        {
            this.protocolFrameDefragmenterFactory = protocolFrameDefragmenterFactory;
            this.TcpClient = tcpClient;
            this.TcpStream = tcpClient.GetStream();
            this.IPEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
            this.sendQueue = sendQueue;
            this.cancellationTokenSource = cts;
        }

        public RemoteTcpPeer(
            Func<IRemoteTcpPeer, IProtocolFrameDefragmenter> protocolFrameDefragmenterFactory,
            TcpClient tcpClient,
            Stream tcpStream,
            ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue,
            CancellationTokenSource cts)
        {
            this.protocolFrameDefragmenterFactory = protocolFrameDefragmenterFactory;
            this.TcpClient = tcpClient;
            this.TcpStream = tcpStream;
            this.IPEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
            this.sendQueue = sendQueue;
            this.cancellationTokenSource = cts;
        }

        /// <summary>
        /// Fires when TCP frame from this client/peer arrived
        /// </summary>
        public event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;

        /// <summary>
        /// Fires when connection with this client/peer closes
        /// </summary>
        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        /// <summary>
        /// Underlying <see cref="TcpClient" />. You should use <see cref="P:AsyncNet.Tcp.Remote.IRemoteTcpPeer.TcpStream" /> instead of TcpClient.GetStream()
        /// </summary>
        public TcpClient TcpClient { get; }

        /// <summary>
        /// Tcp stream
        /// </summary>
        public Stream TcpStream { get; }

        /// <summary>
        /// Remote tcp peer endpoint
        /// </summary>
        public IPEndPoint IPEndPoint { get; }

        /// <summary>
        /// Produces an element when TCP frame from this client/peer arrived
        /// </summary>
        public IObservable<TcpFrameArrivedData> WhenFrameArrived => Observable.FromEventPattern<TcpFrameArrivedEventArgs>(
                h => this.FrameArrived += h,
                h => this.FrameArrived -= h)
            .TakeUntil(this.WhenConnectionClosed)
            .Select(x => x.EventArgs.TcpFrameArrivedData);

        /// <summary>
        /// Produces an element when connection with this client/peer closes
        /// </summary>
        public IObservable<ConnectionClosedData> WhenConnectionClosed => Observable.FromEventPattern<ConnectionClosedEventArgs>(
                h => this.ConnectionClosed += h,
                h => this.ConnectionClosed -= h)
            .Select(x => x.EventArgs.ConnectionClosedData);

        /// <summary>
        /// You can set it to your own custom object that implements <see cref="IDisposable" />. Your custom object will be disposed with this remote peer
        /// </summary>
        public virtual IDisposable CustomObject { get; set; }

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <returns>True - added to the send queue. False - this client/peer is disconnected</returns>
        public virtual Task<bool> SendAsync(byte[] data)
        {
            return this.SendAsync(data, CancellationToken.None);
        }

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns>True - added to the send queue. False - this client/peer is disconnected</returns>
        public virtual Task<bool> SendAsync(byte[] data, CancellationToken cancellationToken)
        {
            return this.SendAsync(data, 0, data.Length, cancellationToken);
        }

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <returns>True - added to the send queue. False - send queue buffer is full or this client/peer is disconnected</returns>
        public virtual Task<bool> SendAsync(byte[] buffer, int offset, int count)
        {
            return this.SendAsync(buffer, offset, count, CancellationToken.None);
        }

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns>True - added to the send queue. False - send queue buffer is full or this client/peer is disconnected</returns>
        public virtual async Task<bool> SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            bool result;

            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(this.cancellationTokenSource.Token, cancellationToken))
            {
                try
                {
                    result = await this.sendQueue.SendAsync(
                        new RemoteTcpPeerOutgoingMessage(
                            this,
                            new AsyncNetBuffer(buffer, offset, count),
                            this.cancellationTokenSource.Token),
                        linkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    result = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Adds data to the send queue. It will fail if send queue buffer is full returning false
        /// </summary>
        /// <param name="data"></param>
        /// <returns>True - added to the send queue. False - send queue buffer is full or this client/peer is disconnected</returns>
        public virtual bool Post(byte[] data) => this.Post(data, 0, data.Length);

        /// <summary>
        /// Adds data to the send queue. It will fail if send queue buffer is full returning false
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <returns>True - added to the send queue. False - send queue buffer is full or this client/peer is disconnected</returns>
        public virtual bool Post(byte[] buffer, int offset, int count)
        {
            return this.sendQueue.Post(new RemoteTcpPeerOutgoingMessage(
                            this,
                            new AsyncNetBuffer(buffer, offset, count),
                            this.cancellationTokenSource.Token));
        }

        public virtual void Dispose()
        {
            this.Disconnect(this.ConnectionCloseReason);

            try
            {
                this.CustomObject?.Dispose();
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// Disconnects this peer/client
        /// </summary>
        /// <param name="reason">Disconnect reason</param>
        public virtual void Disconnect(ConnectionCloseReason reason)
        {
            try
            {
                this.connectionCloseReason = reason;
                this.cancellationTokenSource.Cancel();
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// Changes the protocol frame defragmenter used for TCP deframing/defragmentation
        /// </summary>
        /// <param name="protocolFrameDefragmenterFactory">Factory for constructing <see cref="IProtocolFrameDefragmenter" /></param>
        public virtual void SwitchProtocol(Func<IRemoteTcpPeer, IProtocolFrameDefragmenter> protocolFrameDefragmenterFactory)
        {
            this.protocolFrameDefragmenterFactory = protocolFrameDefragmenterFactory;
        }

        public virtual IProtocolFrameDefragmenter ProtocolFrameDefragmenter => this.protocolFrameDefragmenterFactory(this);

        public virtual ConnectionCloseReason ConnectionCloseReason
        {
            get => this.connectionCloseReason;
            set => this.connectionCloseReason = this.connectionCloseReason != ConnectionCloseReason.NoReason ? this.connectionCloseReason : value;
        }

        public virtual void OnFrameArrived(TcpFrameArrivedEventArgs e)
        {
            this.FrameArrived?.Invoke(this, e);
        }

        public virtual void OnConnectionClosed(ConnectionClosedEventArgs e)
        {
            this.ConnectionClosed?.Invoke(this, e);
        }
    }
}
