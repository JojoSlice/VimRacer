using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VimRacer;

public sealed class LobbyScene : IScene
{
    private readonly SceneManager    _scenes;
    private readonly Game            _game;
    private readonly NetworkManager  _network;

    private SpriteFont _font  = null!;
    private Texture2D  _pixel = null!;
    private float      _time;

    private enum State { Connecting, Browsing, InLobby }
    private State _state = State.Connecting;

    // Browsing
    private LobbyEntry[] _lobbies      = [];
    private int          _selectedIndex;

    // InLobby
    private LobbyInfo? _lobby;

    // Connection state
    private float  _connectTimer;
    private string _statusMsg = "";

    private static readonly Color CmdColor  = new(100, 210, 210);
    private static readonly Color DescColor = new(100, 100, 110);
    private static readonly Color DimColor  = new(80, 80, 80);
    private static readonly Color PanelBg   = new(20, 20, 30);
    private static readonly Color PageBg    = new(10, 10, 14);

    public LobbyScene(SceneManager scenes, Game game, NetworkManager network)
    {
        _scenes  = scenes;
        _game    = game;
        _network = network;
    }

    public void Initialize()
    {
        // Subscribe BEFORE Connect() to avoid missing early events
        _network.OnLobbyList    += HandleLobbyList;
        _network.OnLobbyJoined  += HandleLobbyJoined;
        _network.OnLobbyUpdated += HandleLobbyUpdated;
        _network.OnLobbyLeft    += HandleLobbyLeft;
        _network.OnGameStart    += HandleGameStart;
        _network.OnError        += HandleError;

        _connectTimer = 0f;
        _statusMsg    = "";

        if (_network.IsConnected && _network.Session != null)
        {
            // Already authenticated — go straight to browsing
            _state = State.Browsing;
            _network.RequestLobbyList();
        }
        else
        {
            _state = State.Connecting;
            _network.Connect();
        }
    }

    public void LoadContent()
    {
        _font  = _game.Content.Load<SpriteFont>("Fonts/Mono");
        _pixel = new Texture2D(_game.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void UnloadContent()
    {
        _network.OnLobbyList    -= HandleLobbyList;
        _network.OnLobbyJoined  -= HandleLobbyJoined;
        _network.OnLobbyUpdated -= HandleLobbyUpdated;
        _network.OnLobbyLeft    -= HandleLobbyLeft;
        _network.OnGameStart    -= HandleGameStart;
        _network.OnError        -= HandleError;
        _pixel.Dispose();
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _time += dt;

        if (_state == State.Connecting)
        {
            _connectTimer += dt;
            if (_connectTimer >= 10f && _statusMsg.Length == 0)
                _statusMsg = "Connection failed. Is the server running?";
        }

        _network.Poll();
    }

    // Called by Game1.ExecuteCommand when this scene is active
    public void HandleCommand(string cmd)
    {
        switch (_state)
        {
            case State.Browsing:
                if (cmd == ":join")
                {
                    if (_lobbies.Length > 0)
                        _network.JoinLobby(_lobbies[_selectedIndex].Id);
                }
                else if (cmd.StartsWith(":create"))
                {
                    string name = cmd.Length > 8 ? cmd[8..].Trim() : "Player's game";
                    if (name.Length == 0) name = "Player's game";
                    _network.CreateLobby(name);
                }
                else if (cmd == ":j")
                {
                    _selectedIndex = Math.Min(_selectedIndex + 1, Math.Max(0, _lobbies.Length - 1));
                }
                else if (cmd == ":k")
                {
                    _selectedIndex = Math.Max(_selectedIndex - 1, 0);
                }
                else if (cmd == ":refresh")
                {
                    _network.RequestLobbyList();
                }
                break;

            case State.InLobby:
                if (cmd == ":ready")
                    _network.ToggleReady();
                else if (cmd == ":leave")
                    _network.LeaveLobby();
                break;
        }
    }

    // ── Network event handlers ───────────────────────────────────────────────

    private void HandleLobbyList(LobbyEntry[] entries)
    {
        _lobbies       = entries;
        _selectedIndex = Math.Clamp(_selectedIndex, 0, Math.Max(0, entries.Length - 1));
        _state         = State.Browsing;
    }

    private void HandleLobbyJoined(LobbyInfo info)
    {
        _lobby = info;
        _state = State.InLobby;
    }

    private void HandleLobbyUpdated(PlayerSlot[] players)
    {
        if (_lobby == null) return;
        _lobby = _lobby with { Players = players };
    }

    private void HandleLobbyLeft()
    {
        _lobby = null;
        _state = State.Browsing;
        _network.RequestLobbyList();
    }

    private void HandleGameStart(int seed)
    {
        _scenes.Transition(new GameScene(_scenes, _game, seed, _network));
    }

    private void HandleError(string msg)
    {
        _statusMsg = msg;
    }

    // ── Draw ─────────────────────────────────────────────────────────────────

    public void Draw(SpriteBatch sb)
    {
        var vp    = _game.GraphicsDevice.Viewport;
        float lh  = _font.LineSpacing;

        sb.Begin();
        sb.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), PageBg);

        switch (_state)
        {
            case State.Connecting: DrawConnecting(sb, vp, lh); break;
            case State.Browsing:   DrawBrowsing(sb, vp, lh);   break;
            case State.InLobby:    DrawInLobby(sb, vp, lh);    break;
        }

        sb.End();
    }

