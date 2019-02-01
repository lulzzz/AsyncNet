using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNet.Tcp.Extensions
{
    public static class TcpClientExtensions
    {
        public static async Task ConnectWithCancellationTokenAsync(this TcpClient tcpClient, IPAddress[] addresses, int port, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();

            using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), false))
            {
                var task = tcpClient.ConnectAsync(addresses, port);

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                await completedTask.ConfigureAwait(false);
            }
        }

        public static async Task ConnectWithCancellationTokenAsync(this TcpClient tcpClient, string hostname, int port, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();

            using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), false))
            {
                var task = tcpClient.ConnectAsync(hostname, port);

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                await completedTask.ConfigureAwait(false);
            }
        }
    }
}
