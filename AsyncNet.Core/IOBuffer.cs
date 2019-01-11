namespace AsyncNet.Core
{
    public class IOBuffer
    {
        public IOBuffer(byte[] buffer, int offset, int count)
        {
            this.Buffer = buffer;
            this.Offset = offset;
            this.Count = count;
        }

        public byte[] Buffer { get; }

        public int Offset { get; }

        public int Count { get; }
    }
}
