using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VimRacer;

public sealed class ResultScene : IScene
{
    private readonly SceneManager _scenes;
    private readonly Game         _game;

    private readonly float _time;
    private readonly int   _topSpeedLevel;
    private readonly int   _obstaclesCleared;

    private SpriteFont _font   = null!;
    private Texture2D  _pixel  = null!;
    private float      _blink;

    public ResultScene(SceneManager scenes, Game game,
                       float time, int topSpeedLevel, int obstaclesCleared)
    {
        _scenes           = scenes;
        _game             = game;
        _time             = time;
        _topSpeedLevel    = topSpeedLevel;
        _obstaclesCleared = obstaclesCleared;
    }

    public void Initialize() { }

    public void LoadContent()
    {
        _font  = _game.Content.Load<SpriteFont>("Fonts/Mono");
        _pixel = new Texture2D(_game.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void UnloadContent() => _pixel.Dispose();

    public void Update(GameTime gameTime)
    {
        _blink += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public void Draw(SpriteBatch sb)
    {
        var vp = _game.GraphicsDevice.Viewport;

        int   m       = (int)(_time / 60f);
        float s       = _time % 60f;
        string timeStr    = $"{m}:{s:00.0}";
        string speedStr   = $"LEVEL {_topSpeedLevel}/10";
        string clearStr   = _obstaclesCleared == 0 ? "none" : _obstaclesCleared.ToString();

        string[] rows =
        [
            "RACE COMPLETE",
            "",
            $"TIME          {timeStr}",
            $"TOP SPEED     {speedStr}",
            $"OBSTACLES     {clearStr} cleared",
        ];

        float lineH  = _font.LineSpacing;
        float blockH = rows.Length * lineH;

        // Measure widest row for the border
        float maxW = 0f;
        foreach (var r in rows)
            maxW = MathF.Max(maxW, _font.MeasureString(r).X);

        const int PadX = 24;
        const int PadY = 16;
        float boxW = maxW + PadX * 2f;
        float boxH = blockH + PadY * 2f;
        float boxX = (vp.Width  - boxW) / 2f;
        float boxY = (vp.Height - boxH) / 2f;

        sb.Begin();

        // Background
        sb.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), new Color(10, 10, 14));

        // Panel
        sb.Draw(_pixel, new Rectangle((int)boxX, (int)boxY, (int)boxW, (int)boxH),
            new Color(20, 20, 30));

        // Top accent line
        sb.Draw(_pixel, new Rectangle((int)boxX, (int)boxY, (int)boxW, 2), Color.Cyan);

        // Text rows
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i].Length == 0) continue;

            float tx = boxX + PadX;
            float ty = boxY + PadY + i * lineH;

            Color c = i == 0
                ? Color.Cyan
                : new Color(180, 180, 180);

            sb.DrawString(_font, rows[i], new Vector2(tx, ty), c);
        }

        // Command list
        (string Cmd, string Desc)[] commands =
        [
            (":run",  "new race"),
            (":menu", "main menu"),
            (":q",    "quit"),
        ];

        var cmdColor  = new Color(100, 210, 210);
        var descColor = new Color(100, 100, 110);

        float cmdW = 0f;
        foreach (var (cmd, _) in commands)
            cmdW = MathF.Max(cmdW, _font.MeasureString(cmd).X);

        float descW = 0f;
        foreach (var (_, desc) in commands)
            descW = MathF.Max(descW, _font.MeasureString(desc).X);

        float listX = (vp.Width - cmdW - 16f - descW) / 2f;
        float listY = boxY + boxH + 28f;

        for (int i = 0; i < commands.Length; i++)
        {
            var (cmd, desc) = commands[i];
            float y = listY + i * lineH;
            sb.DrawString(_font, cmd,  new Vector2(listX, y), cmdColor);
            sb.DrawString(_font, desc, new Vector2(listX + cmdW + 16f, y), descColor);
        }

        sb.End();
    }
}