    private void DrawConnecting(SpriteBatch sb, Viewport vp, float lh)
    {
        string s;
        Color  color;
        if (_statusMsg.Length > 0)
        {
            s     = _statusMsg;
            color = new Color(220, 80, 80);
        }
        else
        {
            int dots = (int)(_time * 2f) % 4;
            s     = "CONNECTING" + new string('.', dots);
            color = CmdColor;
        }
        Vector2 sz = _font.MeasureString(s);
        sb.DrawString(_font, s,
            new Vector2((vp.Width - sz.X) / 2f, (vp.Height - sz.Y) / 2f),
            color);
    }

    private void DrawBrowsing(SpriteBatch sb, Viewport vp, float lh)
    {
        (string Cmd, string Desc)[] cmds =
        [
            (":create", "new lobby"),
            (":j / :k", "navigate"),
            (":join",   "join selected"),
            (":refresh","refresh"),
            (":menu",   "main menu"),
            (":q",      "quit"),
        ];

        // Measure panel width
        float titleW = _font.MeasureString("LOBBIES").X;
        float listW  = MeasureLobbyListWidth();
        float cmdColW  = 0f;
        float descColW = 0f;
        foreach (var (c, d) in cmds)
        {
            cmdColW  = MathF.Max(cmdColW,  _font.MeasureString(c).X);
            descColW = MathF.Max(descColW, _font.MeasureString(d).X);
        }
        float cmdBlockW = cmdColW + 16f + descColW;

        float contentW = MathF.Max(MathF.Max(titleW, listW), cmdBlockW);
        const int PadX = 24, PadY = 16, SepH = 8;

        int lobbyRows  = Math.Max(_lobbies.Length, 1); // at least "(empty)"
        float innerH   = lh            // "LOBBIES"
                       + lh            // blank
                       + lobbyRows * lh
                       + SepH          // separator
                       + cmds.Length * lh;
        float boxW = contentW + PadX * 2f;
        float boxH = innerH   + PadY * 2f;
        float boxX = (vp.Width  - boxW) / 2f;
        float boxY = (vp.Height - boxH) / 2f;

        DrawPanel(sb, boxX, boxY, boxW, boxH);

        float tx = boxX + PadX;
        float ty = boxY + PadY;

        // Title
        sb.DrawString(_font, "LOBBIES", new Vector2(tx, ty), Color.Cyan);
        ty += lh * 2f;

        // Lobby list
        if (_lobbies.Length == 0)
        {
            sb.DrawString(_font, "  (empty)", new Vector2(tx, ty), DimColor);
            ty += lh;
        }
        else
        {
            for (int i = 0; i < _lobbies.Length; i++)
            {
                var entry = _lobbies[i];
                bool sel  = i == _selectedIndex;
                string cursor = sel ? "> " : "  ";
                Color  nc     = sel ? Color.White : new Color(150, 150, 150);

                sb.DrawString(_font, cursor + entry.Name, new Vector2(tx, ty), nc);

                string slots = $"{entry.Slots}/6";
                float sw = _font.MeasureString(slots).X;
                sb.DrawString(_font, slots,
                    new Vector2(boxX + boxW - PadX - sw, ty),
                    DescColor);
                ty += lh;
            }
        }

        ty += SepH;

        // Command list
        DrawCommandList(sb, cmds, tx, ty, cmdColW);
    }

