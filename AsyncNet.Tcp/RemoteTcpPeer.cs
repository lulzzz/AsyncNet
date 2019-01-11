using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AsyncNet.Core;
using AsyncNet.Tcp.Defragmentation;

namespace AsyncNet.Tcp
{
    public class RemoteTcpPeer : IRemotePeer
    {
        private ConnectionCloseReason connectionCloseReason;

        public RemoteTcpPeer(
            IProtocolFrameDefragmenter protocolFrameDefragmenter,
            Stream tcpStream,
            IPEndPoint ipEndPoint,
            ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue,
            CancellationTokenSource cts)
        {
            this.ProtocolFrameDefragmenter = protocolFrameDefragmenter;
            this.TcpStream = tcpStream;
            this.IPEndPoint = ipEndPoint;
            this.SendQueue = sendQueue;
            this.CancellationTokenSource = cts;
        }

        public event EventHandler<FrameArrivedEventArgs> FrameArrived;

        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        public IProtocolFrameDefragmenter ProtocolFrameDefragmenter { get; }

        public Stream TcpStream { get; }

        public IPEndPoint IPEndPoint { get; }

        public ActionBlock<RemoteTcpPeerOutgoingMessage> SendQueue { get; }

        public CancellationTokenSource CancellationTokenSource { get; }

        public IObservable<FrameArrivedData> WhenFrameArrived => Observable.FromEventPattern<FrameArrivedEventArgs>(
                h => this.FrameArrived += h,
                h => this.FrameArrived -= h)
            .TakeUntil(this.WhenConnectionClosed)
            .Select(x => x.EventArgs.FrameArrivedData);

        public IObservable<ConnectionClosedData> WhenConnectionClosed => Observable.FromEventPattern<ConnectionClosedEventArgs>(
                h => this.ConnectionClosed += h,
                h => this.ConnectionClosed -= h)
            .Select(x => x.EventArgs.ConnectionClosedData);

        public IDisposable CustomObject { get; set; }

        public ConnectionCloseReason ConnectionCloseReason
        {
            get => this.connectionCloseReason;
            set => this.connectionCloseReason = this.connectionCloseReason != ConnectionCloseReason.NoReason ? this.connectionCloseReason : value;
        }

        public Task<bool> SendAsync(byte[] data)
        {
            return this.SendAsync(data, CancellationToken.None);
        }

        public Task<bool> SendAsync(byte[] data, CancellationToken cancellationToken)
        {
            return this.SendAsync(data, 0, data.Length, cancellationToken);
        }

        public Task<bool> SendAsync(byte[] data, int offset, int count)
        {
            return this.SendAsync(data, offset, count, CancellationToken.None);
        }

        public async Task<bool> SendAsync(byte[] data, int offset, int count, CancellationToken cancellationToken)
        {
            bool result;

            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(this.CancellationTokenSource.Token, cancellationToken))
            {
                try
                {
                    result = await this.SendQueue.SendAsync(
                        new RemoteTcpPeerOutgoingMessage(
                            this, 
                            this.CancellationTokenSource.Token,
                            new IOBuffer(data, offset, count)),
                        linkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    result = false;
                }
            }

            return result;
        }

        public bool Post(byte[] data) => this.Post(data, 0, data.Length);

        public bool Post(byte[] data, int offset, int count)
        {
            return this.SendQueue.Post(new RemoteTcpPeerOutgoingMessage(
                            this,
                            this.CancellationTokenSource.Token,
                            new IOBuffer(data, offset, count)));
        }

        public void Dispose()
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

        public void Disconnect(ConnectionCloseReason reason)
        {
            try
            {
                this.ConnectionCloseReason = reason;
                this.CancellationTokenSource.Cancel();
            }
            catch (Exception)
            {
                return;
            }
        }

        public void OnFrameArrived(FrameArrivedEventArgs e)
        {
            this.FrameArrived?.Invoke(this, e);
        }

        public void OnConnectionClosed(ConnectionClosedEventArgs e)
        {
            this.ConnectionClosed?.Invoke(this, e);
        }
    }
}
