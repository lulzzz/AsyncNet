namespace AsyncNet.Tcp
{
    public class ReceiveResult
    {
        public ReceiveResult(int receivedLength)
        {
            this.ReceivedDataLength = receivedLength;
        }

        public ReceiveResult(ConnectionCloseReason connectionCloseReason)
        {
            this.ConnectionCloseReason = ConnectionCloseReason;
        }

        public int ReceivedDataLength { get; }

        public ConnectionCloseReason ConnectionCloseReason { get; }
    }
}
