using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VimRacer;

public sealed class FriendScene : IScene
{
    private readonly SceneManager   _scenes;
    private readonly Game           _game;
    private readonly NetworkManager _network;

    private SpriteFont _font  = null!;
    private Texture2D  _pixel = null!;

    private FriendEntry[] _friends      = [];
    private int           _selectedIndex;
    private string        _statusMsg    = "";
    private bool          _statusIsError;

    private static readonly Color OnlineColor  = new(80, 220, 80);
    private static readonly Color PendingColor = new(255, 200, 60);

    public FriendScene(SceneManager scenes, Game game, NetworkManager network)
    {
        _scenes  = scenes;
        _game    = game;
        _network = network;
    }

    public void Initialize()
    {
        _network.OnFriendList    += HandleFriendList;
        _network.OnFriendRequest += HandleFriendRequest;
        _network.OnFriendOnline  += HandleFriendOnline;
        _network.OnFriendOffline += HandleFriendOffline;
        _network.OnError         += HandleError;

        if (_network.IsConnected && _network.Session != null)
            _network.RequestFriendList();
        else
            SetStatus("Not logged in. Use :lobby to log in first.", error: true);
    }

    public void LoadContent()
    {
        _font  = _game.Content.Load<SpriteFont>("Fonts/Mono");
        _pixel = new Texture2D(_game.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void UnloadContent()
    {
        _network.OnFriendList    -= HandleFriendList;
        _network.OnFriendRequest -= HandleFriendRequest;
        _network.OnFriendOnline  -= HandleFriendOnline;
        _network.OnFriendOffline -= HandleFriendOffline;
        _network.OnError         -= HandleError;
        _pixel.Dispose();
    }

    public void Update(GameTime gameTime) => _network.Poll();

    public void HandleCommand(string cmd)
    {
        if (cmd.StartsWith(":add "))
        {
            string name = cmd[5..].Trim();
            if (name.Length == 0) { SetStatus("Usage: :add <username>", error: true); return; }
            _network.AddFriend(name);
            SetStatus($"Request sent to {name}.", error: false);
        }
        else if (cmd.StartsWith(":remove "))
        {
            string name = cmd[8..].Trim();
            if (name.Length == 0) { SetStatus("Usage: :remove <username>", error: true); return; }
            _network.RemoveFriend(name);
            SetStatus($"Removed {name}.", error: false);
        }
        else if (cmd == ":j")
        {
            _selectedIndex = Math.Min(_selectedIndex + 1, Math.Max(0, _friends.Length - 1));
        }
        else if (cmd == ":k")
        {
            _selectedIndex = Math.Max(_selectedIndex - 1, 0);
        }
        else if (cmd == ":refresh")
        {
            _network.RequestFriendList();
        }
    }

    // ── Network handlers ─────────────────────────────────────────────────────

    private void HandleFriendList(FriendEntry[] friends)
    {
        _friends       = friends;
        _selectedIndex = Math.Clamp(_selectedIndex, 0, Math.Max(0, friends.Length - 1));
        _statusMsg     = "";
    }

    private void HandleFriendRequest(string fromUsername)
    {
        SetStatus($"Friend request from {fromUsername}. Use :add {fromUsername} to accept.", error: false);
        _network.RequestFriendList();
    }

    private void HandleFriendOnline(string username)  => SetFriendOnline(username, online: true);
    private void HandleFriendOffline(string username) => SetFriendOnline(username, online: false);

    private void SetFriendOnline(string username, bool online)
    {
        for (int i = 0; i < _friends.Length; i++)
            if (string.Equals(_friends[i].Username, username, StringComparison.OrdinalIgnoreCase))
            {
                _friends[i] = _friends[i] with { Online = online };
                return;
            }
    }

    private void HandleError(string msg) => SetStatus(msg, error: true);

    private void SetStatus(string msg, bool error)
    {
        _statusMsg    = msg;
        _statusIsError = error;
    }

    // ── Draw ─────────────────────────────────────────────────────────────────

    public void Draw(SpriteBatch sb)
    {
        var   vp  = _game.GraphicsDevice.Viewport;
        float lh  = _font.LineSpacing;

        sb.Begin();
        sb.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), SceneUi.PageBg);
        DrawPanel(sb, vp, lh);
        sb.End();
    }

    private void DrawPanel(SpriteBatch sb, Viewport vp, float lh)
    {
        (string Cmd, string Desc)[] cmds =
        [
            (":add username",    "add / accept friend"),
            (":remove username", "remove friend"),
            (":j / :k",          "navigate"),
            (":refresh",         "refresh"),
            (":menu",            "main menu"),
            (":q",               "quit"),
        ];

        float titleW   = _font.MeasureString("FRIENDS").X;
        float tagW     = _font.MeasureString("[Online] ").X;
        float nameMaxW = 0f;
        foreach (var f in _friends)
            nameMaxW = MathF.Max(nameMaxW, _font.MeasureString(f.Username).X);
        float listW = tagW + MathF.Max(nameMaxW, _font.MeasureString("(no friends yet)").X);

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

        int   rows   = Math.Max(_friends.Length, 1);
        float statusH = _statusMsg.Length > 0 ? lh + SepH : 0f;
        float innerH = lh            // "FRIENDS"
                     + lh            // blank
                     + rows * lh
                     + statusH
                     + SepH
                     + cmds.Length * lh;
        float boxW = contentW + PadX * 2f;
        float boxH = innerH   + PadY * 2f;
        float boxX = (vp.Width  - boxW) / 2f;
        float boxY = (vp.Height - boxH) / 2f;

        SceneUi.DrawPanel(sb, _pixel, boxX, boxY, boxW, boxH);

        float tx = boxX + PadX;
        float ty = boxY + PadY;

        sb.DrawString(_font, "FRIENDS", new Vector2(tx, ty), Color.Cyan);
        ty += lh * 2f;

        if (_friends.Length == 0)
        {
            sb.DrawString(_font, "  (no friends yet)", new Vector2(tx, ty), SceneUi.DimColor);
            ty += lh;
        }
        else
        {
            for (int i = 0; i < _friends.Length; i++)
            {
                var f   = _friends[i];
                bool sel = i == _selectedIndex;

                string cursor = sel ? "> " : "  ";
                Color  nc     = sel ? Color.White : new Color(150, 150, 150);

                string tag;
                Color  tagColor;
                if (f.IsPending)
                {
                    tag      = "[REQ  ] ";
                    tagColor = PendingColor;
                }
                else if (f.Online)
                {
                    tag      = "[Online] ";
                    tagColor = OnlineColor;
                }
                else
                {
                    tag      = "[-----] ";
                    tagColor = SceneUi.DimColor;
                }

                sb.DrawString(_font, cursor, new Vector2(tx, ty), nc);
                float nameX = tx + _font.MeasureString(cursor).X;
                sb.DrawString(_font, tag,       new Vector2(nameX, ty),          tagColor);
                sb.DrawString(_font, f.Username, new Vector2(nameX + tagW, ty),   nc);
                ty += lh;
            }
        }

        if (_statusMsg.Length > 0)
        {
            ty += SepH;
            Color sc = _statusIsError ? new Color(220, 80, 80) : new Color(80, 200, 80);
            sb.DrawString(_font, _statusMsg, new Vector2(tx, ty), sc);
            ty += lh;
        }

        ty += SepH;
        SceneUi.DrawCommandList(sb, _font, cmds, tx, ty, cmdColW);
    }
}
