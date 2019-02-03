using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AsyncNet.Core.Error;
using AsyncNet.Core.Error.SystemEvent;
using AsyncNet.Core.Exceptions;
using AsyncNet.Core.Extensions;
using AsyncNet.Tcp.Client.SystemEvent;
using AsyncNet.Tcp.Connection;
using AsyncNet.Tcp.Connection.SystemEvent;
using AsyncNet.Tcp.Error;
using AsyncNet.Tcp.Error.SystemEvent;
using AsyncNet.Tcp.Extensions;
using AsyncNet.Tcp.Remote;
using AsyncNet.Tcp.Remote.SystemEvent;

namespace AsyncNet.Tcp.Client
{
    /// <summary>
    /// An implementation of asynchronous TCP client
    /// </summary>
    public class AsyncNetTcpClient : IAsyncNetTcpClient
    {
        /// <summary>
        /// Constructs TCP client that connects to the particular server and has default configuration
        /// </summary>
        /// <param name="targetHostname">Server hostname</param>
        /// <param name="targetPort">Server port</param>
        public AsyncNetTcpClient(string targetHostname, int targetPort) : this (new AsyncTcpClientConfig()
        {
            TargetHostname = targetHostname,
            TargetPort = targetPort
        })
        {
        }

        /// <summary>
        /// Constructs TCP client with custom configuration
        /// </summary>
        /// <param name="config">TCP client configuration</param>
        public AsyncNetTcpClient(AsyncTcpClientConfig config)
        {
            this.Config = new AsyncTcpClientConfig()
            {
                ProtocolFrameDefragmenterFactory = config.ProtocolFrameDefragmenterFactory,
                TargetHostname = config.TargetHostname,
                TargetPort = config.TargetPort,
                ConnectionTimeout = config.ConnectionTimeout,
                MaxSendQueueSize = config.MaxSendQueueSize,
                ConfigureTcpClientCallback = config.ConfigureTcpClientCallback,
                FilterResolvedIpAddressListForConnectionCallback = config.FilterResolvedIpAddressListForConnectionCallback,
                UseSsl = config.UseSsl,
                X509ClientCertificates = config.X509ClientCertificates,
                RemoteCertificateValidationCallback = config.RemoteCertificateValidationCallback,
                LocalCertificateSelectionCallback = config.LocalCertificateSelectionCallback,
                EncryptionPolicy = config.EncryptionPolicy,
                CheckCertificateRevocation = config.CheckCertificateRevocation,
                EnabledProtocols = config.EnabledProtocols
            };
        }

        /// <summary>
        /// Fires when client started running, but it's not connected yet to the server
        /// </summary>
        public event EventHandler<TcpClientStartedEventArgs> ClientStarted;

        /// <summary>
        /// Fires when client stopped running
        /// </summary>
        public event EventHandler<TcpClientStoppedEventArgs> ClientStopped;

        /// <summary>
        /// Fires when there was a problem with the client
        /// </summary>
        public event EventHandler<TcpClientErrorEventArgs> ClientErrorOccured;

        /// <summary>
        /// Fires when there was a problem while handling communication with the server
        /// </summary>
        public event EventHandler<RemoteTcpPeerErrorEventArgs> RemoteTcpPeerErrorOccured;

        /// <summary>
        /// Fires when unhandled error occured - e.g. when event subscriber throws an exception
        /// </summary>
        public event EventHandler<UnhandledErrorEventArgs> UnhandledErrorOccured;

        /// <summary>
        /// Fires when connection with the server is established
        /// </summary>
        public event EventHandler<ConnectionEstablishedEventArgs> ConnectionEstablished;

        /// <summary>
        /// Fires when TCP frame arrived from the server
        /// </summary>
        public event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;

        /// <summary>
        /// Fires when connection with the server closes
        /// </summary>
        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        /// <summary>
        /// Produces an element when client started running, but it's not connected yet to the server
        /// </summary>
        public IObservable<TcpClientStartedData> WhenClientStarted =>
            Observable.FromEventPattern<TcpClientStartedEventArgs>(
                h => this.ClientStarted += h, h => this.ClientStarted -= h)
            .Select(x => x.EventArgs.TcpClientStartedData);

        /// <summary>
        /// Produces an element when client stopped running
        /// </summary>
        public IObservable<TcpClientStoppedData> WhenClientStopped =>
            Observable.FromEventPattern<TcpClientStoppedEventArgs>(
                h => this.ClientStopped += h, h => this.ClientStopped -= h)
            .Select(x => x.EventArgs.TcpClientStoppedData);

        /// <summary>
        /// Produces an element when there was a problem with the client
        /// </summary>
        public IObservable<ErrorData> WhenClientErrorOccured =>
            Observable.FromEventPattern<TcpClientErrorEventArgs>(
                h => this.ClientErrorOccured += h, h => this.ClientErrorOccured -= h)
            .TakeUntil(this.WhenClientStopped)
            .Select(x => x.EventArgs.ErrorData);

