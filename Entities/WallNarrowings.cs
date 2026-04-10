using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

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

    public float LeftInset(float racingW)  => LeftInsetFraction  * racingW;
    public float RightInset(float racingW) => RightInsetFraction * racingW;
}

public static class WallNarrowingGenerator
{
    private const float ObstacleLength = 800f;

    public static WallNarrowing[] Generate(float trackStart, float finishLineY, Random rng)
    {
        var list  = new List<WallNarrowing>();
        float y   = trackStart - 2000f; // leave clearance near start
        float stop = finishLineY + 1000f;
        float span = trackStart - finishLineY;

        while (y > stop)
        {
            float obstacleEnd   = y;
            float obstacleStart = y - ObstacleLength;

            if (obstacleStart <= stop) break;

            float t = MathF.Min(1f, (trackStart - y) / span); // 0 = start, 1 = finish

            int variant = rng.Next(3); // 0 = left, 1 = right, 2 = both
            float leftFrac  = 0f;
            float rightFrac = 0f;

            float minInset = MathHelper.Lerp(0.10f, 0.20f, t);
            float maxInset = MathHelper.Lerp(0.20f, 0.35f, t);
            float minBoth  = MathHelper.Lerp(0.06f, 0.12f, t);
            float maxBoth  = MathHelper.Lerp(0.10f, 0.20f, t);

            switch (variant)
            {
                case 0: leftFrac  = minInset + (float)rng.NextDouble() * (maxInset - minInset); break;
                case 1: rightFrac = minInset + (float)rng.NextDouble() * (maxInset - minInset); break;
                case 2:
                    leftFrac  = minBoth + (float)rng.NextDouble() * (maxBoth - minBoth);
                    rightFrac = minBoth + (float)rng.NextDouble() * (maxBoth - minBoth);
                    break;
            }

            list.Add(new WallNarrowing(obstacleStart, obstacleEnd, leftFrac, rightFrac));

            float minSpacing = MathHelper.Lerp(2500f, 800f, t);
            float maxSpacing = MathHelper.Lerp(3500f, 1600f, t);
            y = obstacleStart - (minSpacing + (float)rng.NextDouble() * (maxSpacing - minSpacing));
        }

        return list.ToArray();
    }
}
