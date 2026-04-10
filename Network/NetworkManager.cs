using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;

namespace VimRacer;

// DTOs
public record LobbyEntry(int Id, string Name, int Slots, bool IsPrivate);
public record PlayerSlot(string Name, bool Ready);
public record LobbyInfo(int Id, string Name, PlayerSlot[] Players, int MyIndex, bool IsPrivate);
public record LoginResult(int UserId, string Username);

public sealed class NetworkManager : INetworkTransport, INetEventListener
{
    // Change RelayHost to the server's public IP before shipping.
    private const string RelayHost  = "127.0.0.1";
    private const int    RelayPort  = 7777;
    private const string ConnectKey = "vimracer";

    private readonly NetManager _net;
    private NetPeer? _server;

    public NetworkManager() => _net = new NetManager(this) { AutoRecycle = true };

    public LoginResult? Session { get; private set; }

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
    public event Action<int>?               OnGameStart;    // int = seed
    public event Action<string>?            OnError;
    public event Action<int, float, float>? OnPlayerUpdate; // (playerIndex, x, y)
    public event Action<LoginResult>?       OnLoginOk;
    public event Action<string>?            OnLoginFail;

    // ── Lobby methods ────────────────────────────────────────────────────────

    public void Connect()
    {
        if (_net.IsRunning) return;
        _net.Start();
        _net.Connect(RelayHost, RelayPort, ConnectKey);
    }

    public void Register(string username, string password) =>
        SendRaw(Packet.Build(MsgType.C_Register, w => { Packet.WriteStr(w, username); Packet.WriteStr(w, password); }));

    public void Login(string username, string password) =>
        SendRaw(Packet.Build(MsgType.C_Login, w => { Packet.WriteStr(w, username); Packet.WriteStr(w, password); }));

    public void RequestLobbyList() =>
        SendRaw(Packet.Simple(MsgType.C_ListLobbies));

    public void CreateLobby(string name, bool isPrivate = false) =>
        SendRaw(Packet.Build(MsgType.C_CreateLobby, w => { Packet.WriteStr(w, name); w.Write(isPrivate); }));

    public void JoinLobby(int lobbyId) =>
        SendRaw(Packet.Build(MsgType.C_JoinLobby, w => w.Write(lobbyId)));

    public void LeaveLobby() =>
        SendRaw(Packet.Simple(MsgType.C_LeaveLobby));

    public void ToggleReady() =>
        SendRaw(Packet.Simple(MsgType.C_ToggleReady));

    public void SendPosition(float x, float y) =>
        SendRaw(Packet.Build(MsgType.C_PlayerUpdate, w => { w.Write(x); w.Write(y); }));

    // ── INetEventListener ────────────────────────────────────────────────────

    public void OnConnectionRequest(ConnectionRequest request) { }

    public void OnPeerConnected(NetPeer peer)
    {
        _server = peer;
        // No C_Hello — client must send C_Login or C_Register explicitly.
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
    {
        _server  = null;
        Session  = null;
        OnLobbyLeft?.Invoke();
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader,
                                 byte channel, DeliveryMethod delivery)
    {
        byte[] data = reader.GetRemainingBytes();
        if (data.Length == 0) return;
        var type = (MsgType)data[0];

        using var ms = new MemoryStream(data, 1, data.Length - 1);
        using var br = new BinaryReader(ms);

        switch (type)
        {
            case MsgType.S_LobbyList:    HandleLobbyList(br);    break;
            case MsgType.S_LobbyJoined:  HandleLobbyJoined(br);  break;
            case MsgType.S_LobbyUpdated: HandleLobbyUpdated(br); break;
            case MsgType.S_LobbyLeft:    OnLobbyLeft?.Invoke();  break;
            case MsgType.S_GameStart:    HandleGameStart(br);      break;
            case MsgType.S_GameData:     OnReceive?.Invoke(data);  break;
            case MsgType.S_PlayerUpdate: HandlePlayerUpdate(br);                    break;
            case MsgType.S_Error:        OnError?.Invoke(Packet.ReadStr(br));       break;
            case MsgType.S_LoginOk:      HandleLoginOk(br);                         break;
            case MsgType.S_LoginFail:    OnLoginFail?.Invoke(Packet.ReadStr(br));   break;
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
            int  slots     = r.ReadByte();
            bool isPrivate = r.ReadBoolean();
            entries[i] = new LobbyEntry(id, name, slots, isPrivate);
        }
        OnLobbyList?.Invoke(entries);
    }

    private void HandleLobbyJoined(BinaryReader r)
    {
        int    id        = r.ReadInt32();
        string lobbyName = Packet.ReadStr(r);
        var    players   = ReadPlayerSlots(r);
        int  myIndex   = r.ReadByte();
        bool isPrivate = r.ReadBoolean();
        OnLobbyJoined?.Invoke(new LobbyInfo(id, lobbyName, players, myIndex, isPrivate));
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

    private void HandleLoginOk(BinaryReader r)
    {
        int    userId   = r.ReadInt32();
        string username = Packet.ReadStr(r);
        Session = new LoginResult(userId, username);
        OnLoginOk?.Invoke(Session);
    }

    private void HandlePlayerUpdate(BinaryReader r)
    {
        int   idx = r.ReadByte();
        float x   = r.ReadSingle();
        float y   = r.ReadSingle();
        OnPlayerUpdate?.Invoke(idx, x, y);
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
