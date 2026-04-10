using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace VimRacer;

public sealed class HUD
{
    private readonly SpriteFont _font;
    private readonly Texture2D  _pixel;

    private static readonly Color ColorActive  = Color.Cyan;
    private static readonly Color ColorDone    = new(60, 60, 60);
    private static readonly Color ColorPending = new(180, 180, 180);
    private static readonly Color ColorPanel   = new(20, 20, 30);
    private static readonly Color ColorSpeed   = new(50, 205, 50);
    private static readonly Color ColorBullet  = new(255, 200, 50);
    private static readonly Color ColorStreak  = new(255, 140, 0);

    public HUD(SpriteFont font, Texture2D pixel)
    {
        _font  = font;
        _pixel = pixel;
    }

    public void Draw(SpriteBatch sb, Player player, ComboSystem combo, GameLayout layout)
    {
        DrawComboPanel(sb, combo, layout);
        DrawInfoPanel(sb, player, combo, layout);
    }

    // ── Combo panel (left section) ───────────────────────────────────────────

    private void DrawComboPanel(SpriteBatch sb, ComboSystem combo, GameLayout layout)
    {
        sb.Draw(_pixel, layout.ComboRect, ColorPanel);

        int keySz  = Math.Min(52, layout.ComboW - 16);
        int keyGap = 8;
        int keyX   = layout.ComboX + (layout.ComboW - keySz) / 2;

        int stackH = combo.Combo.Length * keySz + (combo.Combo.Length - 1) * keyGap;
        int stackY = (layout.ScreenH - stackH) / 2 - 20; // shift up slightly to leave room for streak

        // Keys in single column
        for (int i = 0; i < combo.Combo.Length; i++)
        {
            int ky = stackY + i * (keySz + keyGap);

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

            sb.Draw(_pixel, new Rectangle(keyX, ky, keySz, keySz), bg);

            string label    = KeyLabel(combo.Combo[i]);
            Vector2 labelSz = _font.MeasureString(label);
            sb.DrawString(_font, label,
                new Vector2(keyX + (keySz - labelSz.X) / 2f, ky + (keySz - labelSz.Y) / 2f),
                fg);
        }

        // Timer bar
        int timerY = stackY + stackH + 12;
        int timerX = layout.ComboX + 8;
        int timerW = layout.ComboW - 16;
        int filledW = Math.Max(0, (int)(timerW * combo.TimeLeft / combo.TimeLimit));

        sb.Draw(_pixel, new Rectangle(timerX, timerY, timerW, 6), new Color(40, 40, 55));
        Color timerColor = combo.TimeLeft < combo.TimeLimit * 0.25f ? Color.Red : ColorActive;
        sb.Draw(_pixel, new Rectangle(timerX, timerY, filledW, 6), timerColor);

        // Streak counter
        string streakText = $"{combo.Streak}/5";
        Vector2 streakSz  = _font.MeasureString(streakText);
        sb.DrawString(_font, streakText,
            new Vector2(layout.ComboX + (layout.ComboW - streakSz.X) / 2f, timerY + 14),
            ColorStreak);
    }

    // ── Info panel (right section) ───────────────────────────────────────────

    private void DrawInfoPanel(SpriteBatch sb, Player player, ComboSystem combo, GameLayout layout)
    {
        sb.Draw(_pixel, layout.InfoRect, ColorPanel);

        const int SegH  = 18;
        const int SegGap = 4;
        int segW   = Math.Max(4, layout.InfoW - 16);
        int segX   = layout.InfoX + (layout.InfoW - segW) / 2;
        int meterH = 10 * SegH + 9 * SegGap;
        int meterY = (layout.ScreenH - meterH) / 2;

        // Speed meter: 10 segments, filled bottom-up
        for (int i = 0; i < 10; i++)
        {
            int sy     = meterY + (9 - i) * (SegH + SegGap);
            bool filled = i < player.MaxSpeedLevel;
            sb.Draw(_pixel, new Rectangle(segX, sy, segW, SegH),
                filled ? ColorSpeed : new Color(30, 40, 30));
        }

        // Bullet indicator above speed meter
        string bulletText = combo.HasBullet ? "[*]" : "[ ]";
        Vector2 bulletSz  = _font.MeasureString(bulletText);
        float bx = layout.InfoX + (layout.InfoW - bulletSz.X) / 2f;
        float by = meterY - bulletSz.Y - 10f;
        sb.DrawString(_font, bulletText, new Vector2(bx, by),
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
