// @since 14
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonsterMaze.Gameplay;
using MonsterMaze.UI;

namespace MonsterMaze.Input;

public enum ControlScheme
{
    /// <summary>Swipe to move and turn; tap or hold to walk forward. Works one-handed.</summary>
    Swipe,

    /// <summary>Four on-screen arrow buttons under the thumbs.</summary>
    Buttons
}

/// <summary>
/// Turns touches (and, for testing, the keyboard) into player actions. Holding a finger down,
/// or holding a button, keeps the player moving, which matters when Rex is on your tail.
/// </summary>
public sealed class TouchControls
{
    private const int ButtonSize = 200;

    private readonly Button _forward = new(null, Icons.Up);
    private readonly Button _back = new(null, Icons.Down);
    private readonly Button _turnLeft = new(null, Icons.TurnLeft);
    private readonly Button _turnRight = new(null, Icons.TurnRight);
    private float _hintTime;

    public ControlScheme Scheme { get; set; } = ControlScheme.Swipe;

    public void Layout(Rectangle safeArea)
    {
        const int gap = 24;
        int bottom = safeArea.Bottom - ButtonSize;

        // Turning under the left thumb, walking under the right.
        _turnLeft.Bounds = new Rectangle(safeArea.Left, bottom, ButtonSize, ButtonSize);
        _turnRight.Bounds = new Rectangle(safeArea.Left + ButtonSize + gap, bottom, ButtonSize, ButtonSize);
        _forward.Bounds = new Rectangle(safeArea.Right - ButtonSize, bottom - ButtonSize - gap, ButtonSize, ButtonSize);
        _back.Bounds = new Rectangle(safeArea.Right - ButtonSize, bottom, ButtonSize, ButtonSize);

        foreach (var button in new[] { _forward, _back, _turnLeft, _turnRight })
            button.Color = Color.White * 0.25f;
    }

    /// <summary>Works out what the player wants to do this frame.</summary>
    public PlayerAction Poll(InputManager input, float deltaSeconds)
    {
        _hintTime += deltaSeconds;

        // Keyboard, for testing on a desktop or with a Bluetooth keyboard.
        if (input.IsKeyDown(Keys.Up) || input.IsKeyDown(Keys.W))
            return PlayerAction.Forward;
        if (input.IsKeyDown(Keys.Down) || input.IsKeyDown(Keys.S))
            return PlayerAction.Back;
        if (input.IsKeyDown(Keys.Left) || input.IsKeyDown(Keys.A))
            return PlayerAction.TurnLeft;
        if (input.IsKeyDown(Keys.Right) || input.IsKeyDown(Keys.D))
            return PlayerAction.TurnRight;

        if (Scheme == ControlScheme.Buttons)
        {
            _forward.Update(input, deltaSeconds);
            _back.Update(input, deltaSeconds);
            _turnLeft.Update(input, deltaSeconds);
            _turnRight.Update(input, deltaSeconds);

            // Buttons act while held, so keeping a thumb down keeps walking.
            if (_forward.IsPressed)
                return PlayerAction.Forward;
            if (_back.IsPressed)
                return PlayerAction.Back;
            if (_turnLeft.IsPressed)
                return PlayerAction.TurnLeft;
            if (_turnRight.IsPressed)
                return PlayerAction.TurnRight;
            return PlayerAction.None;
        }

        if (input.Swipes.Count > 0)
        {
            return input.Swipes[0] switch
            {
                SwipeDirection.Up => PlayerAction.Forward,
                SwipeDirection.Down => PlayerAction.Back,
                SwipeDirection.Left => PlayerAction.TurnLeft,
                _ => PlayerAction.TurnRight
            };
        }

        if (input.IsHolding || input.AnyTap)
            return PlayerAction.Forward;

        return PlayerAction.None;
    }

    public void Draw(SpriteBatch spriteBatch, GameAssets assets, Vector2 screenSize)
    {
        if (Scheme == ControlScheme.Buttons)
        {
            _forward.Draw(spriteBatch, assets);
            _back.Draw(spriteBatch, assets);
            _turnLeft.Draw(spriteBatch, assets);
            _turnRight.Draw(spriteBatch, assets);
            return;
        }

        // For the first few seconds, remind the player how the swipe controls work.
        float alpha = MathHelper.Clamp(1f - (_hintTime - 4f) / 1.5f, 0f, 1f);
        if (alpha > 0f)
        {
            assets.Fonts.DrawShadowed(spriteBatch, "SWIPE TO TURN  -  TAP OR HOLD TO WALK",
                new Vector2(screenSize.X / 2f, screenSize.Y * 0.8f), 48f, Color.White * (alpha * 0.85f));
        }
    }
}
