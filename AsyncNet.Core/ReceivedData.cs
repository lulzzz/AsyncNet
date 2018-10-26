namespace AsyncNet.Core
{
    public class ReceivedData
    {
        public ReceivedData(byte[] buffer, int dataLength)
        {
            this.Buffer = buffer;
            this.DataLength = dataLength;
        }

        public byte[] Buffer { get; }

        public int DataLength { get; }
    }
}
