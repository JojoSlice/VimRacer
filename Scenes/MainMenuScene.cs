using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace VimRacer;

public sealed class MainMenuScene : IScene
{
    private readonly SceneManager _scenes;
    private readonly Game _game;

    public MainMenuScene(SceneManager scenes, Game game)
    {
        _scenes = scenes;
        _game = game;
    }

    public void Initialize() { }
    public void LoadContent() { }
    public void UnloadContent() { }

    public void Update(GameTime gameTime)
    {
        if (InputSystem.WasPressed(Keys.Escape))
            _game.Exit();

        if (InputSystem.WasPressed(Keys.Enter))
            _scenes.Transition(new GameScene(_scenes, _game));
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Placeholder: no font loaded yet
    }
}
