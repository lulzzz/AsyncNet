using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using AsyncNet.Tcp.Defragmentation;
using AsyncNet.Tcp.Remote;

namespace AsyncNet.Tcp.Server
{
    public class AsyncNetTcpServerConfig
    {
        public Func<IRemoteTcpPeer, IProtocolFrameDefragmenter> ProtocolFrameDefragmenterFactory { get; set; } = (_) => MixedDefragmenter.Default;

        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.Zero;

        public int MaxSendQueuePerPeerSize { get; set; } = 10000;

        public IPAddress IPAddress { get; set; } = IPAddress.Any;

        public int Port { get; set; }

        public Action<TcpListener> ConfigureTcpListenerCallback { get; set; }

        public bool UseSsl { get; set; }

        public X509Certificate X509Certificate { get; set; }

        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; } = (_, __, ___, ____) => true;

        public EncryptionPolicy EncryptionPolicy { get; set; } = EncryptionPolicy.RequireEncryption;

        public Func<TcpClient, bool> ClientCertificateRequiredCallback { get; set; } = (_) => false;

        public Func<TcpClient, bool> CheckCertificateRevocationCallback { get; set; } = (_) => false;

        public SslProtocols EnabledProtocols { get; set; } = SslProtocols.Default;
    }
}
