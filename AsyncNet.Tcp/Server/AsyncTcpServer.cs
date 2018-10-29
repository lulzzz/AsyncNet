using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Reactive.Linq;
using System.Net.Security;
using System.Security.Authentication;
using AsyncNet.Core;

namespace AsyncNet.Tcp.Server
{
    public class AsyncTcpServer : IAsyncServer
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
                ProtocolFrameDefragmenter = config.ProtocolFrameDefragmenter,
                ConnectionTimeout = config.ConnectionTimeout,
                MaxSendQueuePerPeerSize = config.MaxSendQueuePerPeerSize,
                IPAddress = config.IPAddress,
                Port = config.Port,
                X509Certificate = config.X509Certificate
            };
        }

        public event EventHandler<ServerStartedEventArgs> ServerStarted;

        public event EventHandler<ServerStoppedEventArgs> ServerStopped;

        public event EventHandler<FrameArrivedEventArgs> FrameArrived;

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

        public IObservable<FrameArrivedData> WhenFrameArrived => Observable.FromEventPattern<FrameArrivedEventArgs>(
                h => this.FrameArrived += h,
                h => this.FrameArrived -= h)
            .TakeUntil(this.WhenServerStopped)
            .Select(x => x.EventArgs.FrameArrivedData);

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
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                var sendQueue = new ActionBlock<RemoteTcpPeerOutgoingMessage>(
                                    this.SendToRemotePeerAsync,
                                    new ExecutionDataflowBlockOptions()
                                    {
                                        EnsureOrdered = true,
                                        BoundedCapacity = this.config.MaxSendQueuePerPeerSize,
                                        MaxDegreeOfParallelism = 1,
                                        CancellationToken = linkedCts.Token
                                    });
                
                RemoteTcpPeer remoteTcpPeer;
                SslStream sslStream = null;

                try
                {
                    if (this.config.X509Certificate != null)
                    {
                        sslStream = new SslStream(tcpClient.GetStream(), false);

                        await sslStream.AuthenticateAsServerWithCancellationAsync(this.config.X509Certificate, linkedCts.Token)
                            .ConfigureAwait(false);

                        remoteTcpPeer = new RemoteTcpPeer(
                            sslStream,
                            tcpClient.Client.RemoteEndPoint as IPEndPoint,
                            sendQueue,
                            linkedCts);
                    }
                    else
                    {
                        remoteTcpPeer = new RemoteTcpPeer(
                            tcpClient.GetStream(),
                            tcpClient.Client.RemoteEndPoint as IPEndPoint,
                            sendQueue,
                            linkedCts);
                    }
                }
                catch (AuthenticationException ex)
                {
                    var unhandledErrorEventArgs = new UnhandledErrorEventArgs(new UnhandledErrorData(ex));
                    this.OnUnhandledError(unhandledErrorEventArgs);

                    sendQueue.Complete();
                    sslStream?.Dispose();

                    return;
                }
                catch (Exception)
                {
                    sendQueue.Complete();
                    return;
                }

                using (remoteTcpPeer)
                {
                    var connectionEstablishedEventArgs = new ConnectionEstablishedEventArgs(new ConnectionEstablishedData(remoteTcpPeer));
                    this.OnConnectionEstablished(connectionEstablishedEventArgs);

                    try
                    {
                        await this.HandleRemotePeerAsync(remoteTcpPeer, linkedCts.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        var unhandledErrorEventArgs = new UnhandledErrorEventArgs(new UnhandledErrorData(ex));

                        this.OnUnhandledError(unhandledErrorEventArgs);
                    }
                    finally
                    {
                        sendQueue.Complete();
                        sslStream?.Dispose();
                    }
                }
            }
        }

        protected async Task HandleRemotePeerAsync(RemoteTcpPeer remoteTcpPeer, CancellationToken cancellationToken)
        {
            var connectionCloseReason = await this.ReceiveFromRemotePeerAsync(remoteTcpPeer, cancellationToken).ConfigureAwait(false);
            var connectionClosedEventArgs = new ConnectionClosedEventArgs(new ConnectionClosedData(remoteTcpPeer, connectionCloseReason));
            await this.OnConnectionClosedAsync(remoteTcpPeer, connectionClosedEventArgs)
                .ConfigureAwait(false);
        }

        protected async Task<ConnectionCloseReason> ReceiveFromRemotePeerAsync(RemoteTcpPeer remoteTcpPeer, CancellationToken cancellationToken)
        {
            Defragmentation.ReadFrameResult readFrameResult = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var timeoutCts = this.config.ConnectionTimeout == TimeSpan.Zero ? new CancellationTokenSource() : new CancellationTokenSource(this.config.ConnectionTimeout))
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken))
                    {
                        readFrameResult = await this.config.ProtocolFrameDefragmenter
                            .ReadFrameAsync(remoteTcpPeer, readFrameResult?.LeftOvers, linkedCts.Token)
                            .ConfigureAwait(false);
                    }
                }
                catch (AsyncNetUnhandledException ex)
                {
                    var unhandledErrorEventArgs = new UnhandledErrorEventArgs(new UnhandledErrorData(ex.InnerException));
                    this.OnUnhandledError(unhandledErrorEventArgs);

                    return ConnectionCloseReason.Unknown;
                }
                catch (OperationCanceledException)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        return ConnectionCloseReason.Timeout;
                    }
                    else
                    {
                        return ConnectionCloseReason.LocalShutdown;
                    }
                }
                catch (Exception)
                {
                    return ConnectionCloseReason.Unknown;
                }

                if (readFrameResult.ReadFrameStatus == Defragmentation.ReadFrameStatus.StreamClosed)
                {
                    return ConnectionCloseReason.RemoteShutdown;
                }
                else if (readFrameResult.ReadFrameStatus == Defragmentation.ReadFrameStatus.FrameDropped)
                {
                    readFrameResult = null;

                    continue;
                }

                var transportData = new FrameArrivedData(remoteTcpPeer, readFrameResult.FrameData);
                var frameArrivedEventArgs = new FrameArrivedEventArgs(transportData);

                await this.OnFrameArrivedAsync(remoteTcpPeer, frameArrivedEventArgs)
                    .ConfigureAwait(false);
            }

            return ConnectionCloseReason.LocalShutdown;
        }

        protected async Task SendToRemotePeerAsync(RemoteTcpPeerOutgoingMessage outgoingMessage)
        {
            try
            {
                await outgoingMessage.RemoteTcpPeer.TcpStream.WriteWithRealCancellationAsync(
                    outgoingMessage.IOBuffer.Buffer,
                    outgoingMessage.IOBuffer.Offset,
                    outgoingMessage.IOBuffer.Count,
                    outgoingMessage.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
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

        protected async Task OnFrameArrivedAsync(RemoteTcpPeer remoteTcpPeer, FrameArrivedEventArgs e)
        {
            try
            {
                await Task.WhenAll(
                    Task.Run(() => remoteTcpPeer.OnFrameArrived(e)), 
                    Task.Run(() => this.FrameArrived?.Invoke(this, e)))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var unhandledErrorEventArgs = new UnhandledErrorEventArgs(new UnhandledErrorData(ex));

                this.OnUnhandledError(unhandledErrorEventArgs);
            }
        }

        protected async Task OnConnectionClosedAsync(RemoteTcpPeer remoteTcpPeer, ConnectionClosedEventArgs e)
        {
            try
            {
                await Task.WhenAll(
                    Task.Run(() => remoteTcpPeer.OnConnectionClosed(e)),
                    Task.Run(() => this.ConnectionClosed?.Invoke(this, e)))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var unhandledErrorEventArgs = new UnhandledErrorEventArgs(new UnhandledErrorData(ex));

                this.OnUnhandledError(unhandledErrorEventArgs);
            }
        }

        protected void OnUnhandledError(UnhandledErrorEventArgs e)
        {
            try
            {
                this.UnhandledErrorOccured?.Invoke(this, e);
            }
            catch
            {
            }
        }
    }
}
