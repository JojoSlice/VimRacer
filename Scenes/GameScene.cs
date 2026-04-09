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

    private const float TrackLength = 10000f;
    private const float FinishLineY = 9800f;
    private const int HudHeight = 70;

    public GameScene(SceneManager scenes, Game game)
    {
        _scenes = scenes;
        _game = game;
    }

    public void Initialize()
    {
        float startX = (Player.TrackLeft + Player.TrackRight) / 2f;
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
        // Combo system
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

        _player.Update(gameTime);

        if (_player.Position.Y >= FinishLineY)
            _scenes.Transition(new ResultScene(_scenes, _game));
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var vp = _game.GraphicsDevice.Viewport;
        float cameraY = MathF.Max(0f, _player.Position.Y - (vp.Height - HudHeight) / 2f);

        spriteBatch.Begin();
        DrawTrack(spriteBatch, cameraY, vp.Width, vp.Height);
        _player.Draw(spriteBatch, _pixel, cameraY - HudHeight);
        _hud.Draw(spriteBatch, _player, _combo, vp.Width, vp.Height);
        spriteBatch.End();
    }

    private void DrawTrack(SpriteBatch sb, float cameraY, int vw, int vh)
    {
        // Track surface
        sb.Draw(_pixel,
            new Rectangle((int)Player.TrackLeft, HudHeight, (int)(Player.TrackRight - Player.TrackLeft), vh - HudHeight),
            new Color(40, 40, 40));

        // Lane markers every 200 world units
        float first = MathF.Floor(cameraY / 200f) * 200f;
        for (float worldY = first; worldY < cameraY + vh; worldY += 200f)
        {
            int sy = (int)(worldY - cameraY) + HudHeight;
            if (sy < HudHeight || sy > vh) continue;
            sb.Draw(_pixel,
                new Rectangle((int)Player.TrackLeft + 8, sy, (int)(Player.TrackRight - Player.TrackLeft) - 16, 2),
                new Color(65, 65, 65));
        }

        // Finish line
        int finishSY = (int)(FinishLineY - cameraY) + HudHeight;
        if (finishSY >= HudHeight && finishSY <= vh)
            sb.Draw(_pixel, new Rectangle((int)Player.TrackLeft, finishSY, (int)(Player.TrackRight - Player.TrackLeft), 6), Color.Yellow);

        // Walls
        sb.Draw(_pixel, new Rectangle(0, HudHeight, (int)Player.TrackLeft, vh - HudHeight), new Color(80, 80, 80));
        sb.Draw(_pixel, new Rectangle((int)Player.TrackRight, HudHeight, vw - (int)Player.TrackRight, vh - HudHeight), new Color(80, 80, 80));
    }
}
