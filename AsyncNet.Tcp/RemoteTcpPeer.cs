using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AsyncNet.Core;

namespace AsyncNet.Tcp
{
    public class RemoteTcpPeer : IRemotePeer
    {
        public RemoteTcpPeer(
            Stream tcpStream,
            IPEndPoint ipEndPoint,
            ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue, 
            CancellationTokenSource cts)
        {
            this.TcpStream = tcpStream;
            this.IPEndPoint = ipEndPoint;
            this.SendQueue = sendQueue;
            this.CancellationTokenSource = cts;
        }

        public event EventHandler<FrameArrivedEventArgs> FrameArrived;

        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

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

        public void Dispose()
        {
            this.Disconnect();
        }

        public void Disconnect()
        {
            try
            {
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
