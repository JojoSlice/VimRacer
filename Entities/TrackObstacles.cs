using System;
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

    public bool ContainsY(float worldY) => worldY >= WorldTop && worldY <= WorldBottom;
}

public static class TrackObstacleGenerator
{
    private const float MinSpacing   = 2000f;
    private const float ExtraSpacing = 1000f;
    private const float ObstacleHeight = 90f;

    // X positions that always guarantee at least ~22% clearance on the narrow side
    private static readonly float[] XPositions = [0.35f, 0.5f, 0.65f];

    // Width range: 15–25% of RacingW.
    // Worst case: x=0.35, w=0.25 → left clearance = 35% - 12.5% = 22.5% of RacingW
    // At 300px track that is ~67px, well above Player.Width (32px).
    private const float MinWidth = 0.15f;
    private const float MaxWidth = 0.25f;

    public static TrackObstacle[] Generate(float trackStart, float finishLineY, Random rng)
    {
        var list = new System.Collections.Generic.List<TrackObstacle>();
        float y  = trackStart - 2500f;
        float stop = finishLineY + 1000f;

        while (y > stop)
        {
            float xFrac    = XPositions[rng.Next(XPositions.Length)];
            float widthFrac = MinWidth + (float)rng.NextDouble() * (MaxWidth - MinWidth);

            list.Add(new TrackObstacle(y, xFrac, widthFrac, ObstacleHeight));

            float spacing = MinSpacing + (float)rng.NextDouble() * ExtraSpacing;
            y -= spacing;
        }

        return list.ToArray();
    }
}
