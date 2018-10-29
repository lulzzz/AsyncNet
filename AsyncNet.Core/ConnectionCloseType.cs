namespace AsyncNet.Core
{
    public enum ConnectionCloseReason
    {
        NoReason = 0,
        RemoteShutdown = 1,
        LocalShutdown = 2,
        Timeout = 3,
        Unknown = 4
    }
}
