using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonsterMaze.Platform;
using MonsterMaze.UI;

namespace MonsterMaze;

/// <summary>
/// The game itself. MonoGame calls Initialize and LoadContent once, then Update and Draw
/// every frame. Everything else (screens, input, audio, saving) hangs off this class.
/// </summary>
public class MonsterMazeGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private Texture2D _pixel;

    public MonsterMazeGame(IPlatformServices platform = null)
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.IsFullScreen = true;
        _graphics.SynchronizeWithVerticalRetrace = true;
        Content.RootDirectory = "Content";
        Platform = platform ?? new NullPlatformServices();
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

    protected override void LoadContent()
    {
        SpriteBatch = new SpriteBatch(GraphicsDevice);
        Assets = new GameAssets();
        Assets.Load(Content, GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        SetOrientation(GameOrientation.Portrait);
        UpdateScaler();
    }

    protected override void Update(GameTime gameTime)
    {
        // Android's back button arrives as the gamepad's Back button. iOS apps never quit themselves.
        if (!OperatingSystem.IsIOS() &&
            (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
             Keyboard.GetState().IsKeyDown(Keys.Escape)))
        {
            Exit();
        }

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

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        DrawPlatformInfo(gameTime);

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
}
