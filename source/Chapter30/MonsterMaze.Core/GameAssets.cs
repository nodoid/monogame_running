using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.UI;

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

    /// <summary>The four fonts as one family that picks the right size automatically.</summary>
    public FontSet Fonts { get; private set; }

    // Menus and buttons.
    public Texture2D Pixel { get; private set; }
    public Texture2D Button { get; private set; }
    public Texture2D Panel { get; private set; }
    public Texture2D Icons { get; private set; }
    public Texture2D MenuBackground { get; private set; }

    // The surfaces of the maze.
    public Texture2D WallStone { get; private set; }
    public Texture2D WallMoss { get; private set; }
    public Texture2D WallCrystal { get; private set; }
    public Texture2D Floor { get; private set; }
    public Texture2D Ceiling { get; private set; }
    public Texture2D WallExit { get; private set; }
    public Texture2D ExitLight { get; private set; }

    /// <summary>Rex's animation frames: 4 columns x 2 rows of 512 x 640 pixels.</summary>
    public Texture2D RexSheet { get; private set; }

    // Shaders, compiled ahead of time (see Chapter 29).
    public Effect PostProcessEffect { get; private set; }
    public Texture2D Vignette { get; private set; }

    public void Load(ContentManager content, GraphicsDevice graphicsDevice)
    {
        FontSmall = content.Load<SpriteFont>("Fonts/Small");
        FontMedium = content.Load<SpriteFont>("Fonts/Medium");
        FontLarge = content.Load<SpriteFont>("Fonts/Large");
        FontTitle = content.Load<SpriteFont>("Fonts/Title");
        Fonts = new FontSet(FontSmall, FontMedium, FontLarge, FontTitle);

        Pixel = new Texture2D(graphicsDevice, 1, 1);
        Pixel.SetData(new[] { Color.White });
        Draw2D.Pixel = Pixel;
        Button = content.Load<Texture2D>("UI/button");
        Panel = content.Load<Texture2D>("UI/panel");
        Icons = content.Load<Texture2D>("UI/icons");
        MenuBackground = content.Load<Texture2D>("UI/menu_bg");

        WallStone = content.Load<Texture2D>("Textures/wall_stone");
        WallMoss = content.Load<Texture2D>("Textures/wall_moss");
        WallCrystal = content.Load<Texture2D>("Textures/wall_crystal");
        Floor = content.Load<Texture2D>("Textures/floor");
        Ceiling = content.Load<Texture2D>("Textures/ceiling");
        WallExit = content.Load<Texture2D>("Textures/wall_exit");
        ExitLight = content.Load<Texture2D>("Textures/exit_light");

        RexSheet = content.Load<Texture2D>("Sprites/rex_sheet");

        PostProcessEffect = content.Load<Effect>("Effects/PostProcess");
        Vignette = content.Load<Texture2D>("UI/vignette");
    }
}
