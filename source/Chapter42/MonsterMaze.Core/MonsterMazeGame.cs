using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using MonsterMaze.Platform;
using MonsterMaze.UI;
using MonsterMaze.Input;
using MonsterMaze.Screens;
using MonsterMaze.Data;
using MonsterMaze.Rendering;
using MonsterMaze.Audio;
using MonsterMaze.Diagnostics;

namespace MonsterMaze;

/// <summary>
/// The game itself. MonoGame calls Initialize and LoadContent once, then Update and Draw
/// every frame. Everything else (screens, input, audio, saving) hangs off this class.
/// </summary>
public class MonsterMazeGame : Game
{
    private readonly GraphicsDeviceManager _graphics;

    public MonsterMazeGame(IPlatformServices platform = null)
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.IsFullScreen = true;
        _graphics.SynchronizeWithVerticalRetrace = true;
        Content.RootDirectory = "Content";
        Platform = platform ?? new NullPlatformServices();

        // Format numbers the same way on every phone, so every character is one our fonts contain.
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        // Run the game logic at a steady 60 updates per second.
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);

        // Load saved settings and high scores before anything else needs them.
        SaveStore = new SaveStore(SaveStore.DefaultFolder());
        var saved = SaveStore.Load();
        Settings = saved.Settings ?? new Settings();
        HighScores = HighScoreTable.FromSaveData(saved);
    }

    public SpriteBatch SpriteBatch { get; private set; }
    public GameAssets Assets { get; private set; }

    /// <summary>Services supplied by the Android or iOS project.</summary>
    public IPlatformServices Platform { get; }

    /// <summary>Converts between pixels and the virtual units our 2D layouts use.</summary>
    public ScreenScaler Scaler { get; } = new();

    public GameOrientation Orientation { get; private set; } = GameOrientation.Portrait;

    /// <summary>The size of the back buffer in pixels.</summary>
    public Point ScreenSize => new(GraphicsDevice.PresentationParameters.BackBufferWidth,
        GraphicsDevice.PresentationParameters.BackBufferHeight);

    public InputManager Input { get; } = new();
    public ScreenManager Screens { get; private set; }
    public Settings Settings { get; private set; }
    public AudioManager Audio { get; private set; }
    public HighScoreTable HighScores { get; private set; }
    public SaveStore SaveStore { get; }
    public FrameCounter FrameCounter { get; } = new();

    protected override void Initialize()
    {
        // Lets a mouse act as a finger when the game is run on a desktop for testing.
        TouchPanel.EnableMouseTouchPoint = true;
        ApplyFrameRate();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        SpriteBatch = new SpriteBatch(GraphicsDevice);
        Assets = new GameAssets();
        Assets.Load(Content, GraphicsDevice);

        SetOrientation(GameOrientation.Portrait);
        UpdateScaler();

        // Until the player chooses, pick a quality level that suits this device's screen.
        if (!Settings.QualityChosen)
            Settings.Quality = QualityProfile.Recommended(ScreenSize.X, ScreenSize.Y);

        Audio = new AudioManager(Content, Settings);

        Screens = new ScreenManager(this);
        Screens.SwitchTo(new TitleScreen());
    }

    protected override void Update(GameTime gameTime)
    {
        float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Input.Update(deltaSeconds, Scaler, Platform.ConsumeBackRequest());
        Audio.Update(deltaSeconds);
        Screens.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        Screens.Draw(gameTime);

        FrameCounter.FrameDrawn(gameTime);
        if (Settings.ShowFps)
        {
            SpriteBatch.Begin(transformMatrix: Scaler.Transform);
            FrameCounter.Draw(SpriteBatch, Assets.Fonts, Scaler.SafeArea);
            SpriteBatch.End();
        }

        base.Draw(gameTime);
    }

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

    /// <summary>60 frames per second normally, or 30 to save battery.</summary>
    public void ApplyFrameRate()
    {
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / (Settings.BatterySaver ? 30.0 : 60.0));
    }
}
