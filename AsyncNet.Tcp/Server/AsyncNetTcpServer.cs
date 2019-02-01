using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Reactive.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using AsyncNet.Core.Error.SystemEvent;
using AsyncNet.Core.Extensions;
using AsyncNet.Tcp.Remote;
using AsyncNet.Tcp.Server.SystemEvent;
using AsyncNet.Tcp.Remote.SystemEvent;
using AsyncNet.Tcp.Connection.SystemEvent;
using AsyncNet.Tcp.Connection;
using AsyncNet.Tcp.Extensions;
using AsyncNet.Tcp.Error.SystemEvent;
using AsyncNet.Tcp.Error;
using AsyncNet.Core.Exceptions;
using AsyncNet.Core.Error;

namespace AsyncNet.Tcp.Server
{
    public class AsyncNetTcpServer : IAsyncNetTcpServer
    {
        public AsyncNetTcpServer(int port) : this(new AsyncNetTcpServerConfig()
            {
                Port = port
            })
        {
        }

        public AsyncNetTcpServer(AsyncNetTcpServerConfig config)
        {
            this.Config = new AsyncNetTcpServerConfig()
            {
                ProtocolFrameDefragmenterFactory = config.ProtocolFrameDefragmenterFactory,
                ConnectionTimeout = config.ConnectionTimeout,
                MaxSendQueuePerPeerSize = config.MaxSendQueuePerPeerSize,
                IPAddress = config.IPAddress,
                Port = config.Port,
                ConfigureTcpListenerCallback = config.ConfigureTcpListenerCallback,
                UseSsl = config.UseSsl,
                X509Certificate = config.X509Certificate,
                RemoteCertificateValidationCallback = config.RemoteCertificateValidationCallback,
                EncryptionPolicy = config.EncryptionPolicy,
                ClientCertificateRequiredCallback = config.ClientCertificateRequiredCallback,
                CheckCertificateRevocationCallback = config.CheckCertificateRevocationCallback,
                EnabledProtocols = config.EnabledProtocols
            };
        }

        public event EventHandler<TcpServerStartedEventArgs> ServerStarted;

        public event EventHandler<TcpServerStoppedEventArgs> ServerStopped;

        public event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;

        public event EventHandler<TcpServerErrorEventArgs> ServerErrorOccured;

        public event EventHandler<RemoteTcpPeerErrorEventArgs> RemoteTcpPeerErrorOccured;

        public event EventHandler<UnhandledErrorEventArgs> UnhandledErrorOccured;

        public event EventHandler<ConnectionEstablishedEventArgs> ConnectionEstablished;

        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        public IObservable<TcpServerStartedData> WhenServerStarted => Observable.FromEventPattern<TcpServerStartedEventArgs>(
                h => this.ServerStarted += h,
                h => this.ServerStarted -= h)
            .Select(x => x.EventArgs.TcpServerStartedData);

        public IObservable<TcpServerStoppedData> WhenServerStopped => Observable.FromEventPattern<TcpServerStoppedEventArgs>(
                h => this.ServerStopped += h,
                h => this.ServerStopped -= h)
            .Select(x => x.EventArgs.TcpServerStoppedData);

        public IObservable<TcpFrameArrivedData> WhenFrameArrived => Observable.FromEventPattern<TcpFrameArrivedEventArgs>(
                h => this.FrameArrived += h,
                h => this.FrameArrived -= h)
            .TakeUntil(this.WhenServerStopped)
            .Select(x => x.EventArgs.TcpFrameArrivedData);

        public IObservable<ErrorData> WhenServerErrorOccured => Observable.FromEventPattern<TcpServerErrorEventArgs>(
                h => this.ServerErrorOccured += h,
                h => this.ServerErrorOccured -= h)
            .TakeUntil(this.WhenServerStopped)
            .Select(x => x.EventArgs.ErrorData);

