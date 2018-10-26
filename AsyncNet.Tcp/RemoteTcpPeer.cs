using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AsyncNet.Core;

namespace AsyncNet.Tcp
{
    public class RemoteTcpPeer : IRemoteTcpPeer
    {
        public RemoteTcpPeer(TcpClient tcpClient, ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue, CancellationTokenSource cts)
        {
            this.CancellationTokenSource = cts;
            this.TcpClient = tcpClient;
            this.IPEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
            this.SendQueue = sendQueue;
        }

        public CancellationTokenSource CancellationTokenSource { get; }

        public TcpClient TcpClient { get; }

        public IPEndPoint IPEndPoint { get; }

        public ActionBlock<RemoteTcpPeerOutgoingMessage> SendQueue { get; }

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
                            new OutgoingMessage(data, offset, count)), 
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
