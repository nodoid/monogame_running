using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonsterMaze.UI;

namespace MonsterMaze.Input;

/// <summary>A completed tap: where the finger went down and where it lifted, in virtual units.</summary>
public readonly record struct Tap(Vector2 Start, Vector2 End);

/// <summary>
/// Reads touch, keyboard and gamepad once per frame and turns raw touches into taps
/// (and, from Chapter 14, swipes and holds). All positions are in virtual units.
/// </summary>
public sealed class InputManager
{
    /// <summary>A finger can drift this far (virtual units) and still count as a tap.</summary>
    public const float TapSlop = 40f;

    public const float TapMaxSeconds = 0.45f;

    private struct TrackedTouch
    {
        public Vector2 Start;
        public float Age;
        public bool Moved;
    }

    private readonly Dictionary<int, TrackedTouch> _tracked = new();
    private readonly List<Tap> _taps = new();
    private readonly List<Vector2> _active = new();
    private bool _backPressed;
    private float _sinceBack = 1f;

    public KeyboardState Keyboard { get; private set; }
    public KeyboardState PreviousKeyboard { get; private set; }
    public GamePadState GamePad { get; private set; }
    public GamePadState PreviousGamePad { get; private set; }

    /// <summary>Taps completed this frame.</summary>
    public IReadOnlyList<Tap> Taps => _taps;

    /// <summary>Positions of every finger currently on the screen.</summary>
    public IReadOnlyList<Vector2> ActiveTouches => _active;

    /// <param name="systemBack">True if the operating system reported a back button or gesture.</param>
    public void Update(float deltaSeconds, ScreenScaler scaler, bool systemBack)
    {
        // Android can report one press of back twice (as a gesture and as a key), so ignore a
        // second "back" that follows the first too quickly to be deliberate.
        _sinceBack += deltaSeconds;
        PreviousKeyboard = Keyboard;
        Keyboard = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        PreviousGamePad = GamePad;
        GamePad = Microsoft.Xna.Framework.Input.GamePad.GetState(PlayerIndex.One);

        _taps.Clear();
        _active.Clear();

        TouchCollection touches = TouchPanel.GetState();
        foreach (TouchLocation touch in touches)
        {
            Vector2 position = scaler.ToVirtual(touch.Position);

            if (touch.State == TouchLocationState.Pressed || !_tracked.TryGetValue(touch.Id, out TrackedTouch tracked))
                tracked = new TrackedTouch { Start = position };

            tracked.Age += deltaSeconds;
            if (Vector2.Distance(position, tracked.Start) > TapSlop)
                tracked.Moved = true;

            if (touch.State == TouchLocationState.Released || touch.State == TouchLocationState.Invalid)
            {
                if (!tracked.Moved && tracked.Age <= TapMaxSeconds)
                    _taps.Add(new Tap(tracked.Start, position));
                _tracked.Remove(touch.Id);
            }
            else
            {
                _tracked[touch.Id] = tracked;
                _active.Add(position);
            }
        }

        // The OS can drop touches without a release (for example while the screen rotates).
        if (touches.Count == 0)
            _tracked.Clear();

        bool back = systemBack || IsKeyPressed(Keys.Escape) || IsButtonPressed(Buttons.Back);
        _backPressed = back && _sinceBack > 0.3f;
        if (back)
            _sinceBack = 0f;
    }

    public bool WasTapped(Rectangle area)
    {
        foreach (var tap in _taps)
        {
            if (area.Contains(tap.Start) && area.Contains(tap.End))
                return true;
        }

        return false;
    }

    public bool IsTouching(Rectangle area)
    {
        foreach (var position in _active)
        {
            if (area.Contains(position))
                return true;
        }

        return false;
    }

    /// <summary>Stops anything else this frame from reacting to the same taps.</summary>
    public void ConsumeTaps() => _taps.Clear();

    public bool AnyTap => _taps.Count > 0;

    public bool IsKeyDown(Keys key) => Keyboard.IsKeyDown(key);

    public bool IsKeyPressed(Keys key) => Keyboard.IsKeyDown(key) && PreviousKeyboard.IsKeyUp(key);

    public bool IsButtonPressed(Buttons button) => GamePad.IsButtonDown(button) && PreviousGamePad.IsButtonUp(button);

    /// <summary>The Android back button or gesture, Escape on a keyboard, or Back on a gamepad.</summary>
    public bool BackPressed => _backPressed;

    public bool ConfirmPressed => IsKeyPressed(Keys.Enter) || IsKeyPressed(Keys.Space) || IsButtonPressed(Buttons.A);
}
