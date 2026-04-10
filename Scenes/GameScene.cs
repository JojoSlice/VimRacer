using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VimRacer;

public sealed class GameScene : IScene
{
    private readonly SceneManager _scenes;
    private readonly Game _game;

    private Player _player = null!;
    private ComboSystem _combo = null!;
    private HUD _hud = null!;
    private Texture2D _pixel = null!;

    private const float FinishLineY = 9800f;

    public GameScene(SceneManager scenes, Game game)
    {
        _scenes = scenes;
        _game   = game;
    }

    public void Initialize()
    {
        var layout  = GameLayout.FromViewport(_game.GraphicsDevice.Viewport);
        float startX = (layout.TrackLeft + layout.TrackRight) / 2f;
        _player = new Player(new Vector2(startX, 200f));
        _combo  = new ComboSystem();
        _combo.GenerateCombo(_player.MaxSpeedLevel);
    }

    public void LoadContent()
    {
        _pixel = new Texture2D(_game.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        var font = _game.Content.Load<SpriteFont>("Fonts/Mono");
        _hud = new HUD(font, _pixel);
    }

    public void UnloadContent()
    {
        _pixel.Dispose();
    }

    public void Update(GameTime gameTime)
    {
        var result = _combo.Update(gameTime, _player.MaxSpeedLevel);
        switch (result)
        {
            case ComboResult.Correct:
                _player.SetMaxSpeedLevel(_player.MaxSpeedLevel + 1);
                break;
            case ComboResult.Wrong:
            case ComboResult.Timeout:
                _player.SetMaxSpeedLevel(_player.MaxSpeedLevel - 1);
                break;
        }

        var layout = GameLayout.FromViewport(_game.GraphicsDevice.Viewport);
        _player.Update(gameTime, layout.TrackLeft, layout.TrackRight);

        if (_player.Position.Y >= FinishLineY)
            _scenes.Transition(new ResultScene(_scenes, _game));
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var layout  = GameLayout.FromViewport(_game.GraphicsDevice.Viewport);
        float cameraY = MathF.Max(0f, _player.Position.Y - layout.ScreenH / 2f);

        spriteBatch.Begin();
        DrawTrack(spriteBatch, cameraY, layout);
        _player.Draw(spriteBatch, _pixel, cameraY);
        _hud.Draw(spriteBatch, _player, _combo, layout);
        spriteBatch.End();
    }

    private void DrawTrack(SpriteBatch sb, float cameraY, GameLayout layout)
    {
        // Track surface
        sb.Draw(_pixel, layout.RacingRect, new Color(40, 40, 40));

        // Lane markers every 200 world units
        float first = MathF.Floor(cameraY / 200f) * 200f;
        for (float worldY = first; worldY < cameraY + layout.ScreenH; worldY += 200f)
        {
            int sy = (int)(worldY - cameraY);
            if (sy < 0 || sy > layout.ScreenH) continue;
            sb.Draw(_pixel,
                new Rectangle(layout.RacingX + 8, sy, layout.RacingW - 16, 2),
                new Color(65, 65, 65));
        }

        // Finish line
        int finishSY = (int)(FinishLineY - cameraY);
        if (finishSY >= 0 && finishSY <= layout.ScreenH)
            sb.Draw(_pixel,
                new Rectangle(layout.RacingX, finishSY, layout.RacingW, 6),
                Color.Yellow);

        // Section dividers
        sb.Draw(_pixel, new Rectangle(layout.RacingX, 0, 3, layout.ScreenH), new Color(80, 80, 80));
        sb.Draw(_pixel, new Rectangle(layout.InfoX - 3, 0, 3, layout.ScreenH), new Color(80, 80, 80));
    }
}
