using System;

namespace VimRacer;

public interface INetworkTransport
{
    bool IsConnected { get; }
    void Send(ReadOnlySpan<byte> data);
    event Action<ReadOnlyMemory<byte>> OnReceive;
    void Poll();
    void Disconnect();
}
