using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Core.Error;
using AsyncNet.Udp.Error;
using AsyncNet.Udp.Error.SystemEvent;
using AsyncNet.Udp.Remote;
using AsyncNet.Udp.Remote.SystemEvent;
using AsyncNet.Udp.Server.SystemEvent;

namespace AsyncNet.Udp.Server
{
    public interface IAsyncNetUdpServer
    {
        UdpClient UdpClient { get; }
        IObservable<ErrorData> WhenServerErrorOccured { get; }
        IObservable<UdpServerStartedData> WhenServerStarted { get; }
        IObservable<UdpServerStoppedData> WhenServerStopped { get; }
        IObservable<UdpPacketArrivedData> WhenUdpPacketArrived { get; }
        IObservable<UdpSendErrorData> WhenUdpSendErrorOccured { get; }

        event EventHandler<UdpServerErrorEventArgs> ServerErrorOccured;
        event EventHandler<UdpServerStartedEventArgs> ServerStarted;
        event EventHandler<UdpServerStoppedEventArgs> ServerStopped;
        event EventHandler<UdpPacketArrivedEventArgs> UdpPacketArrived;
        event EventHandler<UdpSendErrorEventArgs> UdpSendErrorOccured;

        bool Post(byte[] data, int offset, int length, IPEndPoint remoteEndPoint);
        bool Post(byte[] data, IPEndPoint remoteEndPoint);
        Task<bool> SendAsync(byte[] data, int offset, int length, IPEndPoint remoteEndPoint);
        Task<bool> SendAsync(byte[] data, IPEndPoint remoteEndPoint);
        Task StartAsync();
        Task StartAsync(CancellationToken cancellationToken);
    }
}