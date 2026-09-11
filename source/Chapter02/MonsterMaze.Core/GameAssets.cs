using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonsterMaze;

/// <summary>
/// Loads every piece of content once, at start-up, and keeps it for the lifetime of the game.
/// Screens ask this class for textures and fonts instead of loading their own copies.
/// </summary>
public sealed class GameAssets
{
    // Fonts, smallest first. All four are Roboto Bold at different sizes.
    public SpriteFont FontSmall { get; private set; }
    public SpriteFont FontMedium { get; private set; }
    public SpriteFont FontLarge { get; private set; }
    public SpriteFont FontTitle { get; private set; }

    public void Load(ContentManager content, GraphicsDevice graphicsDevice)
    {
        FontSmall = content.Load<SpriteFont>("Fonts/Small");
        FontMedium = content.Load<SpriteFont>("Fonts/Medium");
        FontLarge = content.Load<SpriteFont>("Fonts/Large");
        FontTitle = content.Load<SpriteFont>("Fonts/Title");
    }
}
