using System;
using System.Net;
using System.Net.Sockets;

namespace AsyncNet.Udp.Server
{
    public class AsyncNetUdpServerConfig
    {
        public IPAddress IPAddress { get; set; } = IPAddress.Any;

        public int Port { get; set; }

        public int MaxSendQueueSize { get; set; } = 100000;

        public Action<UdpClient> ConfigureUdpListenerCallback { get; set; }

        public bool JoinMulticastGroup { get; set; }

        public Action<UdpClient> JoinMulticastGroupCallback { get; set; }

        public Action<UdpClient> LeaveMulticastGroupCallback { get; set; }
    }
}
