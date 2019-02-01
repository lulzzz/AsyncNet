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
    public class RemoteTcpPeer : IRemoteTcpPeer
    {
        private readonly Lazy<IProtocolFrameDefragmenter> lazyProtocolFrameDefragmenter;
        private readonly ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue;
        private readonly CancellationTokenSource cancellationTokenSource;
        private ConnectionCloseReason connectionCloseReason;

        public RemoteTcpPeer(
            Func<RemoteTcpPeer, IProtocolFrameDefragmenter> protocolFrameDefragmenterFactory,
            TcpClient tcpClient,
            ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue,
            CancellationTokenSource cts)
        {
            this.lazyProtocolFrameDefragmenter = new Lazy<IProtocolFrameDefragmenter>(() => protocolFrameDefragmenterFactory(this), false);
            this.TcpClient = tcpClient;
            this.TcpStream = tcpClient.GetStream();
            this.IPEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
            this.sendQueue = sendQueue;
            this.cancellationTokenSource = cts;
        }

        public RemoteTcpPeer(
            Func<RemoteTcpPeer, IProtocolFrameDefragmenter> protocolFrameDefragmenterFactory,
            TcpClient tcpClient,
            Stream tcpStream,
            ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue,
            CancellationTokenSource cts)
        {
            this.lazyProtocolFrameDefragmenter = new Lazy<IProtocolFrameDefragmenter>(() => protocolFrameDefragmenterFactory(this), false);
            this.TcpClient = tcpClient;
            this.TcpStream = tcpStream;
            this.IPEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
            this.sendQueue = sendQueue;
            this.cancellationTokenSource = cts;
        }

        public event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;

        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        public TcpClient TcpClient { get; }

        public Stream TcpStream { get; }

        public IPEndPoint IPEndPoint { get; }

        public IObservable<TcpFrameArrivedData> WhenFrameArrived => Observable.FromEventPattern<TcpFrameArrivedEventArgs>(
                h => this.FrameArrived += h,
                h => this.FrameArrived -= h)
            .TakeUntil(this.WhenConnectionClosed)
            .Select(x => x.EventArgs.TcpFrameArrivedData);

        public IObservable<ConnectionClosedData> WhenConnectionClosed => Observable.FromEventPattern<ConnectionClosedEventArgs>(
                h => this.ConnectionClosed += h,
                h => this.ConnectionClosed -= h)
            .Select(x => x.EventArgs.ConnectionClosedData);

        public virtual IDisposable CustomObject { get; set; }

        public virtual Task<bool> SendAsync(byte[] data)
        {
            return this.SendAsync(data, CancellationToken.None);
        }

        public virtual Task<bool> SendAsync(byte[] data, CancellationToken cancellationToken)
        {
            return this.SendAsync(data, 0, data.Length, cancellationToken);
        }

        public virtual Task<bool> SendAsync(byte[] data, int offset, int count)
        {
            return this.SendAsync(data, offset, count, CancellationToken.None);
        }

        public virtual async Task<bool> SendAsync(byte[] data, int offset, int count, CancellationToken cancellationToken)
        {
            bool result;

            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(this.cancellationTokenSource.Token, cancellationToken))
            {
                try
                {
                    result = await this.sendQueue.SendAsync(
                        new RemoteTcpPeerOutgoingMessage(
                            this,
                            new AsyncNetBuffer(data, offset, count),
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

        public virtual bool Post(byte[] data) => this.Post(data, 0, data.Length);

        public virtual bool Post(byte[] data, int offset, int count)
        {
            return this.sendQueue.Post(new RemoteTcpPeerOutgoingMessage(
                            this,
                            new AsyncNetBuffer(data, offset, count),
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

        public virtual IProtocolFrameDefragmenter ProtocolFrameDefragmenter => this.lazyProtocolFrameDefragmenter.Value;

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
