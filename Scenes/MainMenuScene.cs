using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VimRacer;

public sealed class MainMenuScene : IScene
{
    private readonly SceneManager _scenes;
    private readonly Game _game;

    private SpriteFont _font = null!;
    private float _time;
    private float _charWidth;
    // Precomputed: (glyph string, line index, position in pre-transform space)
    private readonly List<(string Glyph, int Line, Vector2 Pos)> _glyphs = [];

    private static readonly string[] Logo =
    [
        """ .----------------.  .----------------.  .----------------.  .----------------.  .----------------.  .----------------.  .----------------.  .----------------. """,
        """| .--------------. || .--------------. || .--------------. || .--------------. || .--------------. || .--------------. || .--------------. || .--------------. |""",
        """| | ____   ____  | || |     _____    | || | ____    ____ | || |  _______     | || |      __      | || |     ______   | || |  _________   | || |  _______     | |""",
        """| ||_  _| |_  _| | || |    |_   _|   | || ||_   \  /   _|| || | |_   __ \    | || |     /  \     | || |   .' ___  |  | || | |_   ___  |  | || | |_   __ \    | |""",
        """| |  \ \   / /   | || |      | |     | || |  |   \/   |  | || |   | |__) |   | || |    / /\ \    | || |  / .'   \_|  | || |   | |_  \_|  | || |   | |__) |   | |""",
        """| |   \ \ / /    | || |      | |     | || |  | |\  /| |  | || |   |  __ /    | || |   / ____ \   | || |  | |         | || |   |  _|  _   | || |   |  __ /    | |""",
        """| |    \ ' /     | || |     _| |_    | || | _| |_\/_| |_ | || |  _| |  \ \_  | || | _/ /    \ \_ | || |  \ `.___.'\  | || |  _| |___/ |  | || |  _| |  \ \_  | |""",
        """| |     \_/      | || |    |_____|   | || ||_____||_____|| || | |____| |___| | || ||____|  |____|| || |   `._____.'  | || | |_________|  | || | |____| |___| | |""",
        """| |              | || |              | || |              | || |              | || |              | || |              | || |              | || |              | |""",
        """| '--------------' || '--------------' || '--------------' || '--------------' || '--------------' || '--------------' || '--------------' || '--------------' |""",
        """ '----------------'  '----------------'  '----------------'  '----------------'  '----------------'  '----------------'  '----------------'  '----------------' """,
    ];

    public MainMenuScene(SceneManager scenes, Game game)
    {
        _scenes = scenes;
        _game = game;
    }

    public void Initialize() { }

    public void LoadContent()
    {
        _font = _game.Content.Load<SpriteFont>("Fonts/Mono");
        _charWidth = _font.MeasureString("M").X;

        _glyphs.Clear();
        for (int row = 0; row < Logo.Length; row++)
        {
            string line = Logo[row];
            for (int col = 0; col < line.Length; col++)
            {
                if (line[col] == ' ') continue;
                _glyphs.Add((line[col].ToString(), row, new Vector2(col * _charWidth, row * _font.LineSpacing)));
            }
        }
    }

    public void UnloadContent() { }

    public void Update(GameTime gameTime)
    {
        _time += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        int maxLen = 0;
        foreach (var line in Logo)
            maxLen = Math.Max(maxLen, line.Length);
        float maxWidth = _charWidth * maxLen;

        var viewport = _game.GraphicsDevice.Viewport;
        float scale = viewport.Width / maxWidth;
        float totalH = _font.LineSpacing * scale * Logo.Length;
        float startY = (viewport.Height - totalH) / 2f;

        var transform = Matrix.CreateScale(scale, scale, 1f)
                      * Matrix.CreateTranslation(0f, startY, 0f);

        spriteBatch.Begin(transformMatrix: transform);

        foreach (var (glyph, line, pos) in _glyphs)
        {
            float hue = (_time * 80f + line * 18f) % 360f;
            spriteBatch.DrawString(_font, glyph, pos, HsvToColor(hue, 1f, 1f));
        }

        spriteBatch.End();

        // Command list below logo
        (string Cmd, string Desc)[] commands =
        [
            (":run",     "start race"),
            (":lobby",   "multiplayer"),
            (":friends", "friend list"),
            (":q",       "quit"),
        ];

        var cmdColor  = new Color(100, 210, 210);
        var descColor = new Color(100, 100, 110);

        float cmdW  = 0f;
        float descW = 0f;
        foreach (var (cmd, desc) in commands)
        {
            cmdW  = MathF.Max(cmdW,  _font.MeasureString(cmd).X);
            descW = MathF.Max(descW, _font.MeasureString(desc).X);
        }

        float lineH    = _font.LineSpacing;
        float blockY   = startY + totalH + 40f;
        float blockX   = (viewport.Width - cmdW - 16f - descW) / 2f;

        spriteBatch.Begin();
        for (int i = 0; i < commands.Length; i++)
        {
            var (cmd, desc) = commands[i];
            float y = blockY + i * lineH;
            spriteBatch.DrawString(_font, cmd,  new Vector2(blockX, y), cmdColor);
            spriteBatch.DrawString(_font, desc, new Vector2(blockX + cmdW + 16f, y), descColor);
        }
        spriteBatch.End();
    }

    private static Color HsvToColor(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1f - MathF.Abs(h / 60f % 2f - 1f));
        float m = v - c;
        float r,
            g,
            b;
        if (h < 60f)
        {
            r = c;
            g = x;
            b = 0;
        }
        else if (h < 120f)
        {
            r = x;
            g = c;
            b = 0;
        }
        else if (h < 180f)
        {
            r = 0;
            g = c;
            b = x;
        }
        else if (h < 240f)
        {
            r = 0;
            g = x;
            b = c;
        }
        else if (h < 300f)
        {
            r = x;
            g = 0;
            b = c;
        }
        else
        {
            r = c;
            g = 0;
            b = x;
        }
        return new Color(r + m, g + m, b + m);
    }
}
