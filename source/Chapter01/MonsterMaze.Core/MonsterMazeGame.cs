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
        GraphicsDevice.Clear(new Color(48, 12, 24));

        base.Draw(gameTime);
    }
}
