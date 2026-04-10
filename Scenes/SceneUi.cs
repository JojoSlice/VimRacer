using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VimRacer;

/// <summary>Shared UI constants and draw helpers used by all panel-based scenes.</summary>
internal static class SceneUi
{
    public static readonly Color CmdColor  = new(100, 210, 210);
    public static readonly Color DescColor = new(100, 100, 110);
    public static readonly Color DimColor  = new(80, 80, 80);
    public static readonly Color PanelBg   = new(20, 20, 30);
    public static readonly Color PageBg    = new(10, 10, 14);

    public static void DrawPanel(SpriteBatch sb, Texture2D pixel, float x, float y, float w, float h)
    {
        sb.Draw(pixel, new Rectangle((int)x, (int)y, (int)w, (int)h), PanelBg);
        sb.Draw(pixel, new Rectangle((int)x, (int)y, (int)w, 2), Color.Cyan);
    }

    public static void DrawCommandList(SpriteBatch sb, SpriteFont font,
        (string Cmd, string Desc)[] cmds, float tx, float ty, float cmdColW)
    {
        foreach (var (cmd, desc) in cmds)
        {
            sb.DrawString(font, cmd,  new Vector2(tx, ty), CmdColor);
            sb.DrawString(font, desc, new Vector2(tx + cmdColW + 16f, ty), DescColor);
            ty += font.LineSpacing;
        }
    }
}
