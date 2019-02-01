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
    public class AsyncNetUdpClient : IAsyncNetUdpClient
    {
        public AsyncNetUdpClient(string targetHostname, int targetPort) : this(new AsyncNetUdpClientConfig()
        {
            TargetHostname = targetHostname,
            TargetPort = targetPort
        })
        {
        }

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

        public event EventHandler<UdpClientStartedEventArgs> ClientStarted;

        public event EventHandler<UdpClientStoppedEventArgs> ClientStopped;

        public event EventHandler<UdpClientErrorEventArgs> ClientErrorOccured;

        public event EventHandler<UdpPacketArrivedEventArgs> UdpPacketArrived;

        public event EventHandler<UdpSendErrorEventArgs> UdpSendErrorOccured;

        public IObservable<UdpClientStartedData> WhenClientStarted =>
            Observable.FromEventPattern<UdpClientStartedEventArgs>(
                h => this.ClientStarted += h, h => this.ClientStarted -= h)
            .Select(x => x.EventArgs.UdpClientStartedData);

        public IObservable<UdpClientStoppedData> WhenClientStopped =>
            Observable.FromEventPattern<UdpClientStoppedEventArgs>(
                h => this.ClientStopped += h, h => this.ClientStopped -= h)
            .Select(x => x.EventArgs.UdpClientStoppedData);

        public IObservable<ErrorData> WhenClientErrorOccured =>
            Observable.FromEventPattern<UdpClientErrorEventArgs>(
                h => this.ClientErrorOccured += h, h => this.ClientErrorOccured -= h)
            .TakeUntil(this.WhenClientStopped)
            .Select(x => x.EventArgs.ErrorData);

        public IObservable<UdpPacketArrivedData> WhenUdpPacketArrived =>
            Observable.FromEventPattern<UdpPacketArrivedEventArgs>(
                h => this.UdpPacketArrived += h, h => this.UdpPacketArrived -= h)
            .TakeUntil(this.WhenClientStopped)
            .Select(x => x.EventArgs.UdpPacketArrivedData);

        public IObservable<UdpSendErrorData> WhenUdpSendErrorOccured =>
            Observable.FromEventPattern<UdpSendErrorEventArgs>(
                h => this.UdpSendErrorOccured += h, h => this.UdpSendErrorOccured -= h)
            .TakeUntil(this.WhenClientStopped)
            .Select(x => x.EventArgs.UdpSendErrorData);

        public virtual UdpClient UdpClient { get; protected set; }

        protected virtual IPEndPoint TargetEndPoint { get; set; }

        protected virtual AsyncNetUdpClientConfig Config { get; set; }

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
                this.OnClientErrorOccured(new UdpClientErrorEventArgs(new ErrorData(ex)));

                return;
            }

            this.SendQueueActionBlock = this.CreateSendQueueActionBlock(cancellationToken);
            this.CancellationToken = cancellationToken;

            this.OnClientStarted(new UdpClientStartedEventArgs(new UdpClientStartedData(this.Config.TargetHostname, this.Config.TargetPort)));

            IPAddress[] addresses;

            try
            {
                addresses = await this.GetHostAddresses(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                this.OnClientStopped(new UdpClientStoppedEventArgs(new UdpClientStoppedData(this.Config.TargetHostname, this.Config.TargetPort)));

                return;
            }
            catch (Exception ex)
            {
                this.OnClientErrorOccured(new UdpClientErrorEventArgs(new ErrorData(ex)));

                this.OnClientStopped(new UdpClientStoppedEventArgs(new UdpClientStoppedData(this.Config.TargetHostname, this.Config.TargetPort)));

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

                return;
            }

            try
            {
                await this.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.OnClientErrorOccured(new UdpClientErrorEventArgs(new ErrorData(ex)));

                return;
            }
            finally
            {
                this.OnClientStopped(new UdpClientStoppedEventArgs(new UdpClientStoppedData(this.Config.TargetHostname, this.Config.TargetPort)));

                this.UdpClient.Dispose();
            }
        }

        public virtual bool Post(byte[] data)
        {
            return this.Post(data, 0, data.Length);
        }

        public virtual bool Post(byte[] data, int offset, int length)
        {
            return this.SendQueueActionBlock.Post(new UdpOutgoingPacket(this.TargetEndPoint, new Core.AsyncNetBuffer(data, offset, length)));
        }

        public virtual Task<bool> SendAsync(byte[] data)
        {
            return this.SendAsync(data, 0, data.Length);
        }

        public virtual Task<bool> SendAsync(byte[] data, int offset, int length)
        {
            return this.SendQueueActionBlock.SendAsync(new UdpOutgoingPacket(this.TargetEndPoint, new Core.AsyncNetBuffer(data, offset, length)));
        }

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
