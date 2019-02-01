using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNet.Core.Extensions
{
    public static class DnsExtensions
    {
        public static async Task<IPAddress[]> GetHostAddressesWithCancellationTokenAsync(string hostNameOrAddress, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<IPAddress[]>();

            using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), false))
            {
                var task = Dns.GetHostAddressesAsync(hostNameOrAddress);

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                var addresses = await completedTask.ConfigureAwait(false);

                return addresses;
            }
        }
    }
}
