﻿using System;

namespace AsyncNet.Tcp
{
    public class AsyncTcpServerConfig
    {
        public int ReceiveBufferSize { get; set; } = 4096;

        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.Zero;

        public int MaxSendQueuePerPeerSize { get; set; } = 10000;
    }
}