using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AsyncNet.Core.Error;
using AsyncNet.Core.Extensions;
using AsyncNet.Udp.Client.SystemEvent;
using AsyncNet.Udp.Error;
using AsyncNet.Udp.Error.SystemEvent;
using AsyncNet.Udp.Extensions;
using AsyncNet.Udp.Remote;
using AsyncNet.Udp.Remote.SystemEvent;

namespace AsyncNet.Udp.Client
{
    /// <summary>
    /// An implementation of asynchronous UDP client
    /// </summary>
    public class AsyncNetUdpClient : IAsyncNetUdpClient
    {
        /// <summary>
        /// Constructs UDP client that connects to the particular server and has default configuration
        /// </summary>
        /// <param name="targetHostname">Server hostname</param>
        /// <param name="targetPort">Server port</param>
        public AsyncNetUdpClient(string targetHostname, int targetPort) : this(new AsyncNetUdpClientConfig()
        {
            TargetHostname = targetHostname,
            TargetPort = targetPort
        })
        {
        }

        /// <summary>
        /// Constructs UDP client with custom configuration
        /// </summary>
        /// <param name="config">UDP client configuration</param>
        public AsyncNetUdpClient(AsyncNetUdpClientConfig config)
        {
            this.Config = new AsyncNetUdpClientConfig()
            {
                TargetHostname = config.TargetHostname,
                TargetPort = config.TargetPort,
                MaxSendQueueSize = config.MaxSendQueueSize,
                ConfigureUdpClientCallback = config.ConfigureUdpClientCallback,
                SelectIpAddressCallback = config.SelectIpAddressCallback
            };
        }

        /// <summary>
        /// Fires when client started running
        /// </summary>
        public event EventHandler<UdpClientStartedEventArgs> ClientStarted;

        /// <summary>
        /// Fires when client is ready for sending and receiving packets
        /// </summary>
        public event EventHandler<UdpClientReadyEventArgs> ClientReady;

        /// <summary>
        /// Fires when client stopped running
        /// </summary>
        public event EventHandler<UdpClientStoppedEventArgs> ClientStopped;

        /// <summary>
        /// Fires when there was a problem with the client
        /// </summary>
        public event EventHandler<UdpClientErrorEventArgs> ClientErrorOccured;

        /// <summary>
        /// Fires when packet arrived from server
        /// </summary>
        public event EventHandler<UdpPacketArrivedEventArgs> UdpPacketArrived;

        /// <summary>
        /// Fires when there was a problem while sending packet to the target server
        /// </summary>
        public event EventHandler<UdpSendErrorEventArgs> UdpSendErrorOccured;

        /// <summary>
        /// Produces an element when client started running
        /// </summary>
        public IObservable<UdpClientStartedData> WhenClientStarted =>
            Observable.FromEventPattern<UdpClientStartedEventArgs>(
                h => this.ClientStarted += h, h => this.ClientStarted -= h)
            .Select(x => x.EventArgs.UdpClientStartedData);

        /// <summary>
        /// Produces an element when client is ready for sending and receiving packets
        /// </summary>
        public IObservable<UdpClientReadyData> WhenClientReady =>
            Observable.FromEventPattern<UdpClientReadyEventArgs>(
                h => this.ClientReady += h, h => this.ClientReady -= h)
            .Select(x => x.EventArgs.UdpClientReadyData);

        /// <summary>
        /// Produces an element when client stopped running
        /// </summary>
        public IObservable<UdpClientStoppedData> WhenClientStopped =>
            Observable.FromEventPattern<UdpClientStoppedEventArgs>(
                h => this.ClientStopped += h, h => this.ClientStopped -= h)
            .Select(x => x.EventArgs.UdpClientStoppedData);

        /// <summary>
        /// Produces an element when there was a problem with the client
        /// </summary>
        public IObservable<ErrorData> WhenClientErrorOccured =>
            Observable.FromEventPattern<UdpClientErrorEventArgs>(
                h => this.ClientErrorOccured += h, h => this.ClientErrorOccured -= h)
            .TakeUntil(this.WhenClientStopped)
            .Select(x => x.EventArgs.ErrorData);

        /// <summary>
        /// Produces an element when packet arrived from server
        /// </summary>
        public IObservable<UdpPacketArrivedData> WhenUdpPacketArrived =>
            Observable.FromEventPattern<UdpPacketArrivedEventArgs>(
                h => this.UdpPacketArrived += h, h => this.UdpPacketArrived -= h)
            .TakeUntil(this.WhenClientStopped)
            .Select(x => x.EventArgs.UdpPacketArrivedData);

