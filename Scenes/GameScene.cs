using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VimRacer;

public sealed class GameScene : IScene
{
    private readonly SceneManager _scenes;
    private readonly Game _game;

    private Player _player = null!;
    private Texture2D _pixel = null!;

    private const float TrackLength = 10000f;
    private const float FinishLineY = 9800f;
    private const int ScreenHeight = 854;

    public GameScene(SceneManager scenes, Game game)
    {
        _scenes = scenes;
        _game = game;
    }

    public void Initialize()
    {
        float startX = (Player.TrackLeft + Player.TrackRight) / 2f;
        _player = new Player(new Vector2(startX, 200f));
    }

    public void LoadContent()
    {
        _pixel = new Texture2D(_game.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void UnloadContent()
    {
        _pixel.Dispose();
    }

    public void Update(GameTime gameTime)
    {
        _player.Update(gameTime);

        if (_player.Position.Y >= FinishLineY)
            _scenes.Transition(new ResultScene(_scenes, _game));
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        float cameraY = MathF.Max(0f, _player.Position.Y - ScreenHeight / 2f);

        spriteBatch.Begin();
        DrawTrack(spriteBatch, cameraY);
        _player.Draw(spriteBatch, _pixel, cameraY);
        spriteBatch.End();
    }

    private void DrawTrack(SpriteBatch spriteBatch, float cameraY)
    {
        // Track surface
        spriteBatch.Draw(_pixel,
            new Rectangle((int)Player.TrackLeft, 0, (int)(Player.TrackRight - Player.TrackLeft), ScreenHeight),
            new Color(40, 40, 40));

        // Lane markers — dashed lines every 200 world units
        float markerSpacing = 200f;
        float firstMarkerY = MathF.Floor(cameraY / markerSpacing) * markerSpacing;
        for (float worldY = firstMarkerY; worldY < cameraY + ScreenHeight; worldY += markerSpacing)
        {
            int screenY = (int)(worldY - cameraY);
            spriteBatch.Draw(_pixel,
                new Rectangle((int)Player.TrackLeft + 8, screenY, (int)(Player.TrackRight - Player.TrackLeft) - 16, 2),
                new Color(70, 70, 70));
        }

        // Finish line
        int finishScreenY = (int)(FinishLineY - cameraY);
        if (finishScreenY >= -4 && finishScreenY <= ScreenHeight)
            spriteBatch.Draw(_pixel, new Rectangle((int)Player.TrackLeft, finishScreenY, (int)(Player.TrackRight - Player.TrackLeft), 6), Color.Yellow);

        // Left wall
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, (int)Player.TrackLeft, ScreenHeight), new Color(80, 80, 80));

        // Right wall
        spriteBatch.Draw(_pixel, new Rectangle((int)Player.TrackRight, 0, 480 - (int)Player.TrackRight, ScreenHeight), new Color(80, 80, 80));
    }
}
