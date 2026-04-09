using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VimRacer;

public sealed class GameScene : IScene
{
    private readonly SceneManager _scenes;

    public GameScene(SceneManager scenes) => _scenes = scenes;

    public void Initialize() { }
    public void LoadContent() { }
    public void UnloadContent() { }
    public void Update(GameTime gameTime) { }
    public void Draw(SpriteBatch spriteBatch) { }
}
