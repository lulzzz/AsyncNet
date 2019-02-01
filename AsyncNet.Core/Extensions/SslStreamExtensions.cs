using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNet.Core.Extensions
{
    public static class SslStreamExtensions
    {
        public static async Task AuthenticateAsServerWithCancellationAsync(this SslStream stream, X509Certificate serverCertificate, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<int>();

            using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), false))
            {
                var task = stream.AuthenticateAsServerAsync(serverCertificate);

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                await completedTask.ConfigureAwait(false);
            }
        }

        public static async Task AuthenticateAsServerWithCancellationAsync(
            this SslStream stream,
            X509Certificate serverCertificate,
            bool clientCertificateRequired,
            SslProtocols enabledSslProtocols,
            bool checkCertificateRevocation,
            CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<int>();

            using (cancellationToken.Register(() =>
            {
                taskCompletionSource.TrySetCanceled();
            },
            false))
            {
                var task = stream.AuthenticateAsServerAsync(
                    serverCertificate, 
                    clientCertificateRequired,
                    enabledSslProtocols,
                    checkCertificateRevocation);

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                await completedTask.ConfigureAwait(false);
            }
        }

        public static async Task AuthenticateAsClientWithCancellationAsync(this SslStream stream, string targetHost, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<int>();

            using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), false))
            {
                var task = stream.AuthenticateAsClientAsync(targetHost);

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                await completedTask.ConfigureAwait(false);
            }
        }

        public static async Task AuthenticateAsClientWithCancellationAsync(
            this SslStream stream,
            string targetHost,
            X509CertificateCollection clientCertificates,
            SslProtocols enabledSslProtocols,
            bool checkCertificateRevocation,
            CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<int>();

            using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), false))
            {
                var task = stream.AuthenticateAsClientAsync(targetHost, clientCertificates, enabledSslProtocols, checkCertificateRevocation);

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                await completedTask.ConfigureAwait(false);
            }
        }
    }
}
