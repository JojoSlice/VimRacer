using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VimRacer;

public sealed class BulletManager
{
    private const float BulletSpeed  = 1800f;
    public  const float BulletWidth  = 8f;
    public  const float BulletHeight = 16f;

    private sealed class Bullet
    {
        public Vector2            Position;
        public readonly ColliderBody Body;
        public Bullet(Vector2 pos, ColliderBody body) { Position = pos; Body = body; }
    }

    private readonly List<Bullet>    _active    = new();
    private readonly CollisionSystem _collision;

    public BulletManager(CollisionSystem collision) => _collision = collision;

    public void Spawn(float x, float y)
    {
        var body = _collision.Add(CollisionLayer.Bullet);
        body.Bounds = Aabb.FromCenter(x, y, BulletWidth, BulletHeight);
        _active.Add(new Bullet(new Vector2(x, y), body));
    }

    public void Update(float dt, float finishLineY)
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var b = _active[i];

            if (!b.Body.Active) { Remove(i); continue; }

            b.Position.Y   -= BulletSpeed * dt;
            b.Body.Bounds   = Aabb.FromCenter(b.Position.X, b.Position.Y, BulletWidth, BulletHeight);

            if (b.Position.Y < finishLineY) Remove(i);
        }
    }

    private void Remove(int i)
    {
        _collision.Remove(_active[i].Body);
        _active.RemoveAt(i);
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, float cameraY)
    {
        var bulletColor = new Color(255, 200, 50);
        foreach (var b in _active)
        {
            int sx = (int)(b.Position.X - BulletWidth  / 2f);
            int sy = (int)(b.Position.Y - BulletHeight / 2f - cameraY);
            sb.Draw(pixel, new Rectangle(sx, sy, (int)BulletWidth, (int)BulletHeight), bulletColor);
        }
    }
}
