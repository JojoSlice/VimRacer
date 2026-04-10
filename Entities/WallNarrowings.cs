using System;

namespace VimRacer;

public readonly struct WallNarrowing
{
    public readonly float StartY;
    public readonly float EndY;
    public readonly float LeftInsetFraction;
    public readonly float RightInsetFraction;

    public WallNarrowing(float startY, float endY, float leftFrac, float rightFrac)
    {
        StartY              = startY;
        EndY                = endY;
        LeftInsetFraction   = leftFrac;
        RightInsetFraction  = rightFrac;
    }

    public bool ContainsY(float worldY) => worldY >= StartY && worldY <= EndY;

    public float LeftInset(float racingW)  => LeftInsetFraction  * racingW;
    public float RightInset(float racingW) => RightInsetFraction * racingW;

    public float EffectiveLeft(float trackLeft, float racingW)   => trackLeft  + LeftInset(racingW);
    public float EffectiveRight(float trackRight, float racingW) => trackRight - RightInset(racingW);
}

public static class WallNarrowingGenerator
{
    private const float ObstacleLength = 800f;
    private const float MinSpacing     = 1500f;
    private const float ExtraSpacing   = 1000f;

    public static WallNarrowing[] Generate(float trackStart, float finishLineY, Random rng)
    {
        var list  = new System.Collections.Generic.List<WallNarrowing>();
        float y   = trackStart - 2000f; // leave clearance near start
        float stop = finishLineY + 1000f;

        while (y > stop)
        {
            float obstacleEnd   = y;
            float obstacleStart = y - ObstacleLength;

            if (obstacleStart <= stop) break;

            int variant = rng.Next(3); // 0 = left, 1 = right, 2 = both
            float leftFrac  = 0f;
            float rightFrac = 0f;

            switch (variant)
            {
                case 0: leftFrac  = 0.13f + (float)rng.NextDouble() * 0.20f; break;
                case 1: rightFrac = 0.13f + (float)rng.NextDouble() * 0.20f; break;
                case 2:
                    leftFrac  = 0.08f + (float)rng.NextDouble() * 0.09f;
                    rightFrac = 0.08f + (float)rng.NextDouble() * 0.09f;
                    break;
            }

            list.Add(new WallNarrowing(obstacleStart, obstacleEnd, leftFrac, rightFrac));

            float spacing = MinSpacing + (float)rng.NextDouble() * ExtraSpacing;
            y = obstacleStart - spacing;
        }

        return list.ToArray();
    }
}
