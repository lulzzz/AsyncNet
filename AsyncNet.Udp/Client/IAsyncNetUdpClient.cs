using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Core.Error;
using AsyncNet.Udp.Client.SystemEvent;
using AsyncNet.Udp.Error;
using AsyncNet.Udp.Error.SystemEvent;
using AsyncNet.Udp.Remote;
using AsyncNet.Udp.Remote.SystemEvent;

namespace AsyncNet.Udp.Client
{
    public interface IAsyncNetUdpClient
    {
        UdpClient UdpClient { get; }
        IObservable<ErrorData> WhenClientErrorOccured { get; }
        IObservable<UdpClientStartedData> WhenClientStarted { get; }
        IObservable<UdpClientStoppedData> WhenClientStopped { get; }
        IObservable<UdpPacketArrivedData> WhenUdpPacketArrived { get; }
        IObservable<UdpSendErrorData> WhenUdpSendErrorOccured { get; }

        event EventHandler<UdpClientErrorEventArgs> ClientErrorOccured;
        event EventHandler<UdpClientStartedEventArgs> ClientStarted;
        event EventHandler<UdpClientStoppedEventArgs> ClientStopped;
        event EventHandler<UdpPacketArrivedEventArgs> UdpPacketArrived;
        event EventHandler<UdpSendErrorEventArgs> UdpSendErrorOccured;

        bool Post(byte[] data);
        bool Post(byte[] data, int offset, int length);
        Task<bool> SendAsync(byte[] data);
        Task<bool> SendAsync(byte[] data, int offset, int length);
        Task StartAsync();
        Task StartAsync(CancellationToken cancellationToken);
    }
}