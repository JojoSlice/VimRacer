using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace VimRacer;

public sealed class ResultScene : IScene
{
    private readonly SceneManager _scenes;
    private readonly Game _game;

    public ResultScene(SceneManager scenes, Game game)
    {
        _scenes = scenes;
        _game = game;
    }

    public void Initialize() { }
    public void LoadContent() { }
    public void UnloadContent() { }

    public void Update(GameTime gameTime)
    {
        if (InputSystem.WasPressed(Keys.Enter))
            _scenes.Transition(new MainMenuScene(_scenes, _game));
    }

    public void Draw(SpriteBatch spriteBatch) { }
}
