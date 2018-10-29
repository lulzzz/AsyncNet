using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNet.Core
{
    public interface IAsyncServer
    {
        event EventHandler<ServerStartedEventArgs> ServerStarted;

        event EventHandler<ServerStoppedEventArgs> ServerStopped;

        event EventHandler<FrameArrivedEventArgs> FrameArrived;

        event EventHandler<UnhandledErrorEventArgs> UnhandledErrorOccured;

        event EventHandler<ConnectionEstablishedEventArgs> ConnectionEstablished;

        event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        IObservable<ServerStartedData> WhenServerStarted { get; }

        IObservable<ServerStoppedData> WhenServerStopped { get; }

        IObservable<FrameArrivedData> WhenFrameArrived { get; }

        IObservable<UnhandledErrorData> WhenUnhandledErrorOccured { get; }

        IObservable<ConnectionEstablishedData> WhenConnectionEstablished { get; }

        IObservable<ConnectionClosedData> WhenConnectionClosed { get; }

        Task StartAsync();

        Task StartAsync(CancellationToken cancellationToken);
    }
}
