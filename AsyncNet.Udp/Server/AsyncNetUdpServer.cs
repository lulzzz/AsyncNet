using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using AsyncNet.Udp.Error.SystemEvent;
using AsyncNet.Udp.Server.SystemEvent;
using AsyncNet.Core.Error;
using AsyncNet.Udp.Extensions;
using AsyncNet.Udp.Remote.SystemEvent;
using AsyncNet.Udp.Remote;
using System.Threading.Tasks.Dataflow;
using AsyncNet.Udp.Error;

namespace AsyncNet.Udp.Server
{
    /// <summary>
    /// An implementation of asynchronous UDP server
    /// </summary>
    public class AsyncNetUdpServer : IAsyncNetUdpServer
    {
        /// <summary>
        /// Constructs UDP server that runs on particular port and has default configuration
        /// </summary>
        /// <param name="port">A port that UDP server will run on</param>
        public AsyncNetUdpServer(int port) : this(new AsyncNetUdpServerConfig()
        {
            Port = port
        })
        {
        }

        /// <summary>
        /// Constructs UDP server with custom configuration
        /// </summary>
        /// <param name="config">UDP server configuration</param>
        public AsyncNetUdpServer(AsyncNetUdpServerConfig config)
        {
            this.Config = new AsyncNetUdpServerConfig()
            {
                IPAddress = config.IPAddress,
                Port = config.Port,
                MaxSendQueueSize = config.MaxSendQueueSize,
                ConfigureUdpListenerCallback = config.ConfigureUdpListenerCallback,
                JoinMulticastGroup = config.JoinMulticastGroup,
                JoinMulticastGroupCallback = config.JoinMulticastGroupCallback,
                LeaveMulticastGroupCallback = config.LeaveMulticastGroupCallback
            };
        }

        /// <summary>
        /// Fires when server started running
        /// </summary>
        public event EventHandler<UdpServerStartedEventArgs> ServerStarted;

        /// <summary>
        /// Fires when server stopped running
        /// </summary>
        public event EventHandler<UdpServerStoppedEventArgs> ServerStopped;

        /// <summary>
        /// Fires when there was a problemw with the server
        /// </summary>
        public event EventHandler<UdpServerErrorEventArgs> ServerErrorOccured;

        /// <summary>
        /// Fires when packet arrived from particular client/peer
        /// </summary>
        public event EventHandler<UdpPacketArrivedEventArgs> UdpPacketArrived;

        /// <summary>
        /// Fires when there was a problem while sending packet to the target client/peer
        /// </summary>
        public event EventHandler<UdpSendErrorEventArgs> UdpSendErrorOccured;

        /// <summary>
        /// Produces an element when server started running
        /// </summary>
        public IObservable<UdpServerStartedData> WhenServerStarted =>
            Observable.FromEventPattern<UdpServerStartedEventArgs>(
                h => this.ServerStarted += h, h => this.ServerStarted -= h)
            .Select(x => x.EventArgs.UdpServerStartedData);

        /// <summary>
        /// Produces an element when server stopped running 
        /// </summary>
        public IObservable<UdpServerStoppedData> WhenServerStopped =>
            Observable.FromEventPattern<UdpServerStoppedEventArgs>(
                h => this.ServerStopped += h, h => this.ServerStopped -= h)
            .Select(x => x.EventArgs.UdpServerStoppedData);

        /// <summary>
        /// Produces an element when there was a problemw with the server
        /// </summary>
        public IObservable<ErrorData> WhenServerErrorOccured =>
            Observable.FromEventPattern<UdpServerErrorEventArgs>(
                h => this.ServerErrorOccured += h, h => this.ServerErrorOccured -= h)
            .TakeUntil(this.WhenServerStopped)
            .Select(x => x.EventArgs.ErrorData);

        /// <summary>
        /// Produces an element when packet arrived from particular client/peer
        /// </summary>
        public IObservable<UdpPacketArrivedData> WhenUdpPacketArrived =>
            Observable.FromEventPattern<UdpPacketArrivedEventArgs>(
                h => this.UdpPacketArrived += h, h => this.UdpPacketArrived -= h)
            .TakeUntil(this.WhenServerStopped)
            .Select(x => x.EventArgs.UdpPacketArrivedData);

        /// <summary>
        /// Produces an element when there was a problem while sending packet to the target client/peer
        /// </summary>
        public IObservable<UdpSendErrorData> WhenUdpSendErrorOccured =>
            Observable.FromEventPattern<UdpSendErrorEventArgs>(
                h => this.UdpSendErrorOccured += h, h => this.UdpSendErrorOccured -= h)
            .TakeUntil(this.WhenServerStopped)
            .Select(x => x.EventArgs.UdpSendErrorData);

        /// <summary>
        /// Underlying <see cref="UdpClient" />
        /// </summary>
        public virtual UdpClient UdpClient { get; protected set; }

        /// <summary>
        /// Asynchronously starts the server that will run indefinitely
        /// </summary>
        /// <returns><see cref="Task" /></returns>
        public virtual Task StartAsync()
        {
            return this.StartAsync(CancellationToken.None);
        }

        /// <summary>
        /// Asynchronously starts the server that will run until <paramref name="cancellationToken" /> is cancelled
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="Task" /></returns>
        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.UdpClient = this.CreateUdpClient();
            }
            catch (Exception ex)
            {
                this.OnServerErrorOccured(new UdpServerErrorEventArgs(new ErrorData(ex)));

                return;
            }

