using System.IO;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;

namespace VimRacerServer;

internal sealed class RelayServer : INetEventListener
{
    private readonly NetManager    _net;
    private readonly LobbyRegistry _registry = new();
    private readonly UserDatabase  _db;

    private const string Key = "vimracer";

    public RelayServer(int port, UserDatabase db)
    {
        _db  = db;
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
        NotifyLobbyOnExit(player);
        _registry.Remove(peer);

        // Notify online friends
        if (player.UserId.HasValue)
        {
            var friendIds  = _db.GetFriendIds(player.UserId.Value);
            var offlineMsg = Packet.Build(MsgType.S_FriendOffline, w => Packet.WriteStr(w, player.Username));
            foreach (var fid in friendIds)
            {
                var fp = _registry.FindByUserId(fid);
                if (fp != null) Send(fp.Peer, offlineMsg);
            }
        }
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader,
                                 byte channel, DeliveryMethod delivery)
    {
        if (!_registry.TryGet(peer, out var player)) return;

        byte[] data  = reader.GetRemainingBytes();
        if (data.Length == 0) return;
        var    type  = (MsgType)data[0];
        using var ms = new MemoryStream(data, 1, data.Length - 1);
        using var br = new BinaryReader(ms);

        switch (type)
        {
            case MsgType.C_Register:     HandleRegister(player, br);         break;
            case MsgType.C_Login:        HandleLogin(player, br);            break;
            case MsgType.C_CreateLobby:  HandleCreateLobby(player, br);      break;
            case MsgType.C_ListLobbies:  HandleListLobbies(player);          break;
            case MsgType.C_JoinLobby:    HandleJoinLobby(player, br);        break;
            case MsgType.C_LeaveLobby:   HandleLeaveLobby(player);           break;
            case MsgType.C_ToggleReady:  HandleToggleReady(player);          break;
            case MsgType.C_InviteFriend:  HandleInviteFriend(player, br);    break;
            case MsgType.C_AcceptInvite:  HandleAcceptInvite(player, br);    break;
            case MsgType.C_DeclineInvite: HandleDeclineInvite(player, br);   break;
            case MsgType.C_AddFriend:     HandleAddFriend(player, br);       break;
            case MsgType.C_RemoveFriend:  HandleRemoveFriend(player, br);    break;
            case MsgType.C_ListFriends:   HandleListFriends(player);         break;
            case MsgType.C_GameData:      HandleGameData(player, data);      break;
            case MsgType.C_PlayerUpdate:  HandlePlayerUpdate(player, data);  break;
            // C_Hello (0x01) intentionally unhandled — kept in enum to avoid
            // misrouting stale packets from older clients.
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError error) =>
        Console.WriteLine($"[!] Network error from {endPoint}: {error}");

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint,
        NetPacketReader reader, UnconnectedMessageType messageType) { }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

    // ── Auth handlers ─────────────────────────────────────────────────────────

    private void HandleRegister(PlayerInfo player, BinaryReader r)
    {
        if (player.UserId != null) { SendError(player.Peer, "Already logged in."); return; }

        string username = Packet.ReadStr(r);
        string password = Packet.ReadStr(r);

        if (!_db.TryRegister(username, password, out int userId, out string error))
        {
            Send(player.Peer, Packet.Build(MsgType.S_LoginFail, w => Packet.WriteStr(w, error)));
            return;
        }

        player.UserId   = userId;
        player.Username = username.ToLowerInvariant();
        Console.WriteLine($"    Registered '{player.Username}' (id={userId})");
        Send(player.Peer, Packet.Build(MsgType.S_LoginOk, w =>
        {
            w.Write(userId);
            Packet.WriteStr(w, player.Username);
        }));
    }

    private void HandleLogin(PlayerInfo player, BinaryReader r)
    {
        if (player.UserId != null) { SendError(player.Peer, "Already logged in."); return; }

        string username = Packet.ReadStr(r);
        string password = Packet.ReadStr(r);

        if (!_db.TryLogin(username, password, out int userId, out string displayName))
        {
            Send(player.Peer, Packet.Build(MsgType.S_LoginFail, w => Packet.WriteStr(w, "Invalid username or password.")));
            return;
        }

        // Prevent duplicate sessions
        if (_registry.FindByUserId(userId) != null)
        {
            Send(player.Peer, Packet.Build(MsgType.S_LoginFail, w => Packet.WriteStr(w, "Already logged in from another session.")));
            return;
        }

        player.UserId   = userId;
        player.Username = displayName;
        Console.WriteLine($"    Login '{player.Username}' (id={userId})");
        Send(player.Peer, Packet.Build(MsgType.S_LoginOk, w =>
        {
            w.Write(userId);
            Packet.WriteStr(w, player.Username);
        }));

        // Notify online friends
        var friendIds = _db.GetFriendIds(userId);
        var onlineMsg = Packet.Build(MsgType.S_FriendOnline, w => Packet.WriteStr(w, player.Username));
        foreach (var fid in friendIds)
        {
            var fp = _registry.FindByUserId(fid);
            if (fp != null) Send(fp.Peer, onlineMsg);
        }
    }

