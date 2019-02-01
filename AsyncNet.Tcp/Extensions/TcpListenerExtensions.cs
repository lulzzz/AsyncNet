using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNet.Tcp.Extensions
{
    public static class TcpListenerExtensions
    {
        public static async Task<TcpClient> AcceptTcpClientWithCancellationTokenAsync(this TcpListener tcpListener, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<TcpClient>();

            using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), false))
            {
                var task = tcpListener.AcceptTcpClientAsync();

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                var tcpClient = await completedTask.ConfigureAwait(false);

                return tcpClient;
            }
        }
    }
}
