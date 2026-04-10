using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;

namespace VimRacer;

// DTOs used by LobbyScene
public record LobbyEntry(int Id, string Name, int Slots);
public record PlayerSlot(string Name, bool Ready);
public record LobbyInfo(int Id, string Name, PlayerSlot[] Players, int MyIndex);

public sealed class NetworkManager : INetworkTransport, INetEventListener
{
    // Change RelayHost to the server's public IP before shipping.
    private const string RelayHost  = "127.0.0.1";
    private const int    RelayPort  = 7777;
    private const string ConnectKey = "vimracer";

    private readonly NetManager _net;
    private NetPeer? _server;
    private string   _pendingName = "Player";

    public NetworkManager() => _net = new NetManager(this) { AutoRecycle = true };

    // ── INetworkTransport ────────────────────────────────────────────────────

    public bool IsConnected => _server != null;

    public event Action<ReadOnlyMemory<byte>>? OnReceive;

    public void Send(ReadOnlySpan<byte> data)
    {
        if (_server == null) return;
        _server.Send(data.ToArray(), DeliveryMethod.ReliableOrdered);
    }

    public void Poll() => _net.PollEvents();

    public void Disconnect()
    {
        _net.Stop();
        _server = null;
    }

    // ── Lobby events ─────────────────────────────────────────────────────────

    public event Action<LobbyEntry[]>?    OnLobbyList;
    public event Action<LobbyInfo>?       OnLobbyJoined;
    public event Action<PlayerSlot[]>?    OnLobbyUpdated;
    public event Action?                  OnLobbyLeft;
    public event Action<int>?             OnGameStart;      // int = seed

    // ── Lobby methods ────────────────────────────────────────────────────────

    public void Connect(string playerName)
    {
        if (_net.IsRunning) return;
        _pendingName = playerName;
        _net.Start();
        _net.Connect(RelayHost, RelayPort, ConnectKey);
    }

    public void RequestLobbyList() =>
        SendRaw(Packet.Simple(MsgType.C_ListLobbies));

    public void CreateLobby(string name) =>
        SendRaw(Packet.Build(MsgType.C_CreateLobby, w => Packet.WriteStr(w, name)));

    public void JoinLobby(int lobbyId) =>
        SendRaw(Packet.Build(MsgType.C_JoinLobby, w => w.Write(lobbyId)));

    public void LeaveLobby() =>
        SendRaw(Packet.Simple(MsgType.C_LeaveLobby));

    public void ToggleReady() =>
        SendRaw(Packet.Simple(MsgType.C_ToggleReady));

    // ── INetEventListener ────────────────────────────────────────────────────

    public void OnConnectionRequest(ConnectionRequest request) { }

    public void OnPeerConnected(NetPeer peer)
    {
        _server = peer;
        SendRaw(Packet.Build(MsgType.C_Hello, w => Packet.WriteStr(w, _pendingName)));
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
    {
        _server = null;
        OnLobbyLeft?.Invoke();
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader,
                                 byte channel, DeliveryMethod delivery)
    {
        byte[] data = reader.GetRemainingBytes();
        var type = (MsgType)data[0];

        using var ms = new MemoryStream(data, 1, data.Length - 1);
        using var br = new BinaryReader(ms);

        switch (type)
        {
            case MsgType.S_LobbyList:    HandleLobbyList(br);    break;
            case MsgType.S_LobbyJoined:  HandleLobbyJoined(br);  break;
            case MsgType.S_LobbyUpdated: HandleLobbyUpdated(br); break;
            case MsgType.S_LobbyLeft:    OnLobbyLeft?.Invoke();  break;
            case MsgType.S_GameStart:    HandleGameStart(br);    break;
            case MsgType.S_GameData:     OnReceive?.Invoke(data); break;
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError error) { }
    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint,
        NetPacketReader reader, UnconnectedMessageType messageType) { }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

    // ── Receive handlers ─────────────────────────────────────────────────────

    private void HandleLobbyList(BinaryReader r)
    {
        int count   = r.ReadInt16();
        var entries = new LobbyEntry[count];
        for (int i = 0; i < count; i++)
        {
            int    id    = r.ReadInt32();
            string name  = Packet.ReadStr(r);
            int    slots = r.ReadByte();
            entries[i]   = new LobbyEntry(id, name, slots);
        }
        OnLobbyList?.Invoke(entries);
    }

    private void HandleLobbyJoined(BinaryReader r)
    {
        int    id        = r.ReadInt32();
        string lobbyName = Packet.ReadStr(r);
        var    players   = ReadPlayerSlots(r);
        int    myIndex   = r.ReadByte();
        OnLobbyJoined?.Invoke(new LobbyInfo(id, lobbyName, players, myIndex));
    }

    private void HandleLobbyUpdated(BinaryReader r)
    {
        var players = ReadPlayerSlots(r);
        OnLobbyUpdated?.Invoke(players);
    }

    private void HandleGameStart(BinaryReader r)
    {
        int seed = r.ReadInt32();
        OnGameStart?.Invoke(seed);
    }

    private static PlayerSlot[] ReadPlayerSlots(BinaryReader r)
    {
        int count   = r.ReadByte();
        var players = new PlayerSlot[count];
        for (int i = 0; i < count; i++)
        {
            string name  = Packet.ReadStr(r);
            bool   ready = r.ReadBoolean();
            players[i]   = new PlayerSlot(name, ready);
        }
        return players;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SendRaw(byte[] data)
    {
        if (_server == null) return;
        _server.Send(data, DeliveryMethod.ReliableOrdered);
    }
}
