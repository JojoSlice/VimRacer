using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace VimRacer;

public readonly struct TrackObstacle
{
    public readonly float CenterY;
    public readonly float XFraction;      // center X as fraction of RacingW (0 = left edge, 1 = right edge)
    public readonly float WidthFraction;  // obstacle width as fraction of RacingW
    public readonly float Height;         // world units

    public TrackObstacle(float centerY, float xFrac, float widthFrac, float height)
    {
        CenterY       = centerY;
        XFraction     = xFrac;
        WidthFraction = widthFrac;
        Height        = height;
    }

    public float WorldLeft(float trackLeft, float racingW)  => trackLeft + XFraction * racingW - WidthFraction * racingW / 2f;
    public float WorldRight(float trackLeft, float racingW) => trackLeft + XFraction * racingW + WidthFraction * racingW / 2f;
    public float WorldTop    => CenterY - Height / 2f;
    public float WorldBottom => CenterY + Height / 2f;
}

public static class TrackObstacleGenerator
{
    private const float ObstacleHeight = 90f;

    // X positions that always guarantee clearance on the narrow side
    private static readonly float[] XPositions = [0.35f, 0.5f, 0.65f];

    public static TrackObstacle[] Generate(float trackStart, float finishLineY, Random rng)
    {
        var list = new List<TrackObstacle>();
        float y    = trackStart - 2500f;
        float stop = finishLineY + 1000f;
        float span = trackStart - finishLineY;

        while (y > stop)
        {
            float t = MathF.Min(1f, (trackStart - y) / span); // 0 = start, 1 = finish

            float xFrac     = XPositions[rng.Next(XPositions.Length)];
            float minWidth  = MathHelper.Lerp(0.12f, 0.28f, t);
            float maxWidth  = MathHelper.Lerp(0.20f, 0.38f, t);
            float widthFrac = minWidth + (float)rng.NextDouble() * (maxWidth - minWidth);

            list.Add(new TrackObstacle(y, xFrac, widthFrac, ObstacleHeight));

            float minSpacing = MathHelper.Lerp(2800f, 1000f, t);
            float maxSpacing = MathHelper.Lerp(3800f, 1800f, t);
            y -= minSpacing + (float)rng.NextDouble() * (maxSpacing - minSpacing);
        }

        return list.ToArray();
    }
}
