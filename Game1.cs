using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace VimRacer;

public sealed class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SpriteFont _font = null!;
    private readonly SceneManager _scenes;

    private bool _commandActive;
    private string _command = "";

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 480;
        _graphics.PreferredBackBufferHeight = 854;
        _graphics.IsFullScreen = true;
        Content.RootDirectory = "Content";
        IsMouseVisible = false;

        _scenes = new SceneManager();
    }

    protected override void Initialize()
    {
        Window.TextInput += OnTextInput;
        base.Initialize();
        _scenes.Transition(new MainMenuScene(_scenes, this));
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("Fonts/Mono");
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (!_commandActive)
        {
            if (e.Character == ':')
            {
                _commandActive = true;
                _command = ":";
            }
            return;
        }

        if (e.Character == '\r' || e.Character == '\n')
        {
            ExecuteCommand();
        }
        else if (e.Character == '\b')
        {
            if (_command.Length > 1)
                _command = _command[..^1];
            else
                _commandActive = false;
        }
        else
        {
            _command += e.Character;
        }
    }

    private void ExecuteCommand()
    {
        if (_command == ":q")
            Exit();

        _commandActive = false;
        _command = "";
    }

    protected override void Update(GameTime gameTime)
    {
        InputSystem.Update();

        if (_commandActive && InputSystem.WasPressed(Keys.Escape))
        {
            _commandActive = false;
            _command = "";
        }

        _scenes.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _scenes.Draw(_spriteBatch);

        if (_commandActive)
        {
            _spriteBatch.Begin();
            float y = GraphicsDevice.Viewport.Height - _font.LineSpacing - 4;
            _spriteBatch.DrawString(_font, _command, new Vector2(4, y), Color.White);
            _spriteBatch.End();
        }

        base.Draw(gameTime);
    }
}
