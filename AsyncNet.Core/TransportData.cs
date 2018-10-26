namespace AsyncNet.Core
{
    public class TransportData : ITransportContext
    {
        public TransportData(IRemotePeer remotePeer, ReceivedData receivedData)
        {
            this.RemotePeer = remotePeer;
            this.ReceivedData = receivedData;
        }

        public IRemotePeer RemotePeer { get; }

        public ReceivedData ReceivedData { get; }
    }
}