    // ── Lobby handlers ────────────────────────────────────────────────────────

    private bool RequireLogin(PlayerInfo player)
    {
        if (player.UserId != null) return true;
        SendError(player.Peer, "Not logged in.");
        return false;
    }

    private void HandleCreateLobby(PlayerInfo player, BinaryReader r)
    {
        if (!RequireLogin(player)) return;
        if (player.LobbyId != null) { SendError(player.Peer, "Already in a lobby."); return; }

        string name      = Packet.ReadStr(r);
        bool   isPrivate = r.ReadBoolean();
        var    lobby     = _registry.CreateLobby(player, name, isPrivate);
        Console.WriteLine($"    '{player.Name}' created lobby '{name}' (id={lobby.Id}, private={isPrivate})");
        SendLobbyJoined(player, lobby);
    }

    private void HandleListLobbies(PlayerInfo player)
    {
        if (!RequireLogin(player)) return;

        var lobbies = _registry.PublicOpenLobbies().ToArray();
        var msg = Packet.Build(MsgType.S_LobbyList, w =>
        {
            w.Write((short)lobbies.Length);
            foreach (var l in lobbies)
            {
                w.Write(l.Id);
                Packet.WriteStr(w, l.Name);
                w.Write((byte)l.Slots);
                w.Write(l.IsPrivate); // always false from public list; included for wire-format consistency
            }
        });
        Send(player.Peer, msg);
    }

    private void HandleJoinLobby(PlayerInfo player, BinaryReader r)
    {
        if (!RequireLogin(player)) return;

        int lobbyId = r.ReadInt32();

        if (!_registry.TryJoinLobby(player, lobbyId, out var lobby))
        {
            SendError(player.Peer, "Lobby not found or full.");
            return;
        }

        if (lobby.IsPrivate && (player.UserId == null || !lobby.IsInvited(player.UserId.Value)))
        {
            _registry.Leave(player);
            SendError(player.Peer, "This lobby is private. You need an invitation.");
            return;
        }

        Console.WriteLine($"    '{player.Name}' joined lobby '{lobby.Name}' ({lobby.Slots}/{Lobby.MaxPlayers})");
        SendLobbyJoined(player, lobby);
        SendLobbyUpdated(lobby);
    }

    private void HandleLeaveLobby(PlayerInfo player)
    {
        if (!RequireLogin(player)) return;
        NotifyLobbyOnExit(player);
        Send(player.Peer, Packet.Simple(MsgType.S_LobbyLeft));
    }

    private void HandleInviteFriend(PlayerInfo player, BinaryReader r)
    {
        if (!RequireLogin(player)) return;
        if (player.LobbyId == null) { SendError(player.Peer, "You are not in a lobby."); return; }

        string targetUsername = Packet.ReadStr(r);
        var    target         = _registry.FindByUsername(targetUsername);

        if (target == null) { SendError(player.Peer, "Player is not online."); return; }
        if (target == player) { SendError(player.Peer, "Cannot invite yourself."); return; }

        var lobby = _registry.LobbyOf(player)!;

        if (target.LobbyId == lobby.Id) { SendError(player.Peer, "Player is already in this lobby."); return; }

        if (target.UserId.HasValue)
            lobby.InvitedUserIds.Add(target.UserId.Value);

        Send(target.Peer, Packet.Build(MsgType.S_LobbyInvite, w =>
        {
            Packet.WriteStr(w, player.Username);
            w.Write(lobby.Id);
            Packet.WriteStr(w, lobby.Name);
        }));
        Console.WriteLine($"    '{player.Username}' invited '{target.Username}' to lobby '{lobby.Name}'");
    }