        /// <summary>
        /// Produces an element when there was a problem while sending packet to the target server
        /// </summary>
        public IObservable<UdpSendErrorData> WhenUdpSendErrorOccured =>
            Observable.FromEventPattern<UdpSendErrorEventArgs>(
                h => this.UdpSendErrorOccured += h, h => this.UdpSendErrorOccured -= h)
            .TakeUntil(this.WhenClientStopped)
            .Select(x => x.EventArgs.UdpSendErrorData);

        /// <summary>
        /// Underlying <see cref="UdpClient" />
        /// </summary>
        public virtual UdpClient UdpClient { get; protected set; }

        /// <summary>
        /// Asynchronously starts the client that will run indefinitely
        /// </summary>
        /// <returns><see cref="Task" /></returns>
        public virtual Task StartAsync()
        {
            return this.StartAsync(CancellationToken.None);
        }

        /// <summary>
        /// Asynchronously starts the client that will run until <paramref name="cancellationToken" /> is cancelled
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="Task" /></returns>
        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.UdpClient = this.CreateUdpClient();
            }
            catch (Exception ex)
            {
                this.OnClientErrorOccured(new UdpClientErrorEventArgs(new ErrorData(ex)));

                return;
            }

            this.SendQueueActionBlock = this.CreateSendQueueActionBlock(cancellationToken);
            this.CancellationToken = cancellationToken;

            this.Config.ConfigureUdpClientCallback?.Invoke(this.UdpClient);

            this.OnClientStarted(new UdpClientStartedEventArgs(new UdpClientStartedData(this.Config.TargetHostname, this.Config.TargetPort)));

            IPAddress[] addresses;

            try
            {
                addresses = await this.GetHostAddresses(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                this.OnClientStopped(new UdpClientStoppedEventArgs(new UdpClientStoppedData(this.Config.TargetHostname, this.Config.TargetPort)));

                this.SendQueueActionBlock.Complete();
                this.UdpClient.Dispose();

                return;
            }
            catch (Exception ex)
            {
                this.OnClientErrorOccured(new UdpClientErrorEventArgs(new ErrorData(ex)));

                this.OnClientStopped(new UdpClientStoppedEventArgs(new UdpClientStoppedData(this.Config.TargetHostname, this.Config.TargetPort)));

                this.SendQueueActionBlock.Complete();
                this.UdpClient.Dispose();

                return;
            }

            try
            {
                this.Connect(addresses);
            }
            catch (Exception ex)
            {
                this.OnClientErrorOccured(new UdpClientErrorEventArgs(new ErrorData(ex)));

                this.OnClientStopped(new UdpClientStoppedEventArgs(new UdpClientStoppedData(this.Config.TargetHostname, this.Config.TargetPort)));

                this.SendQueueActionBlock.Complete();
                this.UdpClient.Dispose();

                return;
            }

            try
            {
                await Task.WhenAll(
                    this.ReceiveAsync(cancellationToken),
                    Task.Run(() => this.OnClientReady(new UdpClientReadyEventArgs(new UdpClientReadyData(this, this.Config.TargetHostname, this.Config.TargetPort)))))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.OnClientErrorOccured(new UdpClientErrorEventArgs(new ErrorData(ex)));

                return;
            }
            finally
            {
                this.OnClientStopped(new UdpClientStoppedEventArgs(new UdpClientStoppedData(this.Config.TargetHostname, this.Config.TargetPort)));

                this.SendQueueActionBlock.Complete();
                this.UdpClient.Dispose();
            }
        }

        /// <summary>
        /// Adds data to the send queue. It will fail if send queue buffer is full returning false
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <returns>True - added to the send queue. False - send queue buffer is full or client is stopped</returns>
        public virtual bool Post(byte[] data)
        {
            return this.Post(data, 0, data.Length);
        }

