using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VimRacer;

public sealed class LobbyScene : IScene
{
    private readonly SceneManager _scenes;
    private readonly Game _game;

    public LobbyScene(SceneManager scenes, Game game)
    {
        _scenes = scenes;
        _game = game;
    }

    public void Initialize() { }
    public void LoadContent() { }
    public void UnloadContent() { }
    public void Update(GameTime gameTime) { }
    public void Draw(SpriteBatch spriteBatch) { }
}
