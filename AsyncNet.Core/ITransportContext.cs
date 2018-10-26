namespace AsyncNet.Core
{
    public interface ITransportContext
    {
        IRemotePeer RemotePeer { get; }
    }
}
