using System;
using System.Net;
using System.Net.Sockets;

namespace AsyncNet.Udp.Client
{
    public class AsyncNetUdpClientConfig
    {
        public string TargetHostname { get; set; }

        public int TargetPort { get; set; }

        public int MaxSendQueueSize { get; set; } = 10000;

        public Action<UdpClient> ConfigureUdpClientCallback { get; set; }

        public Func<IPAddress[], IPAddress> SelectIpAddressCallback { get; set; }
    }
}
