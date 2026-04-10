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

    public void Draw(SpriteBatch sb, Player player, ComboSystem combo, GameLayout layout,
                     float trackStart, float trackFinish)
    {
        DrawComboPanel(sb, combo, layout);
        DrawInfoPanel(sb, player, combo, layout, trackStart, trackFinish);
    }

    // ── Combo panel (left section) ───────────────────────────────────────────

    private void DrawComboPanel(SpriteBatch sb, ComboSystem combo, GameLayout layout)
    {
        sb.Draw(_pixel, layout.ComboRect, ColorPanel);

        int n      = combo.Combo.Length;
        int keyGap = 6;
        int avail  = layout.ComboW - 16;
        int keySz  = Math.Clamp((avail - (n - 1) * keyGap) / n, 14, 52);
        int rowW   = n * keySz + (n - 1) * keyGap;
        int rowX   = layout.ComboX + (layout.ComboW - rowW) / 2;
        int rowY   = layout.ScreenH / 2 - keySz - 16;

        // Keys in horizontal row
        for (int i = 0; i < n; i++)
        {
            int kx = rowX + i * (keySz + keyGap);

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

            sb.Draw(_pixel, new Rectangle(kx, rowY, keySz, keySz), bg);

            string label    = KeyLabel(combo.Combo[i]);
            Vector2 labelSz = _font.MeasureString(label);
            sb.DrawString(_font, label,
                new Vector2(kx + (keySz - labelSz.X) / 2f, rowY + (keySz - labelSz.Y) / 2f),
                fg);
        }

        // Timer bar
        int timerY  = rowY + keySz + 10;
        int timerX  = layout.ComboX + 8;
        int timerW  = layout.ComboW - 16;
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

    private void DrawInfoPanel(SpriteBatch sb, Player player, ComboSystem combo,
                               GameLayout layout, float trackStart, float trackFinish)
    {
        sb.Draw(_pixel, layout.InfoRect, ColorPanel);

        const int PadB    = 8;
        const int SegH    = 10;
        const int SegGap  = 3;
        const int MeterH  = 10 * SegH + 9 * SegGap; // 127px
        const int MeterW  = 10;
        int meterX = layout.InfoX + layout.InfoW - MeterW - 4;

        // ── Bottom: bullet + speed meter ────────────────────────────────────
        string bulletText = combo.HasBullet ? "[*]" : "[ ]";
        Vector2 bulletSz  = _font.MeasureString(bulletText);
        int bulletY = layout.ScreenH - PadB - (int)bulletSz.Y;
        float bx = layout.InfoX + (layout.InfoW - bulletSz.X) / 2f;
        sb.DrawString(_font, bulletText, new Vector2(bx, bulletY),
            combo.HasBullet ? ColorBullet : new Color(80, 80, 80));

        int meterBottom = bulletY - 8;
        int meterTop    = meterBottom - MeterH;

        // Speed meter: 10 segments, filled bottom-up (narrow bar, right-aligned)
        for (int i = 0; i < 10; i++)
        {
            int sy     = meterTop + (9 - i) * (SegH + SegGap);
            bool filled = i < player.MaxSpeedLevel;
            sb.Draw(_pixel, new Rectangle(meterX, sy, MeterW, SegH),
                filled ? ColorSpeed : new Color(30, 40, 30));
        }

        // Current speed (km/h) horizontal, to the left of the meter
        const float SpeedScale = 1.3f;
        string speedNum  = $"{(int)player.Speed}";
        string speedUnit = "km/h";
        Vector2 numSz  = _font.MeasureString(speedNum)  * SpeedScale;
        Vector2 unitSz = _font.MeasureString(speedUnit) * SpeedScale;
        float textCenterX = layout.InfoX + (meterX - layout.InfoX - 4f) / 2f;
        float lineH       = _font.LineSpacing * SpeedScale;
        float blockH      = lineH * 2f + 2f;
        float blockTop    = meterTop + (MeterH - blockH) / 2f;
        sb.DrawString(_font, speedNum,
            new Vector2(textCenterX - numSz.X / 2f, blockTop),
            ColorSpeed, 0f, Vector2.Zero, SpeedScale, SpriteEffects.None, 0f);
        sb.DrawString(_font, speedUnit,
            new Vector2(textCenterX - unitSz.X / 2f, blockTop + lineH + 2f),
            new Color(80, 140, 80), 0f, Vector2.Zero, SpeedScale, SpriteEffects.None, 0f);

        // ── Progress strip ───────────────────────────────────────────────────
        const int StripPadT = 16;
        const int StripPadB = 16;
        const int StripW    = 6;
        int stripTop    = StripPadT;
        int stripBottom = meterTop - StripPadB;
        int stripH      = stripBottom - stripTop;
        int stripX      = layout.InfoX + (layout.InfoW - StripW) / 2;

        // Background (full track)
        sb.Draw(_pixel, new Rectangle(stripX, stripTop, StripW, stripH), new Color(40, 40, 55));

        // Player progress marker
        float progress  = Math.Clamp((trackStart - player.Position.Y) / (trackStart - trackFinish), 0f, 1f);
        int   markerY   = (int)(stripBottom - progress * stripH);
        const int MarkerH = 4;
        const int MarkerW = 14;
        sb.Draw(_pixel,
            new Rectangle(layout.InfoX + (layout.InfoW - MarkerW) / 2, markerY - MarkerH / 2, MarkerW, MarkerH),
            Color.Cyan);
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
