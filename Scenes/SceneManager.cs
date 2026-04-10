using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VimRacer;

public sealed class SceneManager
{
    private IScene? _current;
    private IScene? _pending;

    public IScene? CurrentScene => _current;

    public void Transition(IScene next)
    {
        _pending = next;
    }

    public void Update(GameTime gameTime)
    {
        if (_pending != null)
        {
            _current?.UnloadContent();
            _current = _pending;
            _pending = null;
            _current.Initialize();
            _current.LoadContent();
        }

        _current?.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _current?.Draw(spriteBatch);
    }
}
