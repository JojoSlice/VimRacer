using LiteNetLib;

namespace VimRacerServer;

internal sealed class PlayerInfo
{
    public readonly NetPeer Peer;
    public int?   UserId;
    public string Username = "Player";
    public string Name     => Username;   // keeps existing RelayServer references working
    public int?   LobbyId;
    public bool   Ready;

    public PlayerInfo(NetPeer peer) => Peer = peer;
}

internal sealed class Lobby
{
    public const int MaxPlayers = 6;

    public readonly int              Id;
    public readonly string           Name;
    public readonly List<PlayerInfo> Players = new();
    public bool Started;

    public int  Slots   => Players.Count;
    public bool IsFull  => Players.Count >= MaxPlayers;
    public PlayerInfo Host => Players[0];

    public Lobby(int id, string name, PlayerInfo host)
    {
        Id   = id;
        Name = name;
        Players.Add(host);
    }

    public IEnumerable<PlayerInfo> Others(PlayerInfo p) => Players.Where(x => x != p);
}

internal sealed class LobbyRegistry
{
    private readonly Dictionary<int, Lobby>          _lobbies = new();
    private readonly Dictionary<NetPeer, PlayerInfo> _players = new();
    private int _nextId = 1;

    public PlayerInfo GetOrCreate(NetPeer peer)
    {
        if (!_players.TryGetValue(peer, out var p))
        {
            p = new PlayerInfo(peer);
            _players[peer] = p;
        }
        return p;
    }

    public bool TryGet(NetPeer peer, out PlayerInfo player) =>
        _players.TryGetValue(peer, out player!);

    public void Remove(NetPeer peer) => _players.Remove(peer);

    public Lobby CreateLobby(PlayerInfo host, string name)
    {
        var lobby = new Lobby(_nextId++, name, host);
        _lobbies[lobby.Id] = lobby;
        host.LobbyId = lobby.Id;
        host.Ready   = false;
        return lobby;
    }

    public bool TryJoinLobby(PlayerInfo joiner, int lobbyId, out Lobby lobby)
    {
        if (!_lobbies.TryGetValue(lobbyId, out lobby!)) return false;
        if (lobby.IsFull || lobby.Started)              return false;

        lobby.Players.Add(joiner);
        joiner.LobbyId = lobbyId;
        joiner.Ready   = false;
        return true;
    }

    public Lobby? LobbyOf(PlayerInfo player) =>
        player.LobbyId.HasValue && _lobbies.TryGetValue(player.LobbyId.Value, out var l) ? l : null;

    public void Leave(PlayerInfo player)
    {
        var lobby = LobbyOf(player);
        if (lobby == null) return;

        bool wasHost = player == lobby.Host;
        lobby.Players.Remove(player);
        player.LobbyId = null;
        player.Ready   = false;

        if (wasHost || lobby.Players.Count == 0)
        {
            // Host left → close lobby, evict everyone
            foreach (var p in lobby.Players)
                p.LobbyId = null;
            lobby.Players.Clear();
            _lobbies.Remove(lobby.Id);
        }
    }

    public IEnumerable<Lobby> OpenLobbies() =>
        _lobbies.Values.Where(l => !l.Started);

    public PlayerInfo? FindByUsername(string username) =>
        _players.Values.FirstOrDefault(p =>
            string.Equals(p.Username, username, StringComparison.OrdinalIgnoreCase));

    public PlayerInfo? FindByUserId(int userId) =>
        _players.Values.FirstOrDefault(p => p.UserId == userId);
}
