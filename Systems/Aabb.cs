using System;
using Microsoft.Xna.Framework;

namespace VimRacer;

public readonly struct Aabb
{
    public readonly float Left, Top, Right, Bottom;

    public float Width  => Right - Left;
    public float Height => Bottom - Top;

    public Aabb(float left, float top, float right, float bottom)
    {
        Left   = left;
        Top    = top;
        Right  = right;
        Bottom = bottom;
    }

    public static Aabb FromCenter(float cx, float cy, float w, float h)
        => new(cx - w / 2f, cy - h / 2f, cx + w / 2f, cy + h / 2f);

    /// <summary>
    /// Returns true if this AABB overlaps <paramref name="other"/>.
    /// <paramref name="normal"/> points from other toward this (direction to push this out).
    /// <paramref name="depth"/> is the minimum separation distance.
    /// </summary>
    public bool Penetration(in Aabb other, out Vector2 normal, out float depth)
    {
        float penLeft  = other.Right  - Left;
        float penRight = Right  - other.Left;
        float penTop   = other.Bottom - Top;
        float penBot   = Bottom - other.Top;

        if (penLeft <= 0f || penRight <= 0f || penTop <= 0f || penBot <= 0f)
        {
            normal = Vector2.Zero;
            depth  = 0f;
            return false;
        }

        float min = MathF.Min(MathF.Min(penLeft, penRight), MathF.Min(penTop, penBot));

        if      (min == penLeft)  { normal = new Vector2(-1f,  0f); depth = penLeft;  }
        else if (min == penRight) { normal = new Vector2( 1f,  0f); depth = penRight; }
        else if (min == penTop)   { normal = new Vector2( 0f, -1f); depth = penTop;   }
        else                      { normal = new Vector2( 0f,  1f); depth = penBot;   }

        return true;
    }
}
