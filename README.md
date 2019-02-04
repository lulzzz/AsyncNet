# AsyncNet
Asynchronous network library for .NET
## Purpose
The primary purpose of this library is to provide easy to use interface for TCP and UDP networking in C#
## Getting started
This repository contains multiple projects that fall into different category. See below.
## AsyncNet.Tcp
### Installation
[NuGet](https://www.nuget.org/packages/AsyncNet.Tcp/)
### Features:
* Easy to use TCP server
* Easy to use TCP client
* SSL support
* Custom protocol deframing / defragmentation support
### Basic Usage
#### TCP Server
```csharp
var server = new AsyncNetTcpServer(7788);
server.ServerStarted += (s, e) => Console.WriteLine($"Server started on port: " +
    $"{e.TcpServerStartedData.ServerPort}");
server.ConnectionEstablished += (s, e) =>
{
    var peer = e.ConnectionEstablishedData.RemoteTcpPeer;
    Console.WriteLine($"New connection from [{peer.IPEndPoint}]");

    var hello = "Hello from server!";
    var bytes = System.Text.Encoding.UTF8.GetBytes(hello);
    peer.Post(bytes);
};
server.FrameArrived += (s, e) => Console.WriteLine($"Server received: " +
    $"{System.Text.Encoding.UTF8.GetString(e.TcpFrameArrivedData.FrameData)}");
await server.StartAsync(CancellationToken.None);
```
#### TCP Client
```csharp
var client = new AsyncNetTcpClient("127.0.0.1", 7788);
client.ConnectionEstablished += (s, e) =>
{
    var peer = e.ConnectionEstablishedData.RemoteTcpPeer;
    Console.WriteLine($"New connection from [{peer.IPEndPoint}]");

    var hello = "Hello from client!";
    var bytes = System.Text.Encoding.UTF8.GetBytes(hello);
    peer.Post(bytes);
};
client.FrameArrived += (s, e) => Console.WriteLine($"Client received: " +
    $"{System.Text.Encoding.UTF8.GetString(e.TcpFrameArrivedData.FrameData)}");
await client.StartAsync(CancellationToken.None);
```
## AsyncNet.Udp
### Installation
[NuGet](https://www.nuget.org/packages/AsyncNet.Udp/)
### Features:
* Easy to use UDP server
* Easy to use UDP client
### Basic Usage
#### UDP Server
```csharp
var server = new AsyncNetUdpServer(7788);
server.ServerStarted += (s, e) => Console.WriteLine($"Server started on port: {e.UdpServerStartedData.ServerPort}");
server.UdpPacketArrived += (s, e) =>
{
    Console.WriteLine($"Server received: " +
        $"{System.Text.Encoding.UTF8.GetString(e.UdpPacketArrivedData.PacketData)} " +
        "from " +
        $"[{e.UdpPacketArrivedData.RemoteEndPoint}]");

    var response = "Response!";
    var bytes = System.Text.Encoding.UTF8.GetBytes(response);
    server.Post(bytes, e.UdpPacketArrivedData.RemoteEndPoint);
};
await server.StartAsync(CancellationToken.None);
```
#### UDP Client
```csharp
var client = new AsyncNetUdpClient("127.0.0.1", 7788);
client.ClientReady += (s, e) =>
{
    var hello = "Hello!";
    var bytes = System.Text.Encoding.UTF8.GetBytes(hello);

    e.UdpClientReadyData.Client.Post(bytes);
};
client.UdpPacketArrived += (s, e) =>
{
    Console.WriteLine($"Client received: " +
        $"{System.Text.Encoding.UTF8.GetString(e.UdpPacketArrivedData.PacketData)} " +
        "from " +
        $"[{e.UdpPacketArrivedData.RemoteEndPoint}]");
};
await client.StartAsync(CancellationToken.None);
```
