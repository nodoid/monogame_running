using System;
#if CH04
using System.Globalization;
#endif
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#if !CH04
using Microsoft.Xna.Framework.Input;
#endif
#if CH03
using Microsoft.Xna.Framework.Input.Touch;
using MonsterMaze.Platform;
using MonsterMaze.UI;
#endif
#if CH04
using MonsterMaze.Input;
using MonsterMaze.Screens;
#endif
#if CH12
using MonsterMaze.Data;
using MonsterMaze.Rendering;
#endif
#if CH26
using MonsterMaze.Audio;
#endif
#if CH40
using MonsterMaze.Diagnostics;
#endif

namespace MonsterMaze;

/// <summary>
/// The game itself. MonoGame calls Initialize and LoadContent once, then Update and Draw
/// every frame. Everything else (screens, input, audio, saving) hangs off this class.
/// </summary>
public class MonsterMazeGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
#if CH03 && !CH04
    private Texture2D _pixel;
#endif

#if CH03
    public MonsterMazeGame(IPlatformServices platform = null)
#else
    public MonsterMazeGame()
#endif
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.IsFullScreen = true;
        _graphics.SynchronizeWithVerticalRetrace = true;
#if !CH03
        _graphics.SupportedOrientations = DisplayOrientation.Portrait;
#endif
        Content.RootDirectory = "Content";
#if CH03
        Platform = platform ?? new NullPlatformServices();
#endif
#if CH04

        // Format numbers the same way on every phone, so every character is one our fonts contain.
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        // Run the game logic at a steady 60 updates per second.
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
#endif
#if CH37

        // Load saved settings and high scores before anything else needs them.
        SaveStore = new SaveStore(SaveStore.DefaultFolder());
        var saved = SaveStore.Load();
        Settings = saved.Settings ?? new Settings();
        HighScores = HighScoreTable.FromSaveData(saved);
#else
#if CH12
        Settings = new Settings();
#endif
#if CH35
        HighScores = HighScoreTable.CreateDefault();
#endif
#endif
#if CH12 && !CH29

        // Multisampling smooths the jagged edges of the maze walls on the better quality levels.
        _graphics.PreferMultiSampling = QualityProfile.For(Settings.Quality).MultiSampleCount > 0;
#endif
    }
#if CH02

    public SpriteBatch SpriteBatch { get; private set; }
    public GameAssets Assets { get; private set; }
#endif
#if CH03

    /// <summary>Services supplied by the Android or iOS project.</summary>
    public IPlatformServices Platform { get; }

    /// <summary>Converts between pixels and the virtual units our 2D layouts use.</summary>
    public ScreenScaler Scaler { get; } = new();

    public GameOrientation Orientation { get; private set; } = GameOrientation.Portrait;

    /// <summary>The size of the back buffer in pixels.</summary>
    public Point ScreenSize => new(GraphicsDevice.PresentationParameters.BackBufferWidth,
        GraphicsDevice.PresentationParameters.BackBufferHeight);
#endif
#if CH04

    public InputManager Input { get; } = new();
    public ScreenManager Screens { get; private set; }
#endif
#if CH12
    public Settings Settings { get; private set; }
#endif
#if CH26
    public AudioManager Audio { get; private set; }
#endif
#if CH35
    public HighScoreTable HighScores { get; private set; }
#endif
#if CH37
    public SaveStore SaveStore { get; }
#endif
#if CH40
    public FrameCounter FrameCounter { get; } = new();
#endif
#if CH04

    protected override void Initialize()
    {
        // Lets a mouse act as a finger when the game is run on a desktop for testing.
        TouchPanel.EnableMouseTouchPoint = true;
#if CH40
        ApplyFrameRate();
#endif
        base.Initialize();
    }
#endif

#if CH02

    protected override void LoadContent()
    {
        SpriteBatch = new SpriteBatch(GraphicsDevice);
        Assets = new GameAssets();
        Assets.Load(Content, GraphicsDevice);
#if CH03 && !CH04
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
#endif
#if CH03

        SetOrientation(GameOrientation.Portrait);
        UpdateScaler();
#endif
#if CH12

        // Until the player chooses, pick a quality level that suits this device's screen.
        if (!Settings.QualityChosen)
            Settings.Quality = QualityProfile.Recommended(ScreenSize.X, ScreenSize.Y);
#endif
#if CH26

        Audio = new AudioManager(Content, Settings);
#endif
#if CH04

        Screens = new ScreenManager(this);
        Screens.SwitchTo(new TitleScreen());
#endif
    }
#endif

    protected override void Update(GameTime gameTime)
    {
#if !CH04
        // Android's back button arrives as the gamepad's Back button. iOS apps never quit themselves.
        if (!OperatingSystem.IsIOS() &&
            (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
             Keyboard.GetState().IsKeyDown(Keys.Escape)))
        {
            Exit();
        }
#endif
#if CH03 && !CH04

        // Tap anywhere to flip between portrait and landscape.
        foreach (TouchLocation touch in TouchPanel.GetState())
        {
            if (touch.State == TouchLocationState.Released)
            {
                SetOrientation(Orientation == GameOrientation.Portrait
                    ? GameOrientation.Landscape
                    : GameOrientation.Portrait);
            }
        }

        if (ScreenSize != Scaler.PixelSize)
            UpdateScaler();
#endif
#if CH04
        float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Input.Update(deltaSeconds, Scaler, Platform.ConsumeBackRequest());
#if CH26
        Audio.Update(deltaSeconds);
#endif
        Screens.Update(gameTime);
#endif

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
#if !CH02
        GraphicsDevice.Clear(new Color(48, 12, 24));
#endif
#if CH02 && !CH03
        DrawHello(gameTime);
#endif
#if CH03 && !CH04
        DrawPlatformInfo(gameTime);
#endif
#if CH04
        Screens.Draw(gameTime);
#endif
#if CH40

        FrameCounter.FrameDrawn(gameTime);
        if (Settings.ShowFps)
        {
            SpriteBatch.Begin(transformMatrix: Scaler.Transform);
            FrameCounter.Draw(SpriteBatch, Assets.Fonts, Scaler.SafeArea);
            SpriteBatch.End();
        }
#endif

        base.Draw(gameTime);
    }
