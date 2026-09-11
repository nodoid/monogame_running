// @since 04
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.Platform;
#if CH32
using MonsterMaze.UI;
#endif

namespace MonsterMaze.Screens;
#if CH32

public enum TransitionStyle
{
    /// <summary>Fade through black.</summary>
    Fade,

    /// <summary>Two black curtains sweep in from the sides, then open again.</summary>
    Wipe
}
#endif

/// <summary>
/// Owns the stack of screens. It also makes sure the device is in the right orientation before
/// a screen runs: while the screen is rotating we simply draw black.
/// </summary>
public sealed class ScreenManager
{
    /// <summary>If the device will not rotate (a desktop window, say) stop waiting after this long.</summary>
    private const float MaxRotationWaitSeconds = 1.5f;
#if CH32
    private const float TransitionSeconds = 0.3f;
#endif

    private readonly List<GameScreen> _screens = new();
    private Point _layoutSize;
    private Insets _layoutInsets;
    private float _insetCheckTimer;
    private float _rotationWait;
    private bool _waitingForRotation;
#if CH32
    private GameScreen _pendingScreen;
    private TransitionStyle _transitionStyle;
    private float _transition;
    private bool _closing;
#endif

    public ScreenManager(MonsterMazeGame game)
    {
        Game = game;
    }

    public MonsterMazeGame Game { get; }

    public GameScreen TopScreen => _screens.Count > 0 ? _screens[_screens.Count - 1] : null;

    /// <summary>Pushes a screen, usually a pop-up, on top of the current one.</summary>
    public void Add(GameScreen screen)
    {
        screen.Manager = this;
        _screens.Add(screen);
        screen.Load();
        if (!_waitingForRotation && _layoutSize != Point.Zero)
            screen.Layout();
    }

    public void Remove(GameScreen screen)
    {
        if (_screens.Remove(screen))
            screen.Unload();
    }
#if CH32

    /// <summary>Replaces every screen with a new one, with a short transition in between.</summary>
    public void SwitchTo(GameScreen screen, TransitionStyle style = TransitionStyle.Fade)
    {
        if (_screens.Count == 0)
        {
            ReplaceAll(screen);
            return;
        }

        _pendingScreen = screen;
        _transitionStyle = style;
        _closing = true;
    }
#else

    /// <summary>Replaces every screen with a new one.</summary>
    public void SwitchTo(GameScreen screen)
    {
        ReplaceAll(screen);
    }
#endif

    private void ReplaceAll(GameScreen screen)
    {
        for (int i = _screens.Count - 1; i >= 0; i--)
            _screens[i].Unload();
        _screens.Clear();
        Add(screen);
    }

    public void Update(GameTime gameTime)
    {
#if CH32
        float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_closing)
        {
            // Cover the old screen, then swap in the new one once the screen is fully covered.
            _transition += deltaSeconds / TransitionSeconds;
            if (_transition >= 1f)
            {
                _transition = 1f;
                _closing = false;
                ReplaceAll(_pendingScreen);
                _pendingScreen = null;
            }

            return;
        }

#endif
        if (!EnsureOrientation(gameTime))
            return;
#if CH32

        if (_transition > 0f)
            _transition = MathHelper.Max(0f, _transition - deltaSeconds / TransitionSeconds);
#endif

        // Only the top screen updates, so a pop-up such as the pause menu freezes the game beneath it.
        TopScreen?.Update(gameTime);
    }

    public void Draw(GameTime gameTime)
    {
        GraphicsDevice device = Game.GraphicsDevice;
        device.Clear(Color.Black);

        if (!_waitingForRotation)
        {
            // Draw from the last full-screen screen upwards, so pop-ups appear over what's beneath them.
            int first = _screens.Count - 1;
            while (first > 0 && _screens[first].IsPopup)
                first--;
            for (int i = System.Math.Max(0, first); i < _screens.Count; i++)
                _screens[i].Draw(gameTime);
        }
#if CH32

        DrawTransition();
#endif
    }

    /// <summary>
    /// Requests the orientation the top screen wants and reports whether the device has got there.
    /// When the screen size changes every screen is asked to lay itself out again.
    /// </summary>
    private bool EnsureOrientation(GameTime gameTime)
    {
        GameOrientation wanted = GameOrientation.Portrait;
        for (int i = _screens.Count - 1; i >= 0; i--)
        {
            if (!_screens[i].IsPopup)
            {
                wanted = _screens[i].Orientation;
                break;
            }
        }

        if (Game.Orientation != wanted)
        {
            Game.SetOrientation(wanted);
            _rotationWait = 0f;
        }

        Point size = Game.ScreenSize;
        bool isLandscape = size.X > size.Y;
        if (isLandscape != (wanted == GameOrientation.Landscape) && _rotationWait < MaxRotationWaitSeconds)
        {
            _rotationWait += (float)gameTime.ElapsedGameTime.TotalSeconds;
            _waitingForRotation = true;
            return false;
        }

        _waitingForRotation = false;

        // The notch and rounded corners move when the screen rotates, and the system only reports
        // the new safe area once the rotation animation has finished, so keep checking.
        Insets insets = _layoutInsets;
        _insetCheckTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_insetCheckTimer <= 0f)
        {
            _insetCheckTimer = 0.25f;
            insets = Game.Platform.GetSafeAreaInsets();
        }

        if (size != _layoutSize || insets != _layoutInsets)
        {
            _layoutSize = size;
            _layoutInsets = insets;
            Game.UpdateScaler();
            for (int i = 0; i < _screens.Count; i++)
                _screens[i].Layout();
        }

        return true;
    }
#if CH32

    private void DrawTransition()
    {
        float amount = _waitingForRotation ? 1f : _transition;
        if (amount <= 0f)
            return;

        // Ease in and out so the movement feels smooth rather than mechanical.
        float eased = amount * amount * (3f - 2f * amount);
        SpriteBatch spriteBatch = Game.SpriteBatch;
        Vector2 size = Game.Scaler.VirtualSize;

        spriteBatch.Begin(transformMatrix: Game.Scaler.Transform);
        if (_transitionStyle == TransitionStyle.Wipe && !_waitingForRotation)
        {
            float half = size.X / 2f * eased;
            spriteBatch.FillRectangle(Vector2.Zero, new Vector2(half + 1f, size.Y), Color.Black);
            spriteBatch.FillRectangle(new Vector2(size.X - half - 1f, 0f), new Vector2(half + 2f, size.Y), Color.Black);
        }
        else
        {
            spriteBatch.FillRectangle(Vector2.Zero, size, Color.Black * eased);
        }

        spriteBatch.End();
    }
#endif
}
