using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Core.Error;
using AsyncNet.Core.Error.SystemEvent;
using AsyncNet.Tcp.Client.SystemEvent;
using AsyncNet.Tcp.Connection;
using AsyncNet.Tcp.Connection.SystemEvent;
using AsyncNet.Tcp.Error;
using AsyncNet.Tcp.Error.SystemEvent;
using AsyncNet.Tcp.Remote.SystemEvent;

namespace AsyncNet.Tcp.Client
{
    public interface IAsyncNetTcpClient
    {
        IObservable<ErrorData> WhenClientErrorOccured { get; }
        IObservable<TcpClientStartedData> WhenClientStarted { get; }
        IObservable<TcpClientStoppedData> WhenClientStopped { get; }
        IObservable<ConnectionClosedData> WhenConnectionClosed { get; }
        IObservable<ConnectionEstablishedData> WhenConnectionEstablished { get; }
        IObservable<RemoteTcpPeerErrorData> WhenRemoteTcpPeerErrorOccured { get; }
        IObservable<ErrorData> WhenUnhandledErrorOccured { get; }

        event EventHandler<TcpClientErrorEventArgs> ClientErrorOccured;
        event EventHandler<TcpClientStartedEventArgs> ClientStarted;
        event EventHandler<TcpClientStoppedEventArgs> ClientStopped;
        event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;
        event EventHandler<ConnectionEstablishedEventArgs> ConnectionEstablished;
        event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;
        event EventHandler<RemoteTcpPeerErrorEventArgs> RemoteTcpPeerErrorOccured;
        event EventHandler<UnhandledErrorEventArgs> UnhandledErrorOccured;

        Task StartAsync();
        Task StartAsync(CancellationToken cancellationToken);
    }
}