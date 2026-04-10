using System.IO;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;

namespace VimRacerServer;

internal sealed class RelayServer : INetEventListener
{
    private readonly NetManager    _net;
    private readonly LobbyRegistry _registry = new();

    private const string Key = "vimracer";

    public RelayServer(int port)
    {
        _net = new NetManager(this) { AutoRecycle = true };
        _net.Start(port);
    }

    public void Poll() => _net.PollEvents();

    // ── INetEventListener ────────────────────────────────────────────────────

    public void OnConnectionRequest(ConnectionRequest request) =>
        request.AcceptIfKey(Key);

    public void OnPeerConnected(NetPeer peer)
    {
        _registry.GetOrCreate(peer);
        Console.WriteLine($"[+] {peer.EndPoint} connected  (peers: {_net.ConnectedPeersCount})");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
    {
        Console.WriteLine($"[-] {peer.EndPoint} disconnected");
        if (!_registry.TryGet(peer, out var player)) return;

        var lobby   = _registry.LobbyOf(player);
        var others  = lobby?.Others(player).ToArray();
        bool wasHost = lobby != null && player == lobby.Host;

        _registry.Leave(player);

        if (others != null)
        {
            if (wasHost)
            {
                foreach (var o in others)
                    Send(o.Peer, Packet.Simple(MsgType.S_LobbyLeft));
            }
            else if (lobby != null && lobby.Players.Count > 0)
            {
                SendLobbyUpdated(lobby);
            }
        }

        _registry.Remove(peer);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader,
                                 byte channel, DeliveryMethod delivery)
    {
        if (!_registry.TryGet(peer, out var player)) return;

        byte[] data  = reader.GetRemainingBytes();
        var    type  = (MsgType)data[0];
        using var ms = new MemoryStream(data, 1, data.Length - 1);
        using var br = new BinaryReader(ms);

        switch (type)
        {
            case MsgType.C_Hello:       HandleHello(player, br);       break;
            case MsgType.C_CreateLobby: HandleCreateLobby(player, br); break;
            case MsgType.C_ListLobbies: HandleListLobbies(player);     break;
            case MsgType.C_JoinLobby:   HandleJoinLobby(player, br);   break;
            case MsgType.C_LeaveLobby:  HandleLeaveLobby(player);      break;
            case MsgType.C_ToggleReady: HandleToggleReady(player);     break;
            case MsgType.C_GameData:    HandleGameData(player, data);  break;
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError error) =>
        Console.WriteLine($"[!] Network error from {endPoint}: {error}");

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint,
        NetPacketReader reader, UnconnectedMessageType messageType) { }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private void HandleHello(PlayerInfo player, BinaryReader r)
    {
        player.Name = Packet.ReadStr(r);
        Console.WriteLine($"    Hello from '{player.Name}'");
        HandleListLobbies(player);
    }

    private void HandleCreateLobby(PlayerInfo player, BinaryReader r)
    {
        if (player.LobbyId != null) { SendError(player.Peer, "Already in a lobby."); return; }

        string name  = Packet.ReadStr(r);
        var    lobby = _registry.CreateLobby(player, name);
        Console.WriteLine($"    '{player.Name}' created lobby '{name}' (id={lobby.Id})");
        SendLobbyJoined(player, lobby);
    }

    private void HandleListLobbies(PlayerInfo player)
    {
        var lobbies = _registry.OpenLobbies().ToArray();
        var msg = Packet.Build(MsgType.S_LobbyList, w =>
        {
            w.Write((short)lobbies.Length);
            foreach (var l in lobbies)
            {
                w.Write(l.Id);
                Packet.WriteStr(w, l.Name);
                w.Write((byte)l.Slots);
            }
        });
        Send(player.Peer, msg);
    }

    private void HandleJoinLobby(PlayerInfo player, BinaryReader r)
    {
        int lobbyId = r.ReadInt32();

        if (!_registry.TryJoinLobby(player, lobbyId, out var lobby))
        {
            SendError(player.Peer, "Lobby not found or full.");
            return;
        }

        Console.WriteLine($"    '{player.Name}' joined lobby '{lobby.Name}' ({lobby.Slots}/{Lobby.MaxPlayers})");
        SendLobbyJoined(player, lobby);
        SendLobbyUpdated(lobby);   // notify existing players
    }

    private void HandleLeaveLobby(PlayerInfo player)
    {
        var lobby   = _registry.LobbyOf(player);
        var others  = lobby?.Others(player).ToArray();
        bool wasHost = lobby != null && player == lobby.Host;

        _registry.Leave(player);
        Send(player.Peer, Packet.Simple(MsgType.S_LobbyLeft));

        if (others != null)
        {
            if (wasHost)
                foreach (var o in others) Send(o.Peer, Packet.Simple(MsgType.S_LobbyLeft));
            else if (lobby!.Players.Count > 0)
                SendLobbyUpdated(lobby);
        }
    }

    private void HandleToggleReady(PlayerInfo player)
    {
        var lobby = _registry.LobbyOf(player);
        if (lobby == null) return;

        player.Ready = !player.Ready;
        SendLobbyUpdated(lobby);

        if (lobby.Players.Count >= 2 && lobby.Players.All(p => p.Ready))
            SendGameStart(lobby);
    }

    private void HandleGameData(PlayerInfo sender, byte[] raw)
    {
        var lobby = _registry.LobbyOf(sender);
        if (lobby?.Started != true) return;

        var msg = Packet.Build(MsgType.S_GameData, w => w.Write(raw, 1, raw.Length - 1));
        foreach (var other in lobby.Others(sender))
            Send(other.Peer, msg);
    }

    // ── Senders ──────────────────────────────────────────────────────────────

    private void SendLobbyJoined(PlayerInfo recipient, Lobby lobby)
    {
        int myIndex = lobby.Players.IndexOf(recipient);
        var msg = Packet.Build(MsgType.S_LobbyJoined, w =>
        {
            w.Write(lobby.Id);
            Packet.WriteStr(w, lobby.Name);
            w.Write((byte)lobby.Players.Count);
            foreach (var p in lobby.Players)
            {
                Packet.WriteStr(w, p.Name);
                w.Write(p.Ready);
            }
            w.Write((byte)myIndex);
        });
        Send(recipient.Peer, msg);
    }

    private void SendLobbyUpdated(Lobby lobby)
    {
        var msg = Packet.Build(MsgType.S_LobbyUpdated, w =>
        {
            w.Write((byte)lobby.Players.Count);
            foreach (var p in lobby.Players)
            {
                Packet.WriteStr(w, p.Name);
                w.Write(p.Ready);
            }
        });
        foreach (var p in lobby.Players)
            Send(p.Peer, msg);
    }

    private void SendGameStart(Lobby lobby)
    {
        lobby.Started = true;
        int seed = Random.Shared.Next();
        var msg  = Packet.Build(MsgType.S_GameStart, w => w.Write(seed));
        foreach (var p in lobby.Players)
            Send(p.Peer, msg);
        Console.WriteLine($"    Game started in lobby '{lobby.Name}' (seed={seed}, players={lobby.Slots})");
    }

    private static void SendError(NetPeer peer, string message) =>
        Send(peer, Packet.Build(MsgType.S_Error, w => Packet.WriteStr(w, message)));

    private static void Send(NetPeer peer, byte[] data) =>
        peer.Send(data, DeliveryMethod.ReliableOrdered);
}
