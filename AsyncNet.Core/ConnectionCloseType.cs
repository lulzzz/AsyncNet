﻿namespace AsyncNet.Core
{
    public enum ConnectionCloseReason
    {
        RemoteShutdown = 0,
        LocalShutdown = 1,
        Timeout = 2,
        Unknown = 3
    }
}