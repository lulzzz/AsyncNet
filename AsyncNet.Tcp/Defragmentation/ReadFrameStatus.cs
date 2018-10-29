namespace AsyncNet.Tcp.Defragmentation
{
    public enum ReadFrameStatus
    {
        Success = 0,
        StreamClosed = 1,
        FrameDropped = 2
    }
}
