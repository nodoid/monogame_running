// @since 03
using System;
using Microsoft.Xna.Framework;
using MonsterMaze.Platform;

namespace MonsterMaze.UI;

/// <summary>
/// Lets us lay out every 2D screen in "virtual" units instead of pixels. The short side of the
/// screen is always 1080 virtual units, so a portrait phone is 1080 wide and a landscape phone
/// is 1080 high. The long side varies with the device's aspect ratio.
/// </summary>
public sealed class ScreenScaler
{
    public const float ReferenceShortSide = 1080f;

    /// <summary>Extra breathing room kept inside the safe area, in virtual units.</summary>
    private const float ComfortMargin = 24f;

    /// <summary>How many pixels one virtual unit covers.</summary>
    public float Scale { get; private set; } = 1f;

    /// <summary>The size of the screen in virtual units.</summary>
    public Vector2 VirtualSize { get; private set; } = new(ReferenceShortSide, 1920f);

    /// <summary>The size of the screen in pixels.</summary>
    public Point PixelSize { get; private set; }

    /// <summary>Pass this to SpriteBatch.Begin to draw in virtual units.</summary>
    public Matrix Transform { get; private set; } = Matrix.Identity;

    /// <summary>The part of the screen that is safe to use, in virtual units.</summary>
    public Rectangle SafeArea { get; private set; }

    public bool IsLandscape => PixelSize.X > PixelSize.Y;

    public void Update(int pixelWidth, int pixelHeight, Insets insetsInPixels)
    {
        PixelSize = new Point(pixelWidth, pixelHeight);
        Scale = Math.Max(1, Math.Min(pixelWidth, pixelHeight)) / ReferenceShortSide;
        VirtualSize = new Vector2(pixelWidth / Scale, pixelHeight / Scale);
        Transform = Matrix.CreateScale(Scale, Scale, 1f);

        float left = insetsInPixels.Left / Scale + ComfortMargin;
        float top = insetsInPixels.Top / Scale + ComfortMargin;
        float right = insetsInPixels.Right / Scale + ComfortMargin;
        float bottom = insetsInPixels.Bottom / Scale + ComfortMargin;
        SafeArea = new Rectangle(
            (int)left, (int)top,
            (int)(VirtualSize.X - left - right), (int)(VirtualSize.Y - top - bottom));
    }

    /// <summary>Converts a position in pixels (such as a touch) into virtual units.</summary>
    public Vector2 ToVirtual(Vector2 pixels) => pixels / Scale;
}
