using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VimRacer;

public interface IScene
{
    void Initialize();
    void LoadContent();
    void UnloadContent();
    void Update(GameTime gameTime);
    void Draw(SpriteBatch spriteBatch);
}
