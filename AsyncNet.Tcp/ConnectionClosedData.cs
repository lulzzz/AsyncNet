﻿using AsyncNet.Core;

namespace AsyncNet.Tcp
{
    public class ConnectionClosedData : ITransportContext
    {
        public ConnectionClosedData(IRemotePeer remotePeer, ConnectionCloseReason connectionCloseReason)
        {
            this.RemotePeer = remotePeer;
            this.ConnectionCloseReason = connectionCloseReason;
        }

        public IRemotePeer RemotePeer { get; }

        public ConnectionCloseReason ConnectionCloseReason { get; }
    }
}
