// @since 02
#if CH04
using Microsoft.Xna.Framework;
#endif
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#if CH03
using MonsterMaze.UI;
#endif

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
#if CH03

    /// <summary>The four fonts as one family that picks the right size automatically.</summary>
    public FontSet Fonts { get; private set; }
#endif
#if CH04

    // Menus and buttons.
    public Texture2D Pixel { get; private set; }
    public Texture2D Button { get; private set; }
    public Texture2D Panel { get; private set; }
    public Texture2D Icons { get; private set; }
    public Texture2D MenuBackground { get; private set; }
#endif
#if CH11

    // The surfaces of the maze.
    public Texture2D WallStone { get; private set; }
    public Texture2D WallMoss { get; private set; }
    public Texture2D WallCrystal { get; private set; }
    public Texture2D Floor { get; private set; }
    public Texture2D Ceiling { get; private set; }
#endif
#if CH15
    public Texture2D WallExit { get; private set; }
    public Texture2D ExitLight { get; private set; }
#endif
#if CH16

    /// <summary>Rex's animation frames: 4 columns x 2 rows of 512 x 640 pixels.</summary>
    public Texture2D RexSheet { get; private set; }
#endif
#if CH29

    // Shaders, compiled ahead of time (see Chapter 29).
    public Effect PostProcessEffect { get; private set; }
#endif
#if CH30
    public Texture2D Vignette { get; private set; }
#endif
#if CH31
    public Effect BloomEffect { get; private set; }
#endif
#if CH32

    // Particle sprites.
    public Texture2D ParticleSoft { get; private set; }
    public Texture2D ParticleDust { get; private set; }
    public Texture2D ParticleSpark { get; private set; }
#endif
#if CH33

    // The "eaten" ending.
    public Texture2D JawsTop { get; private set; }
    public Texture2D JawsBottom { get; private set; }
    public Texture2D EatenBackground { get; private set; }
#endif
#if CH34
    public Texture2D EscapeSky { get; private set; }
#endif
#if CH38
    public Texture2D Logo { get; private set; }
#endif

    public void Load(ContentManager content, GraphicsDevice graphicsDevice)
    {
        FontSmall = content.Load<SpriteFont>("Fonts/Small");
        FontMedium = content.Load<SpriteFont>("Fonts/Medium");
        FontLarge = content.Load<SpriteFont>("Fonts/Large");
        FontTitle = content.Load<SpriteFont>("Fonts/Title");
#if CH03
        Fonts = new FontSet(FontSmall, FontMedium, FontLarge, FontTitle);
#endif
#if CH04

        Pixel = new Texture2D(graphicsDevice, 1, 1);
        Pixel.SetData(new[] { Color.White });
        Draw2D.Pixel = Pixel;
        Button = content.Load<Texture2D>("UI/button");
        Panel = content.Load<Texture2D>("UI/panel");
        Icons = content.Load<Texture2D>("UI/icons");
        MenuBackground = content.Load<Texture2D>("UI/menu_bg");
#endif
#if CH11

        WallStone = content.Load<Texture2D>("Textures/wall_stone");
        WallMoss = content.Load<Texture2D>("Textures/wall_moss");
        WallCrystal = content.Load<Texture2D>("Textures/wall_crystal");
        Floor = content.Load<Texture2D>("Textures/floor");
        Ceiling = content.Load<Texture2D>("Textures/ceiling");
#endif
#if CH15
        WallExit = content.Load<Texture2D>("Textures/wall_exit");
        ExitLight = content.Load<Texture2D>("Textures/exit_light");
#endif
#if CH16

        RexSheet = content.Load<Texture2D>("Sprites/rex_sheet");
#endif
#if CH29

        PostProcessEffect = content.Load<Effect>("Effects/PostProcess");
#endif
#if CH30
        Vignette = content.Load<Texture2D>("UI/vignette");
#endif
#if CH31
        BloomEffect = content.Load<Effect>("Effects/Bloom");
#endif
#if CH32

        ParticleSoft = content.Load<Texture2D>("Sprites/particle_soft");
        ParticleDust = content.Load<Texture2D>("Sprites/particle_dust");
        ParticleSpark = content.Load<Texture2D>("Sprites/particle_spark");
#endif
#if CH33

        JawsTop = content.Load<Texture2D>("Sprites/jaws_top");
        JawsBottom = content.Load<Texture2D>("Sprites/jaws_bottom");
        EatenBackground = content.Load<Texture2D>("UI/eaten_bg");
#endif
#if CH34
        EscapeSky = content.Load<Texture2D>("UI/escape_sky");
#endif
#if CH38
        Logo = content.Load<Texture2D>("UI/logo");
#endif
    }
}
