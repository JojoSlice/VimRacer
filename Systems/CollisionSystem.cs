using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace VimRacer;

[Flags]
public enum CollisionLayer
{
    None     = 0,
    Player   = 1 << 0,
    Obstacle = 1 << 1,
    Wall     = 1 << 2,
    Bullet   = 1 << 3,
}

public sealed class ColliderBody
{
    public Aabb           Bounds;
    public CollisionLayer Layer;
    public bool           Active = true;
}

public readonly struct CollisionHit
{
    public readonly ColliderBody A;       // first body in pair
    public readonly ColliderBody B;       // second body in pair
    public readonly Vector2      Normal;  // direction to push A out of B
    public readonly float        Depth;

    public CollisionHit(ColliderBody a, ColliderBody b, Vector2 normal, float depth)
    {
        A      = a;
        B      = b;
        Normal = normal;
        Depth  = depth;
    }
}

public sealed class CollisionSystem
{
    private static CollisionLayer CollidesWith(CollisionLayer l) => l switch
    {
        CollisionLayer.Player   => CollisionLayer.Obstacle | CollisionLayer.Wall
                                 | CollisionLayer.Bullet   | CollisionLayer.Player,
        CollisionLayer.Obstacle => CollisionLayer.Player | CollisionLayer.Bullet,
        CollisionLayer.Wall     => CollisionLayer.Player | CollisionLayer.Bullet,
        CollisionLayer.Bullet   => CollisionLayer.Player | CollisionLayer.Obstacle | CollisionLayer.Wall,
        _                       => CollisionLayer.None,
    };

    private readonly List<ColliderBody> _bodies = new();

    public ColliderBody Add(CollisionLayer layer)
    {
        var body = new ColliderBody { Layer = layer };
        _bodies.Add(body);
        return body;
    }

    public void Remove(ColliderBody body) => _bodies.Remove(body);

    /// <summary>
    /// Populates <paramref name="results"/> with all overlapping pairs that match
    /// the collision layer matrix. O(n²) — suitable for ~50 bodies.
    /// </summary>
    public void QueryHits(List<CollisionHit> results)
    {
        results.Clear();
        for (int i = 0; i < _bodies.Count; i++)
        {
            var a = _bodies[i];
            if (!a.Active) continue;

            for (int j = i + 1; j < _bodies.Count; j++)
            {
                var b = _bodies[j];
                if (!b.Active) continue;
                if ((CollidesWith(a.Layer) & b.Layer) == CollisionLayer.None) continue;

                if (a.Bounds.Penetration(b.Bounds, out var normal, out var depth))
                    results.Add(new CollisionHit(a, b, normal, depth));
            }
        }
    }
}