    private void DrawInLobby(SpriteBatch sb, Viewport vp, float lh)
    {
        if (_lobby == null) return;

        const int MaxPlayers = 6;

        (string Cmd, string Desc)[] cmds =
        [
            (":ready", "toggle ready"),
            (":leave", "leave lobby"),
        ];

        string title = $"LOBBY: {_lobby.Name}";

        float cmdColW = 0f;
        foreach (var (c, _) in cmds)
            cmdColW = MathF.Max(cmdColW, _font.MeasureString(c).X);

        float tagW     = _font.MeasureString("[1] ").X;
        float readyW   = _font.MeasureString("  READY").X;
        float contentW = MathF.Max(_font.MeasureString(title).X,
                         MathF.Max(tagW + _font.MeasureString("Player 6").X + readyW,
                                   cmdColW + 16f + _font.MeasureString("toggle ready").X));

        const int PadX = 24, PadY = 16, SepH = 8;
        float innerH = lh                       // title
                     + lh                       // blank
                     + MaxPlayers * lh          // all 6 slots
                     + SepH
                     + cmds.Length * lh;
        float boxW = contentW + PadX * 2f;
        float boxH = innerH   + PadY * 2f;
        float boxX = (vp.Width  - boxW) / 2f;
        float boxY = (vp.Height - boxH) / 2f;

        DrawPanel(sb, boxX, boxY, boxW, boxH);

        float tx = boxX + PadX;
        float ty = boxY + PadY;

        sb.DrawString(_font, title, new Vector2(tx, ty), Color.Cyan);
        ty += lh * 2f;

        float rightEdge = boxX + boxW - PadX;
        for (int i = 0; i < MaxPlayers; i++)
        {
            string tag = $"[{i + 1}]";
            bool   isMe = i == _lobby.MyIndex;
            sb.DrawString(_font, tag, new Vector2(tx, ty), isMe ? CmdColor : DescColor);

            float nameX = tx + tagW;
            if (i < _lobby.Players.Length)
            {
                var slot = _lobby.Players[i];
                sb.DrawString(_font, slot.Name, new Vector2(nameX, ty),
                    isMe ? Color.White : new Color(180, 180, 180));
                if (slot.Ready)
                {
                    float rw = _font.MeasureString("READY").X;
                    sb.DrawString(_font, "READY", new Vector2(rightEdge - rw, ty), CmdColor);
                }
            }
            else
            {
                sb.DrawString(_font, "...", new Vector2(nameX, ty), DimColor);
            }
            ty += lh;
        }

        ty += SepH;
        DrawCommandList(sb, cmds, tx, ty, cmdColW);
    }

    // ── Draw helpers ─────────────────────────────────────────────────────────

    private void DrawPanel(SpriteBatch sb, float x, float y, float w, float h)
    {
        sb.Draw(_pixel, new Rectangle((int)x, (int)y, (int)w, (int)h), PanelBg);
        sb.Draw(_pixel, new Rectangle((int)x, (int)y, (int)w, 2), Color.Cyan);
    }

    private void DrawCommandList(SpriteBatch sb,
                                 (string Cmd, string Desc)[] cmds,
                                 float tx, float ty, float cmdColW)
    {
        foreach (var (cmd, desc) in cmds)
        {
            sb.DrawString(_font, cmd,  new Vector2(tx, ty), CmdColor);
            sb.DrawString(_font, desc, new Vector2(tx + cmdColW + 16f, ty), DescColor);
            ty += _font.LineSpacing;
        }
    }

    private float MeasureLobbyListWidth()
    {
        float w = _font.MeasureString("(empty)").X;
        foreach (var e in _lobbies)
            w = MathF.Max(w, _font.MeasureString("  " + e.Name + "  2/2").X);
        return w;
    }
}
