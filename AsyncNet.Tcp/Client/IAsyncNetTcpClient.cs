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
using AsyncNet.Tcp.Remote;
using AsyncNet.Tcp.Remote.SystemEvent;

namespace AsyncNet.Tcp.Client
{
    /// <summary>
    /// An interface for asynchronous TCP client
    /// </summary>
    public interface IAsyncNetTcpClient
    {
        /// <summary>
        /// Produces an element when there was a problem with the client
        /// </summary>
        IObservable<ErrorData> WhenClientErrorOccured { get; }
        
        /// <summary>
        /// Produces an element when client started running, but it's not connected yet to the server
        /// </summary>
        IObservable<TcpClientStartedData> WhenClientStarted { get; }

        /// <summary>
        /// Produces an element when client stopped running
        /// </summary>
        IObservable<TcpClientStoppedData> WhenClientStopped { get; }

        /// <summary>
        /// Produces an element when connection with the server closes
        /// </summary>
        IObservable<ConnectionClosedData> WhenConnectionClosed { get; }

        /// <summary>
        /// Produces an element when connection with the server is established
        /// </summary>
        IObservable<ConnectionEstablishedData> WhenConnectionEstablished { get; }

        /// <summary>
        /// Fires when TCP frame arrived from the server
        /// </summary>
        IObservable<TcpFrameArrivedData> WhenFrameArrived { get; }

        /// <summary>
        /// Produces an element when there was a problem while handling communication with the server
        /// </summary>
        IObservable<RemoteTcpPeerErrorData> WhenRemoteTcpPeerErrorOccured { get; }

        /// <summary>
        /// Produces an element when unhandled error occured - e.g. when event subscriber throws an exception
        /// </summary>
        IObservable<ErrorData> WhenUnhandledErrorOccured { get; }

        /// <summary>
        /// Fires when there was a problem with the client
        /// </summary>
        event EventHandler<TcpClientErrorEventArgs> ClientErrorOccured;

        /// <summary>
        /// Fires when client started running, but it's not connected yet to the server
        /// </summary>
        event EventHandler<TcpClientStartedEventArgs> ClientStarted;

        /// <summary>
        /// Fires when client stopped running
        /// </summary>
        event EventHandler<TcpClientStoppedEventArgs> ClientStopped;

        /// <summary>
        /// Fires when connection with the server closes
        /// </summary>
        event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        /// <summary>
        /// Fires when connection with the server is established
        /// </summary>
        event EventHandler<ConnectionEstablishedEventArgs> ConnectionEstablished;

        /// <summary>
        /// Fires when TCP frame arrived from the server
        /// </summary>
        event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;

        /// <summary>
        /// Fires when there was a problem while handling communication with the server
        /// </summary>
        event EventHandler<RemoteTcpPeerErrorEventArgs> RemoteTcpPeerErrorOccured;

        /// <summary>
        /// Fires when unhandled error occured - e.g. when event subscriber throws an exception
        /// </summary>
        event EventHandler<UnhandledErrorEventArgs> UnhandledErrorOccured;

        /// <summary>
        /// Asynchronously starts the client that will run until connection with the server is closed
        /// </summary>
        /// <returns><see cref="System.Threading.Tasks.Task"/></returns>
        Task StartAsync();

        /// <summary>
        /// Asynchronously starts the client that will run until connection with the server is closed or <paramref name="cancellationToken"/> is cancelled
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="System.Threading.Tasks.Task"/></returns>
        Task StartAsync(CancellationToken cancellationToken);
    }
}