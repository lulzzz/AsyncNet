using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNet.Tcp
{
    internal static class NetworkStreamExtensions
    {
        public static async Task<int> ReadWithRealCancellationAsync(this NetworkStream networkStream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<int>();

            using (cancellationToken.Register(() =>
            {
                taskCompletionSource.TrySetCanceled();
            }, 
            false))
            {
                var task = networkStream.ReadAsync(buffer, offset, count);

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                var readLength = await completedTask.ConfigureAwait(false);

                return readLength;
            }
        }

        public static async Task WriteWithRealCancellationAsync(this NetworkStream networkStream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<int>();

            using (cancellationToken.Register(() =>
            {
                taskCompletionSource.TrySetCanceled();
            }, 
            false))
            {
                var task = networkStream.WriteAsync(buffer, offset, count);

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                await completedTask.ConfigureAwait(false);
            }
        }
    }
}
