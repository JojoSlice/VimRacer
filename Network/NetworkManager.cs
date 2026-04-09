using System;
using LiteNetLib;

namespace VimRacer;

public sealed class NetworkManager : INetworkTransport
{
    public bool IsConnected => false;

    public event Action<ReadOnlyMemory<byte>>? OnReceive;

    public void Send(ReadOnlySpan<byte> data) { }
    public void Poll() { }
    public void Disconnect() { }
}
