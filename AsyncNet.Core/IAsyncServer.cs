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

        IObservable<ServerStartedData> WhenServerStarted { get; }

        IObservable<ServerStoppedData> WhenServerStopped { get; }

        IObservable<TransportData> WhenFrameArrived { get; }

        IObservable<UnhandledErrorData> WhenUnhandledErrorOccured { get; }

        Task StartAsync();

        Task StartAsync(CancellationToken cancellationToken);
    }
}
