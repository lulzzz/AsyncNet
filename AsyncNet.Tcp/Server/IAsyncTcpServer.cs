using System;
using AsyncNet.Core;

namespace AsyncNet.Tcp.Server
{
    public interface IAsyncTcpServer : IAsyncServer
    {
        event EventHandler<ConnectionEstablishedEventArgs> ConnectionEstablished;

        event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        IObservable<ConnectionEstablishedData> WhenConnectionEstablished { get; }

        IObservable<ConnectionClosedData> WhenConnectionClosed { get; }
    }
}
