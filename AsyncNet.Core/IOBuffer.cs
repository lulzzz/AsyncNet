namespace AsyncNet.Core
{
    public struct IOBuffer
    {
        public IOBuffer(byte[] buffer, int offset, int count)
        {
            this.Buffer = buffer;
            this.Offset = offset;
            this.Count = count;
        }

        public readonly byte[] Buffer;

        public readonly int Offset;

        public readonly int Count;
    }
}
