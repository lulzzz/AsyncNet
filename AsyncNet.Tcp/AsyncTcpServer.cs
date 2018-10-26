using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Reactive.Linq;
using AsyncNet.Core;

namespace AsyncNet.Tcp
{
    public class AsyncTcpServer : IAsyncTcpServer
    {
        private readonly AsyncTcpServerConfig config;

        public AsyncTcpServer(int port) : this(new AsyncTcpServerConfig()
            {
                Port = port
            })
        {
        }

        public AsyncTcpServer(AsyncTcpServerConfig config)
        {
            this.config = new AsyncTcpServerConfig()
            {
                ConnectionTimeout = config.ConnectionTimeout,
                ReceiveBufferSize = config.ReceiveBufferSize,
                MaxSendQueuePerPeerSize = config.MaxSendQueuePerPeerSize,
                IPAddress = config.IPAddress,
                Port = config.Port
            };
        }

        public event EventHandler<ServerStartedEventArgs> ServerStarted;

        public event EventHandler<ServerStoppedEventArgs> ServerStopped;

        public event EventHandler<DataReceivedEventArgs> DataReceived;

        public event EventHandler<UnhandledErrorEventArgs> UnhandledErrorOccured;

        public event EventHandler<ConnectionEstablishedEventArgs> ConnectionEstablished;

        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        public IObservable<ServerStartedData> WhenServerStarted => Observable.FromEventPattern<ServerStartedEventArgs>(
                h => this.ServerStarted += h,
                h => this.ServerStarted -= h)
            .Select(x => x.EventArgs.ServerStartedData);

        public IObservable<ServerStoppedData> WhenServerStopped => Observable.FromEventPattern<ServerStoppedEventArgs>(
                h => this.ServerStopped += h,
                h => this.ServerStopped -= h)
            .Select(x => x.EventArgs.ServerStoppedData);

        public IObservable<TransportData> WhenDataReceived => Observable.FromEventPattern<DataReceivedEventArgs>(
                h => this.DataReceived += h,
                h => this.DataReceived -= h)
            .TakeUntil(this.WhenServerStopped)
            .Select(x => x.EventArgs.TransportData);

        public IObservable<UnhandledErrorData> WhenUnhandledErrorOccured => Observable.FromEventPattern<UnhandledErrorEventArgs>(
                h => this.UnhandledErrorOccured += h,
                h => this.UnhandledErrorOccured -= h)
            .TakeUntil(this.WhenServerStopped)
            .Select(x => x.EventArgs.UnhandledErrorData);

        public IObservable<ConnectionEstablishedData> WhenConnectionEstablished => Observable.FromEventPattern<ConnectionEstablishedEventArgs>(
                h => this.ConnectionEstablished += h,
                h => this.ConnectionEstablished -= h)
            .TakeUntil(this.WhenServerStopped)
            .Select(x => x.EventArgs.ConnectionEstablishedData);

        public IObservable<ConnectionClosedData> WhenConnectionClosed => Observable.FromEventPattern<ConnectionClosedEventArgs>(
                h => this.ConnectionClosed += h,
                h => this.ConnectionClosed -= h)
            .TakeUntil(this.WhenServerStopped)
            .Select(x => x.EventArgs.ConnectionClosedData);