        /// <summary>
        /// Produces an element when there was a problem while handling communication with the server
        /// </summary>
        public IObservable<RemoteTcpPeerErrorData> WhenRemoteTcpPeerErrorOccured => Observable.FromEventPattern<RemoteTcpPeerErrorEventArgs>(
                h => this.RemoteTcpPeerErrorOccured += h,
                h => this.RemoteTcpPeerErrorOccured -= h)
            .TakeUntil(this.WhenClientStopped)
            .Select(x => x.EventArgs.ErrorData);

        /// <summary>
        /// Produces an element when unhandled error occured - e.g. when event subscriber throws an exception
        /// </summary>
        public IObservable<ErrorData> WhenUnhandledErrorOccured => Observable.FromEventPattern<UnhandledErrorEventArgs>(
                h => this.UnhandledErrorOccured += h,
                h => this.UnhandledErrorOccured -= h)
            .TakeUntil(this.WhenClientStopped)
            .Select(x => x.EventArgs.ErrorData);

        /// <summary>
        /// Produces an element when connection with the server is established
        /// </summary>
        public IObservable<ConnectionEstablishedData> WhenConnectionEstablished =>
            Observable.FromEventPattern<ConnectionEstablishedEventArgs>(
                h => this.ConnectionEstablished += h, h => this.ConnectionEstablished -= h)
            .TakeUntil(this.WhenClientStopped)
            .Select(x => x.EventArgs.ConnectionEstablishedData);

        /// <summary>
        /// Fires when TCP frame arrived from the server
        /// </summary>
        public IObservable<TcpFrameArrivedData> WhenFrameArrived =>
            Observable.FromEventPattern<TcpFrameArrivedEventArgs>(
                h => this.FrameArrived += h, h => this.FrameArrived -= h)
            .TakeUntil(this.WhenClientStopped)
            .Select(x => x.EventArgs.TcpFrameArrivedData);

        /// <summary>
        /// Produces an element when connection with the server closes
        /// </summary>
        public IObservable<ConnectionClosedData> WhenConnectionClosed =>
            Observable.FromEventPattern<ConnectionClosedEventArgs>(
                h => this.ConnectionClosed += h, h => this.ConnectionClosed -= h)
            .TakeUntil(this.WhenClientStopped)
            .Select(x => x.EventArgs.ConnectionClosedData);

        /// <summary>
        /// Asynchronously starts the client that run until connection with the server is closed
        /// </summary>
        /// <returns><see cref="Task" /></returns>
        public virtual Task StartAsync()
        {
            return this.StartAsync(CancellationToken.None);
        }

        /// <summary>
        /// Asynchronously starts the client that run until connection with the server is closed or <paramref name="cancellationToken" /> is cancelled
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="Task" /></returns>
        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            IPAddress[] addresses;

            var tcpClient = this.CreateTcpClient();

            this.Config.ConfigureTcpClientCallback?.Invoke(tcpClient);

            this.OnClientStarted(new TcpClientStartedEventArgs(new TcpClientStartedData(this.Config.TargetHostname, this.Config.TargetPort)));

