using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace VimRacer;

public sealed class MainMenuScene : IScene
{
    private readonly SceneManager _scenes;

    public MainMenuScene(SceneManager scenes) => _scenes = scenes;

    public void Initialize() { }
    public void LoadContent() { }
    public void UnloadContent() { }

    public void Update(GameTime gameTime)
    {
        if (InputSystem.WasPressed(Keys.Escape))
            System.Environment.Exit(0);

        if (InputSystem.WasPressed(Keys.Enter))
            _scenes.Transition(new LobbyScene(_scenes));
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Placeholder: nothing to draw without a font loaded yet
    }
}
