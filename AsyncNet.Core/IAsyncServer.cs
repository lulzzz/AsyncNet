using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNet.Core
{
    public interface IAsyncServer
    {
        event EventHandler<DataReceivedEventArgs> DataReceived;

        event EventHandler<UnhandledErrorEventArgs> UnhandledErrorOccured;

        IObservable<TransportData> WhenDataReceived { get; }

        IObservable<UnhandledErrorData> WhenUnhandledErrorOccured { get; }

        Task StartAsync(int port);

        Task StartAsync(int port, CancellationToken cancellationToken);

        Task StartAsync(IPAddress ipAddress, int port, CancellationToken cancellationToken);

        Task StartAsync(IPEndPoint ipEndPoint, CancellationToken cancellationToken);
    }
}
