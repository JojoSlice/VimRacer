using System;
using System.Collections.Generic;
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

    private WallNarrowing[]  _narrowings = [];
    private TrackObstacle[]  _obstacles  = [];
    private readonly Random _rng = new();

    private readonly CollisionSystem    _collision = new();
    private readonly List<CollisionHit> _hits      = new();
    private ColliderBody                _playerBody = null!;

    private float _collisionCooldown = 0f;
    private const float CollisionCooldownDuration = 1.0f;

    private const float TrackStart   = 30000f; // player starts near bottom
    private const float FinishLineY  = 200f;   // finish line near top
    private const float MarkerSpacing = 500f;   // lane marker interval

    public GameScene(SceneManager scenes, Game game)
    {
        _scenes = scenes;
        _game   = game;
    }

    public void Initialize()
    {
        var layout  = GameLayout.FromViewport(_game.GraphicsDevice.Viewport);
        float startX = (layout.TrackLeft + layout.TrackRight) / 2f;
        _player = new Player(new Vector2(startX, TrackStart));
        _combo  = new ComboSystem();
        _combo.GenerateCombo(_player.MaxSpeedLevel);
        _narrowings = WallNarrowingGenerator.Generate(TrackStart, FinishLineY, _rng);
        _obstacles  = TrackObstacleGenerator.Generate(TrackStart, FinishLineY, _rng);

        _playerBody = _collision.Add(CollisionLayer.Player);

        foreach (var obs in _obstacles)
        {
            var b = _collision.Add(CollisionLayer.Obstacle);
            b.Bounds = new Aabb(
                obs.WorldLeft(layout.TrackLeft, layout.RacingW), obs.WorldTop,
                obs.WorldRight(layout.TrackLeft, layout.RacingW), obs.WorldBottom);
        }

        foreach (var n in _narrowings)
        {
            float li = n.LeftInset(layout.RacingW);
            float ri = n.RightInset(layout.RacingW);
            if (li > 0f)
            {
                var b = _collision.Add(CollisionLayer.Wall);
                b.Bounds = new Aabb(layout.TrackLeft, n.StartY, layout.TrackLeft + li, n.EndY);
            }
            if (ri > 0f)
            {
                var b = _collision.Add(CollisionLayer.Wall);
                b.Bounds = new Aabb(layout.TrackRight - ri, n.StartY, layout.TrackRight, n.EndY);
            }
        }
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

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _collisionCooldown = MathF.Max(0f, _collisionCooldown - dt);

        _playerBody.Bounds = Aabb.FromCenter(
            _player.Position.X, _player.Position.Y, Player.Width, Player.Height);

        _player.Update(gameTime, layout.TrackLeft, layout.TrackRight);

        _collision.QueryHits(_hits);
        ProcessHits();

        if (_player.Position.Y <= FinishLineY)
            _scenes.Transition(new ResultScene(_scenes, _game));
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var layout  = GameLayout.FromViewport(_game.GraphicsDevice.Viewport);
        float cameraY = MathF.Max(0f, _player.Position.Y - layout.ScreenH / 2f);

        spriteBatch.Begin();
        DrawTrack(spriteBatch, cameraY, layout);
        DrawNarrowings(spriteBatch, cameraY, layout);
        DrawObstacles(spriteBatch, cameraY, layout);
        _player.Draw(spriteBatch, _pixel, cameraY);
        _hud.Draw(spriteBatch, _player, _combo, layout, TrackStart, FinishLineY);
        spriteBatch.End();
    }

    private void DrawTrack(SpriteBatch sb, float cameraY, GameLayout layout)
    {
        // Track surface
        sb.Draw(_pixel, layout.RacingRect, new Color(40, 40, 40));

        // Lane markers every 200 world units
        float first = MathF.Floor(cameraY / MarkerSpacing) * MarkerSpacing;
        for (float worldY = first; worldY < cameraY + layout.ScreenH; worldY += MarkerSpacing)
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

    private void ProcessHits()
    {
        foreach (var hit in _hits)
        {
            bool aIsPlayer = hit.A.Layer == CollisionLayer.Player;
            bool bIsPlayer = hit.B.Layer == CollisionLayer.Player;
            if (!aIsPlayer && !bIsPlayer) continue;

            var other  = aIsPlayer ? hit.B      : hit.A;
            var normal = aIsPlayer ? hit.Normal : -hit.Normal;

            _player.Position += normal * hit.Depth;

            if (_collisionCooldown <= 0f &&
                (other.Layer == CollisionLayer.Obstacle || other.Layer == CollisionLayer.Wall))
            {
                _player.SetMaxSpeedLevel(_player.MaxSpeedLevel - 1);
                _combo.ResetStreak();
                _collisionCooldown = CollisionCooldownDuration;
            }
        }
    }

    private void DrawObstacles(SpriteBatch sb, float cameraY, GameLayout layout)
    {
        var obstacleColor = new Color(180, 100, 20);

        foreach (ref readonly var obs in _obstacles.AsSpan())
        {
            int screenTop = (int)(obs.WorldTop    - cameraY);
            int screenBot = (int)(obs.WorldBottom - cameraY);

            if (screenBot < 0 || screenTop > layout.ScreenH) continue;

            int top = Math.Max(screenTop, 0);
            int bot = Math.Min(screenBot, layout.ScreenH);
            int h   = bot - top;
            if (h <= 0) continue;

            int x = (int)obs.WorldLeft(layout.TrackLeft, layout.RacingW);
            int w = (int)(obs.WidthFraction * layout.RacingW);
            sb.Draw(_pixel, new Rectangle(x, top, w, h), obstacleColor);
        }
    }

    private void DrawNarrowings(SpriteBatch sb, float cameraY, GameLayout layout)
    {
        var wallColor = new Color(160, 50, 50);

        foreach (ref readonly var n in _narrowings.AsSpan())
        {
            int screenTop    = (int)(n.StartY - cameraY);
            int screenBottom = (int)(n.EndY   - cameraY);

            if (screenBottom < 0 || screenTop > layout.ScreenH) continue;

            int top = Math.Max(screenTop, 0);
            int bot = Math.Min(screenBottom, layout.ScreenH);
            int h   = bot - top;
            if (h <= 0) continue;

            int leftInset  = (int)n.LeftInset(layout.RacingW);
            int rightInset = (int)n.RightInset(layout.RacingW);

            if (leftInset > 0)
                sb.Draw(_pixel, new Rectangle(layout.RacingX, top, leftInset, h), wallColor);

            if (rightInset > 0)
                sb.Draw(_pixel, new Rectangle(layout.RacingX + layout.RacingW - rightInset, top, rightInset, h), wallColor);
        }
    }
}
