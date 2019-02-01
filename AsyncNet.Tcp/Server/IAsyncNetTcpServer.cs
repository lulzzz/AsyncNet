using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Core.Error;
using AsyncNet.Core.Error.SystemEvent;
using AsyncNet.Tcp.Connection;
using AsyncNet.Tcp.Connection.SystemEvent;
using AsyncNet.Tcp.Error;
using AsyncNet.Tcp.Error.SystemEvent;
using AsyncNet.Tcp.Remote;
using AsyncNet.Tcp.Remote.SystemEvent;
using AsyncNet.Tcp.Server.SystemEvent;

namespace AsyncNet.Tcp.Server
{
    public interface IAsyncNetTcpServer
    {
        IObservable<ConnectionClosedData> WhenConnectionClosed { get; }
        IObservable<ConnectionEstablishedData> WhenConnectionEstablished { get; }
        IObservable<TcpFrameArrivedData> WhenFrameArrived { get; }
        IObservable<RemoteTcpPeerErrorData> WhenRemoteTcpPeerErrorOccured { get; }
        IObservable<ErrorData> WhenServerErrorOccured { get; }
        IObservable<TcpServerStartedData> WhenServerStarted { get; }
        IObservable<TcpServerStoppedData> WhenServerStopped { get; }
        IObservable<ErrorData> WhenUnhandledErrorOccured { get; }

        event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;
        event EventHandler<ConnectionEstablishedEventArgs> ConnectionEstablished;
        event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;
        event EventHandler<RemoteTcpPeerErrorEventArgs> RemoteTcpPeerErrorOccured;
        event EventHandler<TcpServerErrorEventArgs> ServerErrorOccured;
        event EventHandler<TcpServerStartedEventArgs> ServerStarted;
        event EventHandler<TcpServerStoppedEventArgs> ServerStopped;
        event EventHandler<UnhandledErrorEventArgs> UnhandledErrorOccured;

        Task StartAsync();
        Task StartAsync(CancellationToken cancellationToken);
    }
}