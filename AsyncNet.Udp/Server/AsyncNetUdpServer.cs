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
    public class AsyncNetUdpServer : IAsyncNetUdpServer
    {
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

        public event EventHandler<UdpServerStartedEventArgs> ServerStarted;

        public event EventHandler<UdpServerStoppedEventArgs> ServerStopped;

        public event EventHandler<UdpServerErrorEventArgs> ServerErrorOccured;

        public event EventHandler<UdpPacketArrivedEventArgs> UdpPacketArrived;

        public event EventHandler<UdpSendErrorEventArgs> UdpSendErrorOccured;

        public IObservable<UdpServerStartedData> WhenServerStarted =>
            Observable.FromEventPattern<UdpServerStartedEventArgs>(
                h => this.ServerStarted += h, h => this.ServerStarted -= h)
            .Select(x => x.EventArgs.UdpServerStartedData);

        public IObservable<UdpServerStoppedData> WhenServerStopped =>
            Observable.FromEventPattern<UdpServerStoppedEventArgs>(
                h => this.ServerStopped += h, h => this.ServerStopped -= h)
            .Select(x => x.EventArgs.UdpServerStoppedData);

        public IObservable<ErrorData> WhenServerErrorOccured =>
            Observable.FromEventPattern<UdpServerErrorEventArgs>(
                h => this.ServerErrorOccured += h, h => this.ServerErrorOccured -= h)
            .TakeUntil(this.WhenServerStopped)
            .Select(x => x.EventArgs.ErrorData);

        public IObservable<UdpPacketArrivedData> WhenUdpPacketArrived =>
            Observable.FromEventPattern<UdpPacketArrivedEventArgs>(
                h => this.UdpPacketArrived += h, h => this.UdpPacketArrived -= h)
            .TakeUntil(this.WhenServerStopped)
            .Select(x => x.EventArgs.UdpPacketArrivedData);

        public IObservable<UdpSendErrorData> WhenUdpSendErrorOccured =>
            Observable.FromEventPattern<UdpSendErrorEventArgs>(
                h => this.UdpSendErrorOccured += h, h => this.UdpSendErrorOccured -= h)
            .TakeUntil(this.WhenServerStopped)
            .Select(x => x.EventArgs.UdpSendErrorData);

        public virtual UdpClient UdpClient { get; protected set; }

        protected virtual AsyncNetUdpServerConfig Config { get; set; }

        protected virtual ActionBlock<UdpOutgoingPacket> SendQueueActionBlock { get; set; }

        protected virtual CancellationToken CancellationToken { get; set; }

        public virtual Task StartAsync()
        {
            return this.StartAsync(CancellationToken.None);
        }

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

            this.OnServerStarted(new UdpServerStartedEventArgs(new UdpServerStartedData(this.Config.IPAddress, this.Config.Port)));

            try
            {
                await this.ReceiveAsync(cancellationToken).ConfigureAwait(false);
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

                this.UdpClient.Dispose();
            }
        }

        public virtual bool Post(byte[] data, IPEndPoint remoteEndPoint)
        {
            return this.Post(data, 0, data.Length, remoteEndPoint);
        }

        public virtual bool Post(byte[] data, int offset, int length, IPEndPoint remoteEndPoint)
        {
            return this.SendQueueActionBlock.Post(new UdpOutgoingPacket(remoteEndPoint, new Core.AsyncNetBuffer(data, offset, length)));
        }

        public virtual Task<bool> SendAsync(byte[] data, IPEndPoint remoteEndPoint)
        {
            return this.SendAsync(data, 0, data.Length, remoteEndPoint);
        }

        public virtual Task<bool> SendAsync(byte[] data, int offset, int length, IPEndPoint remoteEndPoint)
        {
            return this.SendQueueActionBlock.SendAsync(new UdpOutgoingPacket(remoteEndPoint, new Core.AsyncNetBuffer(data, offset, length)));
        }

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
