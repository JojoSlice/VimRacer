using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VimRacer;

public sealed class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private readonly SceneManager _scenes;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 480;
        _graphics.PreferredBackBufferHeight = 854;
        Content.RootDirectory = "Content";
        IsMouseVisible = false;

        _scenes = new SceneManager();
    }

    protected override void Initialize()
    {
        base.Initialize();
        _scenes.Transition(new MainMenuScene(_scenes));
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        InputSystem.Update();
        _scenes.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin();
        _scenes.Draw(_spriteBatch);
        _spriteBatch.End();
        base.Draw(gameTime);
    }
}