        public IObservable<RemoteTcpPeerErrorData> WhenRemoteTcpPeerErrorOccured => Observable.FromEventPattern<RemoteTcpPeerErrorEventArgs>(
                h => this.RemoteTcpPeerErrorOccured += h,
                h => this.RemoteTcpPeerErrorOccured -= h)
            .TakeUntil(this.WhenServerStopped)
            .Select(x => x.EventArgs.ErrorData);

        public IObservable<ErrorData> WhenUnhandledErrorOccured => Observable.FromEventPattern<UnhandledErrorEventArgs>(
                h => this.UnhandledErrorOccured += h,
                h => this.UnhandledErrorOccured -= h)
            .TakeUntil(this.WhenServerStopped)
            .Select(x => x.EventArgs.ErrorData);

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

        protected virtual AsyncNetTcpServerConfig Config { get; set; }

        public virtual Task StartAsync()
        {
            return this.StartAsync(CancellationToken.None);
        }

        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            var tcpListener = this.CreateTcpListener();

            this.Config.ConfigureTcpListenerCallback?.Invoke(tcpListener);

            try
            {
                tcpListener.Start();
            }
            catch (Exception ex)
            {
                var tcpServerErrorEventArgs = new TcpServerErrorEventArgs(new ErrorData(ex));

                this.OnServerErrorOccured(tcpServerErrorEventArgs);

                return;
            }

            try
            {
                await Task.WhenAll(
                    this.ListenAsync(tcpListener, cancellationToken),
                    Task.Run(() => this.OnServerStarted(new TcpServerStartedEventArgs(new TcpServerStartedData(this.Config.IPAddress, this.Config.Port)))))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var tcpServerErrorEventArgs = new TcpServerErrorEventArgs(new ErrorData(ex));

                this.OnServerErrorOccured(tcpServerErrorEventArgs);
            }
            finally
            {
                try
                {
                    tcpListener.Stop();
                }
                catch (Exception ex)
                {
                    var tcpServerErrorEventArgs = new TcpServerErrorEventArgs(new ErrorData(ex));

                    this.OnServerErrorOccured(tcpServerErrorEventArgs);
                }

                this.OnServerStopped(new TcpServerStoppedEventArgs(new TcpServerStoppedData(this.Config.IPAddress, this.Config.Port)));
            }
        }

        protected virtual TcpListener CreateTcpListener()
        {
            return new TcpListener(new IPEndPoint(this.Config.IPAddress, this.Config.Port));
        }

