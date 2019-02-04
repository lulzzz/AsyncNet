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
    /// <summary>
    /// An interface for asynchronous UDP client
    /// </summary>
    public interface IAsyncNetUdpClient
    {
        /// <summary>
        /// Underlying <see cref="System.Net.Sockets.UdpClient"/>
        /// </summary>
        UdpClient UdpClient { get; }

        /// <summary>
        /// Produces an element when there was a problem with the client
        /// </summary>
        IObservable<ErrorData> WhenClientErrorOccured { get; }

        /// <summary>
        /// Produces an element when client started running
        /// </summary>
        IObservable<UdpClientStartedData> WhenClientStarted { get; }

        /// <summary>
        /// Produces an element when client is ready for sending and receiving packets
        /// </summary>
        IObservable<UdpClientReadyData> WhenClientReady { get; }

        /// <summary>
        /// Produces an element when client stopped running
        /// </summary>
        IObservable<UdpClientStoppedData> WhenClientStopped { get; }

        /// <summary>
        /// Produces an element when packet arrived from server
        /// </summary>
        IObservable<UdpPacketArrivedData> WhenUdpPacketArrived { get; }

        /// <summary>
        /// Produces an element when there was a problem while sending packet to the target server
        /// </summary>
        IObservable<UdpSendErrorData> WhenUdpSendErrorOccured { get; }

        /// <summary>
        /// Fires when there was a problem with the client
        /// </summary>
        event EventHandler<UdpClientErrorEventArgs> ClientErrorOccured;

        /// <summary>
        /// Fires when client started running
        /// </summary>
        event EventHandler<UdpClientStartedEventArgs> ClientStarted;

        /// <summary>
        /// Fires when client is ready for sending and receiving packets
        /// </summary>
        event EventHandler<UdpClientReadyEventArgs> ClientReady;

        /// <summary>
        /// Fires when client stopped running
        /// </summary>
        event EventHandler<UdpClientStoppedEventArgs> ClientStopped;

        /// <summary>
        /// Fires when packet arrived from server
        /// </summary>
        event EventHandler<UdpPacketArrivedEventArgs> UdpPacketArrived;

        /// <summary>
        /// Fires when there was a problem while sending packet to the target server
        /// </summary>
        event EventHandler<UdpSendErrorEventArgs> UdpSendErrorOccured;

        /// <summary>
        /// Adds data to the send queue. It will fail if send queue buffer is full returning false
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <returns>True - added to the send queue. False - send queue buffer is full or client is stopped</returns>
        bool Post(byte[] data);

        /// <summary>
        /// Adds data to the send queue. It will fail if send queue buffer is full returning false
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer"/></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <returns>True - added to the send queue. False - send queue buffer is full or client is stopped</returns>
        bool Post(byte[] buffer, int offset, int count);

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <returns>True - added to the send queue. False - client is stopped</returns>
        Task<bool> AddToSendQueueAsync(byte[] data);

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns>True - added to the send queue. False - client is stopped</returns>
        Task<bool> AddToSendQueueAsync(byte[] data, CancellationToken cancellationToken);

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer"/></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <returns>True - added to the send queue. False - client is stopped</returns>
        Task<bool> AddToSendQueueAsync(byte[] buffer, int offset, int count);

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer"/></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns>True - added to the send queue. False - client is stopped</returns>
        Task<bool> AddToSendQueueAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

        /// <summary>
        /// Sends data asynchronously
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <returns>True - data was sent. False - client is stopped or underlying send buffer is full</returns>
        Task<bool> SendAsync(byte[] data);

        /// <summary>
        /// Sends data asynchronously
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns>True - data was sent. False - client is stopped or underlying send buffer is full</returns>
        Task<bool> SendAsync(byte[] data, CancellationToken cancellationToken);

        /// <summary>
        /// Sends data asynchronously
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer"/></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <returns>True - data was sent. False - client is stopped or underlying send buffer is full</returns>
        Task<bool> SendAsync(byte[] buffer, int offset, int count);

        /// <summary>
        /// Sends data asynchronously
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer"/></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns>True - data was sent. False - client is stopped or underlying send buffer is full</returns>
        Task<bool> SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously starts the client that will run indefinitely
        /// </summary>
        /// <returns><see cref="Task" /></returns>
        Task StartAsync();

        /// <summary>
        /// Asynchronously starts the client that will run until <paramref name="cancellationToken"/> is cancelled
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="Task" /></returns>
        Task StartAsync(CancellationToken cancellationToken);
    }
}