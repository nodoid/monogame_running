using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonsterMaze;

/// <summary>
/// The game itself. MonoGame calls Initialize and LoadContent once, then Update and Draw
/// every frame. Everything else (screens, input, audio, saving) hangs off this class.
/// </summary>
public class MonsterMazeGame : Game
{
    private readonly GraphicsDeviceManager _graphics;

    public MonsterMazeGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.IsFullScreen = true;
        _graphics.SynchronizeWithVerticalRetrace = true;
        _graphics.SupportedOrientations = DisplayOrientation.Portrait;
        Content.RootDirectory = "Content";
    }

    public SpriteBatch SpriteBatch { get; private set; }
    public GameAssets Assets { get; private set; }

    protected override void LoadContent()
    {
        SpriteBatch = new SpriteBatch(GraphicsDevice);
        Assets = new GameAssets();
        Assets.Load(Content, GraphicsDevice);
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

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        DrawHello(gameTime);

        base.Draw(gameTime);
    }

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
}