        protected virtual async Task ListenAsync(TcpListener tcpListener, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await AcceptTcpClient(tcpListener, token).ConfigureAwait(false);

                    this.HandleNewTcpClientAsync(tcpClient, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        protected virtual Task<TcpClient> AcceptTcpClient(TcpListener tcpListener, CancellationToken token)
        {
            return tcpListener.AcceptTcpClientWithCancellationTokenAsync(token);
        }

        // exploiting "async void" simplifies everything
        protected virtual async void HandleNewTcpClientAsync(TcpClient tcpClient, CancellationToken token)
        {
            using (tcpClient)
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                var sendQueue = this.CreateSendQueueActionBlock(linkedCts.Token);

                RemoteTcpPeer remoteTcpPeer;
                SslStream sslStream = null;

                try
                {
                    if (this.Config.UseSsl && this.Config.X509Certificate != null)
                    {
                        sslStream = this.CreateSslStream(tcpClient);

                        await this.AuthenticateSslStream(tcpClient, sslStream, linkedCts.Token)
                            .ConfigureAwait(false);

                        remoteTcpPeer = this.CreateRemoteTcpPeer(tcpClient, sslStream, sendQueue, linkedCts);
                    }
                    else
                    {
                        remoteTcpPeer = this.CreateRemoteTcpPeer(tcpClient, sendQueue, linkedCts);
                    }
                }
                catch (AuthenticationException ex)
                {
                    var serverErrorEventArgs = new TcpServerErrorEventArgs(new ErrorData(ex));
                    this.OnServerErrorOccured(serverErrorEventArgs);

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
                        var unhandledErrorEventArgs = new UnhandledErrorEventArgs(new ErrorData(ex));

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

        protected virtual ActionBlock<RemoteTcpPeerOutgoingMessage> CreateSendQueueActionBlock(CancellationToken token)
        {
            return new ActionBlock<RemoteTcpPeerOutgoingMessage>(
                this.SendToRemotePeerAsync,
                new ExecutionDataflowBlockOptions()
                {
                    EnsureOrdered = true,
                    BoundedCapacity = this.Config.MaxSendQueuePerPeerSize,
                    MaxDegreeOfParallelism = 1,
                    CancellationToken = token
                });
        }

        protected virtual SslStream CreateSslStream(TcpClient tcpClient)
        {
            return new SslStream(
                tcpClient.GetStream(),
                false,
                this.Config.RemoteCertificateValidationCallback,
                this.SelectDefaultLocalCertificate,
                this.Config.EncryptionPolicy);
        }

        protected virtual X509Certificate SelectDefaultLocalCertificate(
            object sender,
            string targetHost,
            X509CertificateCollection localCertificates,
            X509Certificate remoteCertificate,
            string[] acceptableIssuers)
        {
            return this.Config.X509Certificate;
        }

        protected virtual Task AuthenticateSslStream(TcpClient tcpClient, SslStream sslStream, CancellationToken token)
        {
            return sslStream.AuthenticateAsServerWithCancellationAsync(
                this.Config.X509Certificate,
                this.Config.ClientCertificateRequiredCallback(tcpClient),
                this.Config.EnabledProtocols,
                this.Config.CheckCertificateRevocationCallback(tcpClient),
                token);
        }

        protected virtual RemoteTcpPeer CreateRemoteTcpPeer(TcpClient tcpClient, ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue, CancellationTokenSource tokenSource)
        {
            return new RemoteTcpPeer(
                            this.Config.ProtocolFrameDefragmenterFactory,
                            tcpClient,
                            sendQueue,
                            tokenSource);
        }

        protected virtual RemoteTcpPeer CreateRemoteTcpPeer(TcpClient tcpClient, SslStream sslStream, ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue, CancellationTokenSource tokenSource)
        {
            return new RemoteTcpPeer(
                            this.Config.ProtocolFrameDefragmenterFactory,
                            tcpClient,
                            sslStream,
                            sendQueue,
                            tokenSource);
        }

        protected virtual async Task HandleRemotePeerAsync(RemoteTcpPeer remoteTcpPeer, CancellationToken cancellationToken)
        {
            await this.ReceiveFromRemotePeerAsync(remoteTcpPeer, cancellationToken).ConfigureAwait(false);

            var connectionClosedEventArgs = new ConnectionClosedEventArgs(new ConnectionClosedData(remoteTcpPeer, remoteTcpPeer.ConnectionCloseReason));
            await this.OnConnectionClosedAsync(remoteTcpPeer, connectionClosedEventArgs)
                .ConfigureAwait(false);
        }

        protected virtual async Task ReceiveFromRemotePeerAsync(RemoteTcpPeer remoteTcpPeer, CancellationToken cancellationToken)
        {
            Defragmentation.ReadFrameResult readFrameResult = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var timeoutCts = this.Config.ConnectionTimeout == TimeSpan.Zero ? new CancellationTokenSource() : new CancellationTokenSource(this.Config.ConnectionTimeout))
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken))
                    {
                        readFrameResult = await remoteTcpPeer.ProtocolFrameDefragmenter
                            .ReadFrameAsync(remoteTcpPeer, readFrameResult?.LeftOvers, linkedCts.Token)
                            .ConfigureAwait(false);
                    }
                }
                catch (AsyncNetUnhandledException ex)
                {
                    var unhandledErrorEventArgs = new UnhandledErrorEventArgs(new ErrorData(ex.InnerException));
                    this.OnUnhandledError(unhandledErrorEventArgs);

                    remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.Unknown;
                    return;
                }
                catch (OperationCanceledException)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.Timeout;
                        return;
                    }
                    else
                    {
                        remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.LocalShutdown;
                        return;
                    }
                }
                catch (Exception)
                {
                    remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.Unknown;
                    return;
                }

                if (readFrameResult.ReadFrameStatus == Defragmentation.ReadFrameStatus.StreamClosed)
                {
                    remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.RemoteShutdown;
                    return;
                }
                else if (readFrameResult.ReadFrameStatus == Defragmentation.ReadFrameStatus.FrameDropped)
                {
                    readFrameResult = null;

                    continue;
                }

                var tcpFrameArrivedData = new TcpFrameArrivedData(remoteTcpPeer, readFrameResult.FrameData);
                var frameArrivedEventArgs = new TcpFrameArrivedEventArgs(tcpFrameArrivedData);

                await this.OnFrameArrivedAsync(remoteTcpPeer, frameArrivedEventArgs)
                    .ConfigureAwait(false);
            }

            remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.LocalShutdown;
        }

        protected virtual async Task SendToRemotePeerAsync(RemoteTcpPeerOutgoingMessage outgoingMessage)
        {
            try
            {
                await outgoingMessage.RemoteTcpPeer.TcpStream.WriteWithRealCancellationAsync(
                    outgoingMessage.Buffer.Memory,
                    outgoingMessage.Buffer.Offset,
                    outgoingMessage.Buffer.Count,
                    outgoingMessage.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var remoteTcpPeerErrorEventArgs = new RemoteTcpPeerErrorEventArgs(new RemoteTcpPeerErrorData(outgoingMessage.RemoteTcpPeer, ex));

                this.OnRemoteTcpPeerErrorOccured(remoteTcpPeerErrorEventArgs);
            }
        }

        protected virtual void OnConnectionEstablished(ConnectionEstablishedEventArgs e)
        {
            try
            {
                this.ConnectionEstablished?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var unhandledErrorEventArgs = new UnhandledErrorEventArgs(new ErrorData(ex));

                this.OnUnhandledError(unhandledErrorEventArgs);
            }
        }

        protected virtual async Task OnFrameArrivedAsync(RemoteTcpPeer remoteTcpPeer, TcpFrameArrivedEventArgs e)
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
                var unhandledErrorEventArgs = new UnhandledErrorEventArgs(new ErrorData(ex));

                this.OnUnhandledError(unhandledErrorEventArgs);
            }
        }

        protected virtual async Task OnConnectionClosedAsync(RemoteTcpPeer remoteTcpPeer, ConnectionClosedEventArgs e)
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
                var unhandledErrorEventArgs = new UnhandledErrorEventArgs(new ErrorData(ex));

                this.OnUnhandledError(unhandledErrorEventArgs);
            }
        }

        protected virtual void OnServerStarted(TcpServerStartedEventArgs e)
        {
            this.ServerStarted?.Invoke(this, e);
        }

        protected virtual void OnServerStopped(TcpServerStoppedEventArgs e)
        {
            this.ServerStopped?.Invoke(this, e);
        }

        protected virtual void OnServerErrorOccured(TcpServerErrorEventArgs e)
        {
            this.ServerErrorOccured?.Invoke(this, e);
        }

        protected virtual void OnRemoteTcpPeerErrorOccured(RemoteTcpPeerErrorEventArgs e)
        {
            this.RemoteTcpPeerErrorOccured?.Invoke(this, e);
        }

        protected virtual void OnUnhandledError(UnhandledErrorEventArgs e)
        {
            this.UnhandledErrorOccured?.Invoke(this, e);
        }
    }
}
