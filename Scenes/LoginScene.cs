using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VimRacer;

public sealed class LoginScene : IScene
{
    private readonly SceneManager   _scenes;
    private readonly Game           _game;
    private readonly NetworkManager _network;

    private SpriteFont _font  = null!;
    private Texture2D  _pixel = null!;
    private float      _time;

    private enum State { Connecting, Idle, Waiting }
    private State  _state = State.Connecting;
    private string _statusMsg  = "";
    private bool   _statusIsError;
    private float  _connectTimer;

    private static readonly Color CmdColor   = SceneUi.CmdColor;
    private static readonly Color DescColor  = SceneUi.DescColor;
    private static readonly Color PanelBg    = SceneUi.PanelBg;
    private static readonly Color PageBg     = SceneUi.PageBg;
    private static readonly Color ErrorColor = new(220, 80, 80);

    public LoginScene(SceneManager scenes, Game game, NetworkManager network)
    {
        _scenes  = scenes;
        _game    = game;
        _network = network;
    }

    public void Initialize()
    {
        _network.OnLoginOk   += HandleLoginOk;
        _network.OnLoginFail += HandleLoginFail;
        _network.OnError     += HandleError;

        _connectTimer  = 0f;
        _statusMsg     = "";
        _statusIsError = false;
        _state         = State.Connecting;
        _network.Connect();
    }

    public void LoadContent()
    {
        _font  = _game.Content.Load<SpriteFont>("Fonts/Mono");
        _pixel = new Texture2D(_game.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void UnloadContent()
    {
        _network.OnLoginOk   -= HandleLoginOk;
        _network.OnLoginFail -= HandleLoginFail;
        _network.OnError     -= HandleError;
        _pixel.Dispose();
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _time += dt;

        if (_state == State.Connecting)
        {
            _network.Poll();
            _connectTimer += dt;
            if (_connectTimer >= 10f && _statusMsg.Length == 0)
            {
                _statusMsg     = "Connection failed. Is the server running?";
                _statusIsError = true;
                _state         = State.Idle;
            }
        }
    }

    // Called by Game1.ExecuteCommand when this scene is active
    public void HandleCommand(string cmd)
    {
        if (_state == State.Waiting) return;

        string[] parts = cmd.TrimStart(':').Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 3 && parts[0] == "login")
        {
            _network.Login(parts[1], parts[2]);
            SetWaiting("Logging in...");
        }
        else if (parts.Length >= 3 && parts[0] == "register")
        {
            _network.Register(parts[1], parts[2]);
            SetWaiting("Registering...");
        }
        else if (cmd == ":q")
        {
            // handled by Game1
        }
        else
        {
            _statusMsg     = "Usage:  :login user pass  or  :register user pass";
            _statusIsError = true;
        }
    }

    // ── Network handlers ─────────────────────────────────────────────────────

    private void HandleLoginOk(LoginResult result)
    {
        _scenes.Transition(new LobbyScene(_scenes, _game, _network));
    }

    private void HandleLoginFail(string reason)
    {
        _statusMsg     = reason;
        _statusIsError = true;
        _state         = State.Idle;
    }

    private void HandleError(string msg)
    {
        _statusMsg     = msg;
        _statusIsError = true;
        _state         = State.Idle;
    }

    // ── Draw ─────────────────────────────────────────────────────────────────

    public void Draw(SpriteBatch sb)
    {
        var   vp = _game.GraphicsDevice.Viewport;
        float lh = _font.LineSpacing;

        sb.Begin();
        sb.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), PageBg);

        if (_state == State.Connecting && _statusMsg.Length == 0)
        {
            DrawConnecting(sb, vp);
        }
        else
        {
            DrawPanel(sb, vp, lh);
        }

        sb.End();
    }

    private void DrawConnecting(SpriteBatch sb, Viewport vp)
    {
        int    dots = (int)(_time * 2f) % 4;
        string s    = "CONNECTING" + new string('.', dots);
        Vector2 sz  = _font.MeasureString(s);
        sb.DrawString(_font, s,
            new Vector2((vp.Width - sz.X) / 2f, (vp.Height - sz.Y) / 2f),
            CmdColor);
    }

    private void DrawPanel(SpriteBatch sb, Viewport vp, float lh)
    {
        (string Cmd, string Desc)[] cmds =
        [
            (":login user pass",    "sign in"),
            (":register user pass", "create account"),
            (":q",                  "quit"),
        ];

        string title   = "VIMRACER";
        float  titleW  = _font.MeasureString(title).X;
        float  statusW = _statusMsg.Length > 0 ? _font.MeasureString(_statusMsg).X : 0f;

        float cmdColW  = 0f;
        float descColW = 0f;
        foreach (var (c, d) in cmds)
        {
            cmdColW  = MathF.Max(cmdColW,  _font.MeasureString(c).X);
            descColW = MathF.Max(descColW, _font.MeasureString(d).X);
        }
        float cmdBlockW = cmdColW + 16f + descColW;

        float contentW = MathF.Max(MathF.Max(titleW, statusW), cmdBlockW);
        const int PadX = 24, PadY = 16, SepH = 12;

        int   statusRows = _statusMsg.Length > 0 ? 2 : 0; // blank line + status
        float innerH     = lh                         // title
                         + statusRows * lh
                         + SepH
                         + cmds.Length * lh;
        float boxW = contentW + PadX * 2f;
        float boxH = innerH   + PadY * 2f;
        float boxX = (vp.Width  - boxW) / 2f;
        float boxY = (vp.Height - boxH) / 2f;

        SceneUi.DrawPanel(sb, _pixel, boxX, boxY, boxW, boxH);

        float tx = boxX + PadX;
        float ty = boxY + PadY;

        sb.DrawString(_font, title, new Vector2(tx, ty), Color.Cyan);
        ty += lh;

        if (_statusMsg.Length > 0)
        {
            ty += lh * 0.3f;
            Color sc = _statusIsError ? ErrorColor : CmdColor;
            sb.DrawString(_font, _statusMsg, new Vector2(tx, ty), sc);
            ty += lh * 1.2f;
        }

        ty += SepH;
        SceneUi.DrawCommandList(sb, _font, cmds, tx, ty, cmdColW);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetWaiting(string msg)
    {
        _statusMsg     = msg;
        _statusIsError = false;
        _state         = State.Waiting;
    }
}
