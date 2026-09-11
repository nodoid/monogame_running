using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonsterMaze.UI;

/// <summary>
/// Simple shapes for SpriteBatch, drawn by stretching a single white pixel.
/// </summary>
public static class Draw2D
{
    /// <summary>A 1 x 1 white texture, created when the assets load.</summary>
    public static Texture2D Pixel { get; set; }

    public static void FillRectangle(this SpriteBatch spriteBatch, Rectangle rectangle, Color color)
    {
        spriteBatch.Draw(Pixel, rectangle, color);
    }

    public static void FillRectangle(this SpriteBatch spriteBatch, Vector2 position, Vector2 size, Color color)
    {
        spriteBatch.Draw(Pixel, position, null, color, 0f, Vector2.Zero, size, SpriteEffects.None, 0f);
    }

    public static void DrawRectangle(this SpriteBatch spriteBatch, Rectangle rectangle, Color color, int thickness = 2)
    {
        spriteBatch.Draw(Pixel, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
        spriteBatch.Draw(Pixel, new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
        spriteBatch.Draw(Pixel, new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
        spriteBatch.Draw(Pixel, new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
    }

    public static void DrawLine(this SpriteBatch spriteBatch, Vector2 from, Vector2 to, Color color, float thickness = 2f)
    {
        Vector2 delta = to - from;
        float angle = MathF.Atan2(delta.Y, delta.X);
        spriteBatch.Draw(Pixel, from, null, color, angle, new Vector2(0f, 0.5f), new Vector2(delta.Length(), thickness),
            SpriteEffects.None, 0f);
    }
}
