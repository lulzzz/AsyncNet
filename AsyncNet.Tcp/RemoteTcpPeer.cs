using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace AsyncNet.Tcp
{
    public class RemoteTcpPeer : IRemoteTcpPeer
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

        public Stream TcpStream { get; }

        public IPEndPoint IPEndPoint { get; }

        public ActionBlock<RemoteTcpPeerOutgoingMessage> SendQueue { get; }

        public CancellationTokenSource CancellationTokenSource { get; }

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
    }
}
