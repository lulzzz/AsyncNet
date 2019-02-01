using System;

namespace AsyncNet.Core
{
    public class AsyncNetBuffer
    {
        public AsyncNetBuffer(byte[] memory, int offset, int count)
        {
            this.Memory = memory;
            this.Offset = offset;
            this.Count = count;
        }

        public byte[] Memory { get; }

        public int Offset { get; }

        public int Count { get; }

        public byte[] ToBytes()
        {
            if (this.Offset == 0 && this.Count == this.Memory.Length)
            {
                return this.Memory;
            }

            byte[] buffer = new byte[this.Count];

            Array.Copy(this.Memory, this.Offset, buffer, 0, this.Count);

            return buffer;
        }
    }
}