            try
            {
                addresses = await this.GetHostAddresses(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                this.OnClientStopped(new TcpClientStoppedEventArgs(new TcpClientStoppedData(this.Config.TargetHostname, this.Config.TargetPort)));

                return;
            }
            catch (Exception ex)
            {
                this.OnClientErrorOccured(new TcpClientErrorEventArgs(new ErrorData(ex)));

                this.OnClientStopped(new TcpClientStoppedEventArgs(new TcpClientStoppedData(this.Config.TargetHostname, this.Config.TargetPort)));

                return;
            }

            try
            {
                await this.ConnectAsync(tcpClient, addresses, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                this.OnClientStopped(new TcpClientStoppedEventArgs(new TcpClientStoppedData(this.Config.TargetHostname, this.Config.TargetPort)));

                return;
            }
            catch (Exception ex)
            {
                this.OnClientErrorOccured(new TcpClientErrorEventArgs(new ErrorData(ex)));

                this.OnClientStopped(new TcpClientStoppedEventArgs(new TcpClientStoppedData(this.Config.TargetHostname, this.Config.TargetPort)));

                return;
            }

            try
            {
                await this.HandleTcpClientAsync(tcpClient, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                this.OnClientStopped(new TcpClientStoppedEventArgs(new TcpClientStoppedData(this.Config.TargetHostname, this.Config.TargetPort)));
            }
        }

        protected virtual AsyncTcpClientConfig Config { get; set; }

        protected virtual TcpClient CreateTcpClient()
        {
            return new TcpClient();
        }

        protected virtual Task<IPAddress[]> GetHostAddresses(CancellationToken cancellationToken)
        {
            return DnsExtensions.GetHostAddressesWithCancellationTokenAsync(this.Config.TargetHostname, cancellationToken);
        }

        protected virtual IEnumerable<IPAddress> DefaultIpAddressFilter(IPAddress[] addresses)
        {
            return addresses;
        }

        protected virtual async Task ConnectAsync(TcpClient tcpClient, IPAddress[] addresses, CancellationToken cancellationToken)
        {
            IEnumerable<IPAddress> filteredAddressList;

            if (addresses != null || addresses.Length > 0)
            {
                if (this.Config.FilterResolvedIpAddressListForConnectionCallback != null)
                {
                    filteredAddressList = this.Config.FilterResolvedIpAddressListForConnectionCallback(addresses);
                }
                else
                {
                    filteredAddressList = this.DefaultIpAddressFilter(addresses);
                }

                await tcpClient.ConnectWithCancellationTokenAsync(filteredAddressList.ToArray(), this.Config.TargetPort, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await tcpClient.ConnectWithCancellationTokenAsync(this.Config.TargetHostname, this.Config.TargetPort, cancellationToken).ConfigureAwait(false);
            }
        }

        protected virtual ActionBlock<RemoteTcpPeerOutgoingMessage> CreateSendQueueActionBlock(CancellationToken token)
        {
            return new ActionBlock<RemoteTcpPeerOutgoingMessage>(
                this.SendToRemotePeerAsync,
                new ExecutionDataflowBlockOptions()
                {
                    EnsureOrdered = true,
                    BoundedCapacity = this.Config.MaxSendQueueSize,
                    MaxDegreeOfParallelism = 1,
                    CancellationToken = token
                });
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

        protected virtual async Task HandleTcpClientAsync(TcpClient tcpClient, CancellationToken token)
        {
            using (tcpClient)
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                var sendQueue = this.CreateSendQueueActionBlock(linkedCts.Token);

                RemoteTcpPeer remoteTcpPeer;
                SslStream sslStream = null;

                try
                {
                    if (this.Config.UseSsl)
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
                    var clientErrorEventArgs = new TcpClientErrorEventArgs(new ErrorData(ex));
                    this.OnClientErrorOccured(clientErrorEventArgs);

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

        protected virtual SslStream CreateSslStream(TcpClient tcpClient)
        {
            LocalCertificateSelectionCallback certificateSelectionCallback;

            certificateSelectionCallback = this.Config.LocalCertificateSelectionCallback;

            if (certificateSelectionCallback == null)
            {
                certificateSelectionCallback = this.SelectDefaultLocalCertificate;
            }

            return new SslStream(
                tcpClient.GetStream(),
                false,
                this.Config.RemoteCertificateValidationCallback,
                certificateSelectionCallback,
                this.Config.EncryptionPolicy);
        }

        protected virtual X509Certificate SelectDefaultLocalCertificate(
            object sender,
            string targetHost,
            X509CertificateCollection localCertificates,
            X509Certificate remoteCertificate,
            string[] acceptableIssuers)
        {
            if (acceptableIssuers != null
                && acceptableIssuers.Length > 0
                && localCertificates != null
                && localCertificates.Count > 0)
            {
                foreach (X509Certificate certificate in localCertificates)
                {
                    string issuer = certificate.Issuer;
                    if (Array.IndexOf(acceptableIssuers, issuer) != -1)
                        return certificate;
                }
            }
            if (localCertificates != null
                && localCertificates.Count > 0)
            {
                return localCertificates[0];
            }

            return null;
        }

        protected virtual Task AuthenticateSslStream(TcpClient tcpClient, SslStream sslStream, CancellationToken token)
        {
            return sslStream.AuthenticateAsClientWithCancellationAsync(
                this.Config.TargetHostname,
                new X509CertificateCollection(this.Config.X509ClientCertificates?.ToArray() ?? new X509Certificate[] { }),
                this.Config.EnabledProtocols,
                this.Config.CheckCertificateRevocation,
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

                this.OnFrameArrived(remoteTcpPeer, frameArrivedEventArgs);
            }

            remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.LocalShutdown;
        }

        protected virtual void OnClientStarted(TcpClientStartedEventArgs e)
        {
            this.ClientStarted?.Invoke(this, e);
        }

        protected virtual void OnClientStopped(TcpClientStoppedEventArgs e)
        {
            this.ClientStopped?.Invoke(this, e);
        }

        protected virtual void OnConnectionEstablished(ConnectionEstablishedEventArgs e)
        {
            this.ConnectionEstablished?.Invoke(this, e);
        }

        protected virtual void OnFrameArrived(RemoteTcpPeer remoteTcpPeer, TcpFrameArrivedEventArgs e)
        {
            try
            {
                remoteTcpPeer.OnFrameArrived(e);

                this.FrameArrived?.Invoke(this, e);
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

        protected virtual void OnClientErrorOccured(TcpClientErrorEventArgs e)
        {
            this.ClientErrorOccured?.Invoke(this, e);
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
