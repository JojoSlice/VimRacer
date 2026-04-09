using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace VimRacer;

public sealed class MainMenuScene : IScene
{
    private readonly SceneManager _scenes;
    private readonly Game _game;

    private SpriteFont _font = null!;
    private float _time;

    private static readonly string[] Logo =
    [
        """ .----------------.  .----------------.  .----------------.  .----------------.  .----------------.  .----------------.  .----------------.  .----------------. """,
        """| .--------------. || .--------------. || .--------------. || .--------------. || .--------------. || .--------------. || .--------------. || .--------------. |""",
        """| | ____   ____  | || |     _____    | || | ____    ____ | || |  _______     | || |      __      | || |     ______   | || |  _________   | || |  _______     | |""",
        """| ||_  _| |_  _| | || |    |_   _|   | || ||_   \  /   _|| || | |_   __ \   | || |     /  \     | || |   .' ___  |  | || | |_   ___  |  | || | |_   __ \   | |""",
        """| |  \ \   / /   | || |      | |     | || |  |   \/   |  | || |   | |__) |  | || |    / /\ \    | || |  / .'   \_|  | || |   | |_  \_|  | || |   | |__) |  | |""",
        """| |   \ \ / /    | || |      | |     | || |  | |\  /| |  | || |   |  __ /   | || |   / ____ \   | || |  | |         | || |   |  _|  _   | || |   |  __ /   | |""",
        """| |    \ ' /     | || |     _| |_    | || | _| |_\/_| |_ | || |  _| |  \ \_ | || | _/ /    \ \_ | || |  \ `.___.'\  | || |  _| |___/ |  | || |  _| |  \ \_ | |""",
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
    }

    public void UnloadContent() { }

    public void Update(GameTime gameTime)
    {
        _time += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (InputSystem.WasPressed(Keys.Escape))
            _game.Exit();

        if (InputSystem.WasPressed(Keys.Enter))
            _scenes.Transition(new GameScene(_scenes, _game));
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Measure logo to compute uniform scale fitting screen width
        float maxWidth = 0f;
        foreach (var line in Logo)
            maxWidth = MathF.Max(maxWidth, _font.MeasureString(line).X);

        float scale = 480f / maxWidth;
        float lineH = _font.LineSpacing * scale;
        float totalH = lineH * Logo.Length;
        float startY = (854f - totalH) / 3f; // place in upper third

        var transform = Matrix.CreateScale(scale, scale, 1f)
                      * Matrix.CreateTranslation(0f, startY, 0f);

        spriteBatch.Begin(transformMatrix: transform);

        for (int i = 0; i < Logo.Length; i++)
        {
            // Neon wave: hue shifts across lines and over time
            float hue = (_time * 80f + i * 18f) % 360f;
            Color color = HsvToColor(hue, 1f, 1f);
            spriteBatch.DrawString(_font, Logo[i], new Vector2(0f, i * _font.LineSpacing), color);
        }

        spriteBatch.End();
    }

    private static Color HsvToColor(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1f - MathF.Abs(h / 60f % 2f - 1f));
        float m = v - c;
        float r, g, b;
        if      (h < 60f)  { r = c; g = x; b = 0; }
        else if (h < 120f) { r = x; g = c; b = 0; }
        else if (h < 180f) { r = 0; g = c; b = x; }
        else if (h < 240f) { r = 0; g = x; b = c; }
        else if (h < 300f) { r = x; g = 0; b = c; }
        else               { r = c; g = 0; b = x; }
        return new Color(r + m, g + m, b + m);
    }
}