            this.Config.ConfigureUdpListenerCallback?.Invoke(this.UdpClient);

            if (this.Config.JoinMulticastGroup)
            {
                this.Config.JoinMulticastGroupCallback?.Invoke(this.UdpClient);
            }

            this.SendQueueActionBlock = this.CreateSendQueueActionBlock(cancellationToken);
            this.CancellationToken = cancellationToken;

            try
            {
                await Task.WhenAll(
                    this.ReceiveAsync(cancellationToken),
                    Task.Run(() => this.OnServerStarted(new UdpServerStartedEventArgs(new UdpServerStartedData(this.Config.IPAddress, this.Config.Port)))))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.OnServerErrorOccured(new UdpServerErrorEventArgs(new ErrorData(ex)));

                return;
            }
            finally
            {
                if (this.Config.JoinMulticastGroup)
                {
                    this.Config.LeaveMulticastGroupCallback?.Invoke(this.UdpClient);
                }

                this.OnServerStopped(new UdpServerStoppedEventArgs(new UdpServerStoppedData(this.Config.IPAddress, this.Config.Port)));

                this.SendQueueActionBlock.Complete();
                this.UdpClient.Dispose();
            }
        }

        /// <summary>
        /// Adds data to the send queue. It will fail if send queue buffer is full returning false
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="remoteEndPoint">Client/peer endpoint</param>
        /// <returns>True - added to the send queue. False - send queue buffer is full or server is stopped</returns>
        public virtual bool Post(byte[] data, IPEndPoint remoteEndPoint)
        {
            return this.Post(data, 0, data.Length, remoteEndPoint);
        }

        /// <summary>
        /// Adds data to the send queue. It will fail if send queue buffer is full returning false
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="remoteEndPoint">Client/peer endpoint</param>
        /// <returns>True - added to the send queue. False - send queue buffer is full or server is stopped</returns>
        public virtual bool Post(byte[] buffer, int offset, int count, IPEndPoint remoteEndPoint)
        {
            return this.SendQueueActionBlock.Post(new UdpOutgoingPacket(remoteEndPoint, new Core.AsyncNetBuffer(buffer, offset, count)));
        }

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="remoteEndPoint">Client/peer endpoint</param>
        /// <returns>True - added to the send queue. False - server is stopped</returns>
        public virtual Task<bool> SendAsync(byte[] data, IPEndPoint remoteEndPoint)
        {
            return this.SendAsync(data, 0, data.Length, remoteEndPoint);
        }

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="remoteEndPoint">Client/peer endpoint</param>
        /// <returns>True - added to the send queue. False - server is stopped</returns>
        public virtual Task<bool> SendAsync(byte[] buffer, int offset, int count, IPEndPoint remoteEndPoint)
        {
            return this.SendQueueActionBlock.SendAsync(new UdpOutgoingPacket(remoteEndPoint, new Core.AsyncNetBuffer(buffer, offset, count)));
        }

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="remoteEndPoint">Client/peer endpoint</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns>True - added to the send queue. False - server is stopped</returns>
        public Task<bool> SendAsync(byte[] data, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            return this.SendAsync(data, 0, data.Length, remoteEndPoint, cancellationToken);
        }

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="remoteEndPoint">Client/peer endpoint</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns>True - added to the send queue. False - server is stopped</returns>
        public async Task<bool> SendAsync(byte[] buffer, int offset, int count, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            bool result;

            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(this.CancellationToken, cancellationToken))
            {
                try
                {
                    result = await this.SendQueueActionBlock.SendAsync(
                        new UdpOutgoingPacket(remoteEndPoint, new Core.AsyncNetBuffer(buffer, offset, count)),
                        linkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    result = false;
                }
            }

            return result;
        }

        protected virtual AsyncNetUdpServerConfig Config { get; set; }

        protected virtual ActionBlock<UdpOutgoingPacket> SendQueueActionBlock { get; set; }

        protected virtual CancellationToken CancellationToken { get; set; }

        protected virtual UdpClient CreateUdpClient()
        {
            return new UdpClient(new IPEndPoint(this.Config.IPAddress, this.Config.Port));
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

        protected virtual async Task SendPacketAsync(UdpOutgoingPacket packet)
        {
            int numberOfBytesSent;

            try
            {
                if (packet.Buffer.Offset == 0)
                {
                    numberOfBytesSent = await this.UdpClient.SendWithCancellationTokenAsync(packet.Buffer.Memory, packet.Buffer.Count, packet.RemoteEndPoint, this.CancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var bytes = packet.Buffer.ToBytes();

                    numberOfBytesSent = await this.UdpClient.SendWithCancellationTokenAsync(bytes, bytes.Length, packet.RemoteEndPoint, this.CancellationToken).ConfigureAwait(false);
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

        protected virtual void OnServerStarted(UdpServerStartedEventArgs e)
        {
            this.ServerStarted?.Invoke(this, e);
        }

        protected virtual void OnServerStopped(UdpServerStoppedEventArgs e)
        {
            this.ServerStopped?.Invoke(this, e);
        }

        protected virtual void OnServerErrorOccured(UdpServerErrorEventArgs e)
        {
            this.ServerErrorOccured?.Invoke(this, e);
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