#if CH03

    /// <summary>
    /// Locks the screen to portrait or landscape. MonoGame needs to know which orientations are
    /// allowed and how big the back buffer should be; the platform then does the actual rotation.
    /// </summary>
    public void SetOrientation(GameOrientation orientation)
    {
        Orientation = orientation;
        bool landscape = orientation == GameOrientation.Landscape;

        DisplayMode display = GraphicsDevice.Adapter.CurrentDisplayMode;
        int longSide = Math.Max(display.Width, display.Height);
        int shortSide = Math.Min(display.Width, display.Height);

        _graphics.SupportedOrientations = landscape
            ? DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight
            : DisplayOrientation.Portrait;
        _graphics.PreferredBackBufferWidth = landscape ? longSide : shortSide;
        _graphics.PreferredBackBufferHeight = landscape ? shortSide : longSide;
        _graphics.ApplyChanges();

        Platform.SetOrientation(orientation);
    }

    /// <summary>Recalculates the virtual resolution after the screen size changes.</summary>
    public void UpdateScaler()
    {
        Point size = ScreenSize;
        Scaler.Update(size.X, size.Y, Platform.GetSafeAreaInsets());
        Assets.Fonts.ScreenScale = Scaler.Scale;
    }
#endif
#if CH37

    /// <summary>Writes settings and high scores to storage.</summary>
    public void Save()
    {
        SaveStore.Save(HighScores.ToSaveData(Settings));
    }

    protected override void OnDeactivated(object sender, EventArgs args)
    {
        base.OnDeactivated(sender, args);

        // Once we are in the background the operating system may close us without warning.
        Save();
    }
#endif
#if CH40

    /// <summary>60 frames per second normally, or 30 to save battery.</summary>
    public void ApplyFrameRate()
    {
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / (Settings.BatterySaver ? 30.0 : 60.0));
    }
#endif
#if CH02 && !CH03

    private void DrawHello(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(24, 8, 16));

        const string text = "HELLO MAZE";
        Viewport viewport = GraphicsDevice.Viewport;
        Vector2 centre = new Vector2(viewport.Width, viewport.Height) / 2f;
        Vector2 size = Assets.FontTitle.MeasureString(text);

        // Scale the text to fill most of the screen width, whatever the device.
        float scale = viewport.Width * 0.8f / size.X;
        float pulse = 0.5f + 0.5f * MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds * 3f);
        Color color = Color.Lerp(new Color(130, 230, 80), new Color(255, 206, 64), pulse);

        SpriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        SpriteBatch.DrawString(Assets.FontTitle, text, centre, color, 0f, size / 2f, scale, SpriteEffects.None, 0f);
        SpriteBatch.End();
    }
#endif
#if CH03 && !CH04

    private void DrawPlatformInfo(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(24, 8, 16));

        Vector2 size = Scaler.VirtualSize;
        Rectangle safe = Scaler.SafeArea;
        FontSet fonts = Assets.Fonts;
        float pulse = 0.5f + 0.5f * MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds * 3f);

        SpriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: Scaler.Transform);

        // Outline the safe area so we can see how notches and rounded corners affect the layout.
        var outline = new Color(130, 230, 80) * 0.6f;
        SpriteBatch.Draw(_pixel, new Rectangle(safe.X, safe.Y, safe.Width, 4), outline);
        SpriteBatch.Draw(_pixel, new Rectangle(safe.X, safe.Bottom - 4, safe.Width, 4), outline);
        SpriteBatch.Draw(_pixel, new Rectangle(safe.X, safe.Y, 4, safe.Height), outline);
        SpriteBatch.Draw(_pixel, new Rectangle(safe.Right - 4, safe.Y, 4, safe.Height), outline);

        var centre = new Vector2(size.X / 2f, size.Y * 0.3f);
        fonts.DrawShadowed(SpriteBatch, "HELLO MAZE", centre, 150f,
            Color.Lerp(new Color(130, 230, 80), new Color(255, 206, 64), pulse));

        float y = size.Y * 0.5f;
        string[] lines =
        {
            $"Platform: {Platform.Name}",
            $"Screen: {Scaler.PixelSize.X} x {Scaler.PixelSize.Y} pixels",
            $"Virtual: {size.X:0} x {size.Y:0} units",
            $"Orientation: {Orientation}",
            "Tap to rotate"
        };
        foreach (string line in lines)
        {
            fonts.DrawShadowed(SpriteBatch, line, new Vector2(size.X / 2f, y), 56f, Color.White);
            y += 80f;
        }

        SpriteBatch.End();
    }
#endif
}
