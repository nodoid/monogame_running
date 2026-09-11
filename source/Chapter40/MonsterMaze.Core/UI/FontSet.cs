using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonsterMaze.UI;

public enum TextAlign
{
    Left,
    Center,
    Right
}

/// <summary>
/// One typeface built at several sizes. When drawing, it picks the smallest font that is at
/// least as big as the text will appear on screen, so text stays sharp on every display.
/// Sizes are given as the height of a line of text in virtual units.
/// </summary>
public sealed class FontSet
{
    private readonly SpriteFont[] _fonts;

    public FontSet(params SpriteFont[] fontsSmallestFirst)
    {
        _fonts = fontsSmallestFirst;
    }

    /// <summary>Pixels per virtual unit on the current screen.</summary>
    public float ScreenScale { get; set; } = 1f;

    public SpriteFont Pick(float size, out float scale)
    {
        float pixels = size * ScreenScale;
        SpriteFont font = _fonts[_fonts.Length - 1];
        for (int i = 0; i < _fonts.Length; i++)
        {
            if (_fonts[i].LineSpacing >= pixels * 0.9f)
            {
                font = _fonts[i];
                break;
            }
        }

        scale = size / font.LineSpacing;
        return font;
    }

    public Vector2 Measure(string text, float size)
    {
        var font = Pick(size, out float scale);
        return font.MeasureString(text) * scale;
    }

    /// <summary>
    /// Draws text. The position is the vertical middle of the line, and the left edge,
    /// centre or right edge horizontally depending on <paramref name="align"/>.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, string text, Vector2 position, float size, Color color,
        TextAlign align = TextAlign.Center, float rotation = 0f)
    {
        var font = Pick(size, out float scale);
        Vector2 measured = font.MeasureString(text);
        spriteBatch.DrawString(font, text, position, color, rotation, Origin(measured, font, align), scale,
            SpriteEffects.None, 0f);
    }

    /// <summary>Draws text with a solid outline, which keeps it readable over busy backgrounds.</summary>
    public void DrawOutlined(SpriteBatch spriteBatch, string text, Vector2 position, float size, Color color,
        Color outline, float thickness = 3f, TextAlign align = TextAlign.Center)
    {
        for (int i = 0; i < 8; i++)
        {
            float angle = i * MathHelper.PiOver4;
            var offset = new Vector2(System.MathF.Cos(angle), System.MathF.Sin(angle)) * thickness;
            Draw(spriteBatch, text, position + offset, size, outline, align);
        }

        Draw(spriteBatch, text, position, size, color, align);
    }

    /// <summary>Draws text with a soft drop shadow.</summary>
    public void DrawShadowed(SpriteBatch spriteBatch, string text, Vector2 position, float size, Color color,
        TextAlign align = TextAlign.Center)
    {
        Draw(spriteBatch, text, position + new Vector2(0f, size * 0.06f), size, Color.Black * (color.A / 255f * 0.7f),
            align);
        Draw(spriteBatch, text, position, size, color, align);
    }

    /// <summary>Splits text into lines no wider than <paramref name="maxWidth"/>.</summary>
    public List<string> Wrap(string text, float size, float maxWidth)
    {
        var lines = new List<string>();
        string line = "";
        foreach (string word in text.Split(' '))
        {
            string candidate = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && Measure(candidate, size).X > maxWidth)
            {
                lines.Add(line);
                line = word;
            }
            else
            {
                line = candidate;
            }
        }

        if (line.Length > 0)
            lines.Add(line);
        return lines;
    }

    /// <summary>
    /// StringBuilder versions avoid creating a new string every frame for values that change
    /// constantly, such as the score. Fewer strings means less work for the garbage collector.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, StringBuilder text, Vector2 position, float size, Color color,
        TextAlign align = TextAlign.Center)
    {
        var font = Pick(size, out float scale);
        Vector2 measured = font.MeasureString(text);
        spriteBatch.DrawString(font, text, position, color, 0f, Origin(measured, font, align), scale,
            SpriteEffects.None, 0f);
    }

    public void DrawShadowed(SpriteBatch spriteBatch, StringBuilder text, Vector2 position, float size,
        Color color, TextAlign align = TextAlign.Center)
    {
        Draw(spriteBatch, text, position + new Vector2(0f, size * 0.06f), size, Color.Black * (color.A / 255f * 0.7f),
            align);
        Draw(spriteBatch, text, position, size, color, align);
    }

    private static Vector2 Origin(Vector2 measured, SpriteFont font, TextAlign align)
    {
        float x = align switch
        {
            TextAlign.Left => 0f,
            TextAlign.Center => measured.X / 2f,
            _ => measured.X
        };
        return new Vector2(x, font.LineSpacing / 2f);
    }
}
