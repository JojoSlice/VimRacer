using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VimRacer;

public sealed class Player
{
    public Vector2 Position;
    public float Speed;

    public const float Width  = 32f;
    public const float Height = 48f;

    public const float MinSpeed = 300f;
    private const float LateralSpeed    = 400f;
    private const float SpeedChangeRate = 500f;

    private static readonly float[] MaxSpeeds =
    [
        450f, 550f, 650f, 750f, 850f, 950f, 1050f, 1150f, 1300f, 1500f
    ];

    public int MaxSpeedLevel { get; private set; } = 1;
    public float MaxSpeed => MaxSpeeds[MaxSpeedLevel - 1];

    public Player(Vector2 startPosition)
    {
        Position = startPosition;
        Speed    = MinSpeed;
    }

    public void Update(GameTime gameTime, float trackLeft, float trackRight)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Speed control: k = faster, j = slower
        if (InputSystem.SpeedUp)
            Speed = MathF.Min(Speed + SpeedChangeRate * dt, MaxSpeed);
        else if (InputSystem.SpeedDown)
            Speed = MathF.Max(Speed - SpeedChangeRate * dt, MinSpeed);

        // Move forward (upward on screen = negative Y in world space)
        Position.Y -= Speed * dt;

        // Lateral movement: h = left, l = right
        if (InputSystem.MoveLeft)
            Position.X = MathF.Max(Position.X - LateralSpeed * dt, trackLeft + Width / 2f);
        if (InputSystem.MoveRight)
            Position.X = MathF.Min(Position.X + LateralSpeed * dt, trackRight - Width / 2f);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, float cameraY)
    {
        int screenX = (int)(Position.X - Width / 2f);
        int screenY = (int)(Position.Y - Height / 2f - cameraY);
        spriteBatch.Draw(pixel, new Rectangle(screenX, screenY, (int)Width, (int)Height), Color.Cyan);
    }

    public void SetMaxSpeedLevel(int level)
    {
        MaxSpeedLevel = Math.Clamp(level, 1, 10);
        Speed = MathF.Min(Speed, MaxSpeed);
    }
}
