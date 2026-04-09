using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace VimRacer;

public sealed class HUD
{
    private readonly SpriteFont _font;
    private readonly Texture2D _pixel;

    private static readonly string[] KeyLabels = ["A", "S", "D", "F"];
    private static readonly Keys[] ComboKeys =
        [Keys.A, Keys.S, Keys.D, Keys.F];

    private static readonly Color ColorPending  = new(180, 180, 180);
    private static readonly Color ColorActive   = Color.Cyan;
    private static readonly Color ColorDone     = new(60, 60, 60);
    private static readonly Color ColorBar      = new(20, 20, 30, 180);
    private static readonly Color ColorSpeed    = new(50, 205, 50);
    private static readonly Color ColorBullet   = new(255, 200, 50);
    private static readonly Color ColorStreak   = new(255, 140, 0);

    public HUD(SpriteFont font, Texture2D pixel)
    {
        _font  = font;
        _pixel = pixel;
    }

    public void Draw(SpriteBatch sb, Player player, ComboSystem combo, int viewportW, int viewportH)
    {
        const int PadX   = 8;
        const int PadY   = 8;
        const int BarH   = 70;
        const int KeySz  = 36;
        const int KeyGap = 6;
        const int TimerH = 6;

        // HUD background strip at top
        sb.Draw(_pixel, new Rectangle(0, 0, viewportW, BarH), ColorBar);

        // ── Combo keys ──────────────────────────────────────────────────────
        int totalKeysW = combo.Combo.Length * (KeySz + KeyGap) - KeyGap;
        int keysX      = (viewportW - totalKeysW) / 2;
        int keysY      = PadY;

        for (int i = 0; i < combo.Combo.Length; i++)
        {
            int kx = keysX + i * (KeySz + KeyGap);
            string label = KeyLabel(combo.Combo[i]);

            Color bg, fg;
            if (i < combo.Progress)
            {
                bg = ColorDone;
                fg = new Color(90, 90, 90);
            }
            else if (i == combo.Progress)
            {
                bg = ColorActive;
                fg = Color.Black;
            }
            else
            {
                bg = new Color(40, 40, 55);
                fg = ColorPending;
            }

            sb.Draw(_pixel, new Rectangle(kx, keysY, KeySz, KeySz), bg);

            Vector2 labelSize = _font.MeasureString(label);
            var labelPos = new Vector2(
                kx + (KeySz - labelSize.X) / 2f,
                keysY + (KeySz - labelSize.Y) / 2f);
            sb.DrawString(_font, label, labelPos, fg);
        }

        // ── Timer bar ───────────────────────────────────────────────────────
        int timerY  = keysY + KeySz + 4;
        int timerW  = (int)(totalKeysW * (combo.TimeLeft / combo.TimeLimit));
        timerW      = Math.Max(0, timerW);

        sb.Draw(_pixel, new Rectangle(keysX, timerY, totalKeysW, TimerH), new Color(40, 40, 55));
        Color timerColor = combo.TimeLeft < combo.TimeLimit * 0.25f ? Color.Red : ColorActive;
        sb.Draw(_pixel, new Rectangle(keysX, timerY, timerW, TimerH), timerColor);

        // ── Speed meter (10 segments, right side) ───────────────────────────
        const int SegW  = 12;
        const int SegH  = 24;
        const int SegGp = 3;
        int meterW      = 10 * (SegW + SegGp) - SegGp;
        int meterX      = viewportW - PadX - meterW;
        int meterY      = (BarH - SegH) / 2;

        for (int i = 0; i < 10; i++)
        {
            int sx     = meterX + i * (SegW + SegGp);
            bool filled = i < player.MaxSpeedLevel;
            Color sc   = filled ? ColorSpeed : new Color(30, 40, 30);
            sb.Draw(_pixel, new Rectangle(sx, meterY, SegW, SegH), sc);
        }

        // ── Streak counter (left of speed meter) ─────────────────────────
        string streakText = $"{combo.Streak}/5";
        Vector2 streakSize = _font.MeasureString(streakText);
        sb.DrawString(_font, streakText,
            new Vector2(meterX - streakSize.X - 10, (BarH - streakSize.Y) / 2f),
            ColorStreak);

        // ── Bullet indicator (left side) ────────────────────────────────────
        string bulletText = combo.HasBullet ? "●" : "○";
        sb.DrawString(_font, bulletText,
            new Vector2(PadX, (BarH - _font.LineSpacing) / 2f),
            combo.HasBullet ? ColorBullet : new Color(80, 80, 80));
    }

    private static string KeyLabel(Keys key) => key switch
    {
        Keys.A => "A",
        Keys.S => "S",
        Keys.D => "D",
        Keys.F => "F",
        _      => "?"
    };
}
