using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace VimRacer;

public sealed class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;
    private readonly SceneManager   _scenes;
    private readonly NetworkManager _network = new();

    private bool _commandActive;
    private string _command = "";

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        _graphics.PreferredBackBufferWidth  = display.Width;
        _graphics.PreferredBackBufferHeight = display.Height;
        _graphics.HardwareModeSwitch = false; // borderless fullscreen
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
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
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
        else if (_command == ":run")
            _scenes.Transition(new GameScene(_scenes, this));
        else if (_command == ":menu")
            _scenes.Transition(new MainMenuScene(_scenes, this));
        else if (_command == ":lobby")
            _scenes.Transition(new LoginScene(_scenes, this, _network));
        else if (_scenes.CurrentScene is LoginScene login)
            login.HandleCommand(_command);
        else if (_scenes.CurrentScene is LobbyScene lobby)
            lobby.HandleCommand(_command);

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
            int vh = GraphicsDevice.Viewport.Height;
            int vw = GraphicsDevice.Viewport.Width;
            int barH = _font.LineSpacing + 8;
            var bgColor = new Color(22, 30, 46);

            _spriteBatch.Begin();
            _spriteBatch.Draw(_pixel, new Rectangle(0, vh - barH, vw, barH), bgColor);
            float tx = 4f, ty = vh - barH + 4f;
            foreach (char c in _command)
            {
                string s = c.ToString();
                _spriteBatch.DrawString(_font, s, new Vector2(tx, ty), Color.White);
                tx += _font.MeasureString(s).X + 2f;
            }
            _spriteBatch.End();
        }

        base.Draw(gameTime);
    }
}
