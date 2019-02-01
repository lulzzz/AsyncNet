using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace AsyncNet.Udp.Extensions
{
    public static class UdpClientExtensions
    {
        public static async Task<UdpReceiveResult> ReceiveWithCancellationTokenAsync(this UdpClient udpClient, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<UdpReceiveResult>();

            using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), false))
            {
                var task = udpClient.ReceiveAsync();

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                var result = await completedTask.ConfigureAwait(false);

                return result;
            }
        }

        public static async Task<int> SendWithCancellationTokenAsync(this UdpClient udpClient, byte[] datagram, int bytes, IPEndPoint endPoint, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<int>();

            using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), false))
            {
                var task = udpClient.SendAsync(datagram, bytes, endPoint);

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                var result = await completedTask.ConfigureAwait(false);

                return result;
            }
        }

        public static async Task<int> SendWithCancellationTokenAsync(this UdpClient udpClient, byte[] datagram, int bytes, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<int>();

            using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), false))
            {
                var task = udpClient.SendAsync(datagram, bytes);

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                var result = await completedTask.ConfigureAwait(false);

                return result;
            }
        }
    }
}
