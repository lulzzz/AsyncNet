using AsyncNet.Core;

namespace AsyncNet.Tcp
{
    public class ConnectionEstablishedData : ITransportContext
    {
        public ConnectionEstablishedData(IRemotePeer remotePeer)
        {
            this.RemotePeer = remotePeer;
        }

        public IRemotePeer RemotePeer { get; }
    }
}
