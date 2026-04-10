using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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

        if (InputSystem.WasPressed(Keys.Enter))
            _scenes.Transition(new MainMenuScene(_scenes, _game));
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

        // Pulsing prompt
        const string prompt = "PRESS ENTER";
        float alpha = (MathF.Sin(_blink * 3f) + 1f) / 2f;
        var promptColor = new Color(1f, 1f, 1f, alpha);
        Vector2 promptSz = _font.MeasureString(prompt);
        sb.DrawString(_font, prompt,
            new Vector2((vp.Width - promptSz.X) / 2f, boxY + boxH + 24f),
            promptColor);

        sb.End();
    }
}
