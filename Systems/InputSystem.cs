using Microsoft.Xna.Framework.Input;

namespace VimRacer;

public static class InputSystem
{
    private static KeyboardState _prev;
    private static KeyboardState _curr;

    public static void Update()
    {
        _prev = _curr;
        _curr = Keyboard.GetState();
    }

    public static bool IsDown(Keys key) => _curr.IsKeyDown(key);
    public static bool WasPressed(Keys key) => _curr.IsKeyDown(key) && !_prev.IsKeyDown(key);
    public static bool WasReleased(Keys key) => !_curr.IsKeyDown(key) && _prev.IsKeyDown(key);

    // Vim movement (h/l = left/right, held for smooth movement)
    public static bool MoveLeft => IsDown(Keys.H);
    public static bool MoveRight => IsDown(Keys.L);

    // Speed control (j/k held down)
    public static bool SpeedDown => IsDown(Keys.J);
    public static bool SpeedUp => IsDown(Keys.K);

    // Shoot
    public static bool Shoot => WasPressed(Keys.Space);
}