        public Task StartAsync()
        {
            return this.StartAsync(CancellationToken.None);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var tcpListener = new TcpListener(new IPEndPoint(this.config.IPAddress, this.config.Port));
            tcpListener.Start();

            this.ServerStarted?.Invoke(this, new ServerStartedEventArgs(new ServerStartedData(this.config.IPAddress, this.config.Port)));

            try
            {
                await this.ListenAsync(tcpListener, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                tcpListener.Stop();

                this.ServerStopped?.Invoke(this, new ServerStoppedEventArgs(new ServerStoppedData(this.config.IPAddress, this.config.Port)));
            }
        }

        protected async Task ListenAsync(TcpListener tcpListener, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await tcpListener.AcceptTcpClientWithCancellationTokenAsync(token).ConfigureAwait(false);

                    this.HandleNewTcpClientAsync(tcpClient, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        // exploiting "async void" simplifies everything
        protected async void HandleNewTcpClientAsync(TcpClient tcpClient, CancellationToken token)
        {
            using (tcpClient)
            using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                var sendQueue = new ActionBlock<RemoteTcpPeerOutgoingMessage>(
                                    this.SendToRemotePeerAsync,
                                    new ExecutionDataflowBlockOptions()
                                    {
                                        EnsureOrdered = true,
                                        BoundedCapacity = this.config.MaxSendQueuePerPeerSize,
                                        MaxDegreeOfParallelism = 1,
                                        CancellationToken = linkedSource.Token
                                    });
                
                RemoteTcpPeer remoteTcpPeer;

                try
                {
                    remoteTcpPeer = new RemoteTcpPeer(
                        tcpClient, 
                        sendQueue, 
                        linkedSource);
                }
                catch (Exception)
                {
                    sendQueue.Complete();
                    return;
                }

                var connectionEstablishedEventArgs = new ConnectionEstablishedEventArgs(new ConnectionEstablishedData(remoteTcpPeer));
                this.OnConnectionEstablished(connectionEstablishedEventArgs);

                try
                {
                    await this.HandleRemotePeerAsync(remoteTcpPeer, linkedSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var unhandledErrorEventArgs = new UnhandledErrorEventArgs(new UnhandledErrorData(ex));

                    this.OnUnhandledError(unhandledErrorEventArgs);
                }

                sendQueue.Complete();
            }
        }

        protected async Task HandleRemotePeerAsync(RemoteTcpPeer remoteTcpPeer, CancellationToken cancellationToken)
        {
            var connectionCloseType = await this.ReceiveFromRemotePeerAsync(remoteTcpPeer, cancellationToken).ConfigureAwait(false);
            
            var connectionClosedEventArgs = new ConnectionClosedEventArgs(new ConnectionClosedData(remoteTcpPeer, connectionCloseType));
            this.OnConnectionClosed(connectionClosedEventArgs);
        }

        protected async Task<ConnectionCloseReason> ReceiveFromRemotePeerAsync(RemoteTcpPeer remoteTcpPeer, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var buffer = new byte[this.config.ReceiveBufferSize];
                int readLength;

                try
                {
                    using (var timeoutCts = this.config.ConnectionTimeout == TimeSpan.Zero ? new CancellationTokenSource() : new CancellationTokenSource(this.config.ConnectionTimeout))
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken))
                    {
                        readLength = await remoteTcpPeer.TcpClient.GetStream().ReadWithRealCancellationAsync(buffer, 0, buffer.Length, linkedCts.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        return ConnectionCloseReason.Timeout;
                    }

                    continue;
                }
                catch (Exception)
                {
                    return ConnectionCloseReason.Unknown;
                }

                if (readLength < 1)
                {
                    return ConnectionCloseReason.RemoteShutdown;
                }

                var receivedData = new ReceivedData(buffer, readLength);
                var transportData = new TransportData(remoteTcpPeer, receivedData);
                var dataReceivedEventArgs = new DataReceivedEventArgs(transportData);

                this.OnDataReceived(dataReceivedEventArgs);
            }

            return ConnectionCloseReason.LocalShutdown;
        }

        protected async Task SendToRemotePeerAsync(RemoteTcpPeerOutgoingMessage remotePeerSendItem)
        {
            try
            {
                await remotePeerSendItem.RemoteTcpPeer.TcpClient.GetStream().WriteWithRealCancellationAsync(
                    remotePeerSendItem.OutgoingMessage.Buffer,
                    remotePeerSendItem.OutgoingMessage.Offset,
                    remotePeerSendItem.OutgoingMessage.Count,
                    remotePeerSendItem.CancellationToken).ConfigureAwait(false);
            }
            catch(Exception)
            {
                return;
            }
        }

        protected void OnConnectionEstablished(ConnectionEstablishedEventArgs e)
        {
            try
            {
                this.ConnectionEstablished?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var unhandledErrorEventArgs = new UnhandledErrorEventArgs(new UnhandledErrorData(ex));

                this.OnUnhandledError(unhandledErrorEventArgs);
            }
        }

        protected void OnDataReceived(DataReceivedEventArgs e)
        {
            try
            {
                this.DataReceived?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var unhandledErrorEventArgs = new UnhandledErrorEventArgs(new UnhandledErrorData(ex));

                this.OnUnhandledError(unhandledErrorEventArgs);
            }
        }

        protected void OnConnectionClosed(ConnectionClosedEventArgs e)
        {
            try
            {
                this.ConnectionClosed?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var unhandledErrorEventArgs = new UnhandledErrorEventArgs(new UnhandledErrorData(ex));

                this.OnUnhandledError(unhandledErrorEventArgs);
            }
        }

        protected void OnUnhandledError(UnhandledErrorEventArgs e)
        {
            this.UnhandledErrorOccured?.Invoke(this, e);
        }
    }
}
