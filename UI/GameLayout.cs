using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VimRacer;

public readonly struct GameLayout
{
    public readonly int ComboX;
    public readonly int ComboW;
    public readonly int RacingX;
    public readonly int RacingW;
    public readonly int InfoX;
    public readonly int InfoW;
    public readonly int ScreenH;

    public float TrackLeft  => RacingX;
    public float TrackRight => RacingX + RacingW;

    public Rectangle ComboRect  => new(ComboX,  0, ComboW,  ScreenH);
    public Rectangle RacingRect => new(RacingX, 0, RacingW, ScreenH);
    public Rectangle InfoRect   => new(InfoX,   0, InfoW,   ScreenH);

    public GameLayout(int vw, int vh)
    {
        ScreenH = vh;
        ComboW  = vw / 4;
        InfoW   = vw / 8;
        RacingW = vw - ComboW - InfoW;
        ComboX  = 0;
        RacingX = ComboW;
        InfoX   = ComboW + RacingW;
    }

    public static GameLayout FromViewport(Viewport vp) => new(vp.Width, vp.Height);
}
