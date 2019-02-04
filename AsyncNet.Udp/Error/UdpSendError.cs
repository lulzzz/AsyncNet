using System;
using AsyncNet.Core.Error;
using AsyncNet.Udp.Remote;

namespace AsyncNet.Udp.Error
{
    public class UdpSendErrorData : ErrorData
    {
        public UdpSendErrorData(
            UdpOutgoingPacket packet,
            int numberOfBytesWrittenToTheSendBuffer,
            Exception exception) : base(exception)
        {
            this.Packet = packet;
            this.NumberOfBytesWrittenToTheSendBuffer = numberOfBytesWrittenToTheSendBuffer;
        }

        public UdpOutgoingPacket Packet { get; }

        public int NumberOfBytesWrittenToTheSendBuffer { get; }

        public UdpSendErrorType SendErrorType
        {
            get
            {
                if (this.Exception != null)
                {
                    return UdpSendErrorType.Exception;
                }
                else
                {
                    return UdpSendErrorType.SocketSendBufferIsFull;
                }
            }
        }
    }
}