        /// <summary>
        /// Adds data to the send queue. It will fail if send queue buffer is full returning false
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <returns>True - added to the send queue. False - send queue buffer is full or client is stopped</returns>
        public virtual bool Post(byte[] buffer, int offset, int count)
        {
            return this.SendQueueActionBlock.Post(new UdpOutgoingPacket(this.TargetEndPoint, new Core.AsyncNetBuffer(buffer, offset, count)));
        }

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <returns>True - added to the send queue. False - client is stopped</returns>
        public virtual Task<bool> SendAsync(byte[] data)
        {
            return this.SendAsync(data, 0, data.Length);
        }

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <returns>True - added to the send queue. False - client is stopped</returns>
        public virtual Task<bool> SendAsync(byte[] buffer, int offset, int count)
        {
            return this.SendQueueActionBlock.SendAsync(new UdpOutgoingPacket(this.TargetEndPoint, new Core.AsyncNetBuffer(buffer, offset, count)));
        }

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns>True - added to the send queue. False - client is stopped</returns>
        public Task<bool> SendAsync(byte[] data, CancellationToken cancellationToken)
        {
            return this.SendAsync(data, 0, data.Length, cancellationToken);
        }

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns>True - added to the send queue. False - client is stopped</returns>
        public async Task<bool> SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            bool result;

            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(this.CancellationToken, cancellationToken))
            {
                try
                {
                    result = await this.SendQueueActionBlock.SendAsync(
                        new UdpOutgoingPacket(this.TargetEndPoint, new Core.AsyncNetBuffer(buffer, offset, count)),
                        linkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    result = false;
                }
            }

            return result;
        }

        protected virtual IPEndPoint TargetEndPoint { get; set; }

        protected virtual AsyncNetUdpClientConfig Config { get; set; }

        protected virtual ActionBlock<UdpOutgoingPacket> SendQueueActionBlock { get; set; }

        protected virtual CancellationToken CancellationToken { get; set; }

        protected virtual UdpClient CreateUdpClient()
        {
            return new UdpClient();
        }

        protected virtual void Connect(IPAddress[] addresses)
        {
            IPAddress selected;

            if (addresses != null || addresses.Length > 0)
            {
                if (this.Config.SelectIpAddressCallback != null)
                {
                    selected = this.Config.SelectIpAddressCallback(addresses);
                }
                else
                {
                    selected = this.SelectDefaultIpAddress(addresses);
                }

                this.UdpClient.Connect(selected, this.Config.TargetPort);

                this.TargetEndPoint = new IPEndPoint(selected, this.Config.TargetPort);
            }
            else
            {
                this.UdpClient.Connect(this.Config.TargetHostname, this.Config.TargetPort);

                this.TargetEndPoint = new IPEndPoint(IPAddress.Any, this.Config.TargetPort);
            }
        }

        protected virtual Task<IPAddress[]> GetHostAddresses(CancellationToken cancellationToken)
        {
            return DnsExtensions.GetHostAddressesWithCancellationTokenAsync(this.Config.TargetHostname, cancellationToken);
        }

        protected virtual IPAddress SelectDefaultIpAddress(IPAddress[] addresses)
        {
            return addresses[0];
        }

        protected virtual ActionBlock<UdpOutgoingPacket> CreateSendQueueActionBlock(CancellationToken token)
        {
            return new ActionBlock<UdpOutgoingPacket>(
                this.SendPacketAsync,
                new ExecutionDataflowBlockOptions()
                {
                    EnsureOrdered = true,
                    BoundedCapacity = this.Config.MaxSendQueueSize,
                    MaxDegreeOfParallelism = 1,
                    CancellationToken = token
                });
        }

        protected virtual async Task ReceiveAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await this.UdpClient.ReceiveWithCancellationTokenAsync(cancellationToken).ConfigureAwait(false);

                    this.OnUdpPacketArrived(new UdpPacketArrivedEventArgs(new UdpPacketArrivedData(result.RemoteEndPoint, result.Buffer)));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        protected virtual async Task SendPacketAsync(UdpOutgoingPacket packet)
        {
            int numberOfBytesSent;

            try
            {
                if (packet.Buffer.Offset == 0)
                {
                    numberOfBytesSent = await this.UdpClient.SendWithCancellationTokenAsync(packet.Buffer.Memory, packet.Buffer.Count, this.CancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var bytes = packet.Buffer.ToBytes();

                    numberOfBytesSent = await this.UdpClient.SendWithCancellationTokenAsync(bytes, bytes.Length, this.CancellationToken).ConfigureAwait(false);
                }

                if (numberOfBytesSent != packet.Buffer.Count)
                {
                    this.OnUdpSendErrorOccured(new UdpSendErrorEventArgs(new UdpSendErrorData(packet, numberOfBytesSent, null)));
                }
            }
            catch (Exception ex)
            {
                this.OnUdpSendErrorOccured(new UdpSendErrorEventArgs(new UdpSendErrorData(packet, 0, ex)));
            }
        }

        protected virtual void OnClientStarted(UdpClientStartedEventArgs e)
        {
            this.ClientStarted?.Invoke(this, e);
        }

        protected virtual void OnClientReady(UdpClientReadyEventArgs e)
        {
            this.ClientReady?.Invoke(this, e);
        }

        protected virtual void OnClientStopped(UdpClientStoppedEventArgs e)
        {
            this.ClientStopped?.Invoke(this, e);
        }

        protected virtual void OnClientErrorOccured(UdpClientErrorEventArgs e)
        {
            this.ClientErrorOccured?.Invoke(this, e);
        }

        protected virtual void OnUdpPacketArrived(UdpPacketArrivedEventArgs e)
        {
            this.UdpPacketArrived?.Invoke(this, e);
        }

        protected virtual void OnUdpSendErrorOccured(UdpSendErrorEventArgs e)
        {
            this.UdpSendErrorOccured?.Invoke(this, e);
        }
    }
}
