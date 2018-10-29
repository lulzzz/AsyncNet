using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using AsyncNet.Tcp.Defragmentation;

namespace AsyncNet.Tcp.Server
{
    public class AsyncTcpServerConfig
    {
        public IProtocolFrameDefragmenter ProtocolFrameDefragmenter { get; set; } = new MixedDefragmenter(new DefaultProtocolFrameDefragmentationStrategy());

        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.Zero;

        public int MaxSendQueuePerPeerSize { get; set; } = 10000;

        public IPAddress IPAddress { get; set; } = IPAddress.Any;

        public int Port { get; set; }

        public X509Certificate X509Certificate { get; set; }
    }
}
