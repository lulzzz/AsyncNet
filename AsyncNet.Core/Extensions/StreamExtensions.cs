using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNet.Core.Extensions
{
    public static class StreamExtensions
    {
        public static async Task<int> ReadWithRealCancellationAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<int>();

            using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), false))
            {
                var task = stream.ReadAsync(buffer, offset, count);

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                var readLength = await completedTask.ConfigureAwait(false);

                return readLength;
            }
        }

        public static async Task WriteWithRealCancellationAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<int>();

            using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), false))
            {
                var task = stream.WriteAsync(buffer, offset, count);

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                await completedTask.ConfigureAwait(false);
            }
        }

        public static async Task<bool> ReadUntilBufferIsFullAsync(
            this Stream stream,
            byte[] buffer,
            int offset,
            int count)
        {
            int readLength = 0;

            while (readLength < count)
            {
                readLength += await stream.ReadAsync(buffer, offset + readLength, count - readLength)
                    .ConfigureAwait(false);

                if (readLength < 1)
                {
                    return false;
                }
            }

            return true;
        }

        public static async Task<bool> ReadUntilBufferIsFullAsync(
            this Stream stream,
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            int readLength = 0;

            while (readLength < count)
            {
                readLength += await stream.ReadWithRealCancellationAsync(buffer, offset + readLength, count - readLength, cancellationToken)
                    .ConfigureAwait(false);

                if (readLength < 1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