    private void HandleAcceptInvite(PlayerInfo player, BinaryReader r)
    {
        if (!RequireLogin(player)) return;
        if (player.LobbyId != null) { SendError(player.Peer, "Already in a lobby."); return; }

        int lobbyId = r.ReadInt32();

        if (!_registry.TryGetLobby(lobbyId, out var lobby) || lobby.Started)
        {
            SendError(player.Peer, "Lobby no longer available.");
            return;
        }

        if (lobby.IsPrivate && (player.UserId == null || !lobby.IsInvited(player.UserId.Value)))
        {
            SendError(player.Peer, "You were not invited to this lobby.");
            return;
        }

        if (!_registry.TryJoinLobby(player, lobbyId, out lobby))
        {
            SendError(player.Peer, "Lobby is full.");
            return;
        }

        Console.WriteLine($"    '{player.Username}' accepted invite to lobby '{lobby.Name}'");
        SendLobbyJoined(player, lobby);
        SendLobbyUpdated(lobby);
    }

    private void HandleDeclineInvite(PlayerInfo player, BinaryReader r)
    {
        int lobbyId = r.ReadInt32();
        if (player.UserId.HasValue && _registry.TryGetLobby(lobbyId, out var lobby))
            lobby.InvitedUserIds.Remove(player.UserId.Value);
    }

    // Handles lobby state cleanup when a player exits (disconnect or leave).
    // Evicts all remaining members if the host left; otherwise sends an update.
    private void NotifyLobbyOnExit(PlayerInfo player)
    {
        var lobby    = _registry.LobbyOf(player);
        bool wasHost = lobby != null && player == lobby.Host;
        _registry.Leave(player);

        if (lobby == null) return;

        if (wasHost)
        {
            foreach (var o in lobby.Others(player))
                Send(o.Peer, Packet.Simple(MsgType.S_LobbyLeft));
        }
        else if (lobby.Players.Count > 0)
        {
            SendLobbyUpdated(lobby);
        }
    }

    private void HandleToggleReady(PlayerInfo player)
    {
        if (!RequireLogin(player)) return;

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

    private void HandlePlayerUpdate(PlayerInfo sender, byte[] raw)
    {
        var lobby = _registry.LobbyOf(sender);
        if (lobby?.Started != true) return;

        int senderIndex = lobby.Players.IndexOf(sender);
        var msg = Packet.Build(MsgType.S_PlayerUpdate, w =>
        {
            w.Write((byte)senderIndex);
            w.Write(raw, 1, raw.Length - 1);
        });
        foreach (var other in lobby.Others(sender))
            Send(other.Peer, msg);
    }

    // ── Friend handlers ───────────────────────────────────────────────────────

    private void HandleAddFriend(PlayerInfo player, BinaryReader r)
    {
        if (!RequireLogin(player)) return;

        string targetName = Packet.ReadStr(r);
        int? targetId = _db.GetUserIdByUsername(targetName);
        if (targetId == null) { SendError(player.Peer, "User not found."); return; }
        if (targetId == player.UserId) { SendError(player.Peer, "Cannot add yourself."); return; }

        var (ok, error) = _db.SendFriendRequest(player.UserId!.Value, targetId.Value);
        if (!ok) { SendError(player.Peer, error ?? "Could not send request."); return; }

        // If target is online, send a friend request notification
        var target = _registry.FindByUserId(targetId.Value);
        if (target != null)
            Send(target.Peer, Packet.Build(MsgType.S_FriendRequest, w => Packet.WriteStr(w, player.Username)));

        SendFriendList(player);
    }

    private void HandleRemoveFriend(PlayerInfo player, BinaryReader r)
    {
        if (!RequireLogin(player)) return;

        string targetName = Packet.ReadStr(r);
        int? targetId = _db.GetUserIdByUsername(targetName);
        if (targetId == null) { SendError(player.Peer, "User not found."); return; }

        _db.RemoveFriendship(player.UserId!.Value, targetId.Value);
        SendFriendList(player);
    }

    private void HandleListFriends(PlayerInfo player)
    {
        if (!RequireLogin(player)) return;
        SendFriendList(player);
    }

    private void SendFriendList(PlayerInfo player)
    {
        var friends = _db.GetFriends(player.UserId!.Value);
        var msg = Packet.Build(MsgType.S_FriendList, w =>
        {
            w.Write((short)friends.Count);
            foreach (var (id, name, isPending) in friends)
            {
                w.Write(id);
                Packet.WriteStr(w, name);
                w.Write(_registry.FindByUserId(id) != null); // online
                w.Write(isPending);
            }
        });
        Send(player.Peer, msg);
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
            w.Write(lobby.IsPrivate);
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
