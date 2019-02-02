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
    /// <summary>
    /// An interface for asynchronous TCP server
    /// </summary>
    public interface IAsyncNetTcpServer
    {
        /// <summary>
        /// Produces an element when connection closes for particular client/peer
        /// </summary>
        IObservable<ConnectionClosedData> WhenConnectionClosed { get; }
        
        /// <summary>
        /// Produces an element when new client/peer connects to the server
        /// </summary>
        IObservable<ConnectionEstablishedData> WhenConnectionEstablished { get; }

        /// <summary>
        /// Produces an element when TCP frame arrived from particular client/peer
        /// </summary>
        IObservable<TcpFrameArrivedData> WhenFrameArrived { get; }

        /// <summary>
        /// Produces an element when there was an error while handling particular client/peer
        /// </summary>
        IObservable<RemoteTcpPeerErrorData> WhenRemoteTcpPeerErrorOccured { get; }

        /// <summary>
        /// Produces an element when there was a problem with the server
        /// </summary>
        IObservable<ErrorData> WhenServerErrorOccured { get; }

        /// <summary>
        /// Produces an element when server started running
        /// </summary>
        IObservable<TcpServerStartedData> WhenServerStarted { get; }
        
        /// <summary>
        /// Produces an element when server stopped running 
        /// </summary>
        IObservable<TcpServerStoppedData> WhenServerStopped { get; }

        /// <summary>
        /// Produces an element when unhandled error occured - e.g. when event subscriber throws an exception
        /// </summary>
        IObservable<ErrorData> WhenUnhandledErrorOccured { get; }

        /// <summary>
        /// Fires when connection closes for particular client/peer
        /// </summary>
        event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        /// <summary>
        /// Fires when new client/peer connects to the server
        /// </summary>
        event EventHandler<ConnectionEstablishedEventArgs> ConnectionEstablished;

        /// <summary>
        /// Fires when TCP frame arrived from particular client/peer
        /// </summary>
        event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;

        /// <summary>
        /// Fires when there was an error while handling particular client/peer
        /// </summary>
        event EventHandler<RemoteTcpPeerErrorEventArgs> RemoteTcpPeerErrorOccured;

        /// <summary>
        /// Fires when there was a problem with the server
        /// </summary>
        event EventHandler<TcpServerErrorEventArgs> ServerErrorOccured;

        /// <summary>
        /// Fires when server started running
        /// </summary>
        event EventHandler<TcpServerStartedEventArgs> ServerStarted;

        /// <summary>
        /// Fires when server stopped running 
        /// </summary>
        event EventHandler<TcpServerStoppedEventArgs> ServerStopped;

        /// <summary>
        /// Fires when unhandled error occured - e.g. when event subscriber throws an exception
        /// </summary>
        event EventHandler<UnhandledErrorEventArgs> UnhandledErrorOccured;

        /// <summary>
        /// Asynchronously starts the server that will run indefinitely
        /// </summary>
        /// <returns><see cref="System.Threading.Tasks.Task"/></returns>
        Task StartAsync();

        /// <summary>
        /// Asynchronously starts the server that will run until <paramref name="cancellationToken"/> is cancelled
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="System.Threading.Tasks.Task"/></returns>
        Task StartAsync(CancellationToken cancellationToken);
    }
}