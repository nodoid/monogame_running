using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.Input;

namespace MonsterMaze.UI;

/// <summary>
/// A big, touch-friendly button. It fires when a finger is lifted inside it, which lets
/// players slide off a button to cancel, just like the buttons in the operating system.
/// </summary>
public sealed class Button
{
    private float _press;

    public Button(string text, int icon = -1)
    {
        Text = text;
        Icon = icon;
    }

    public Rectangle Bounds { get; set; }
    public string Text { get; set; }
    public int Icon { get; set; }
    public Color Color { get; set; } = Palette.Button;
    public float TextSize { get; set; } = 64f;
    public bool Visible { get; set; } = true;
    public bool IsPressed { get; private set; }

    /// <summary>Raised whenever any button is tapped, so the audio system can play a click.</summary>
    public static event Action AnyTapped;

    /// <summary>Returns true on the frame the button is tapped.</summary>
    public bool Update(InputManager input, float deltaSeconds)
    {
        if (!Visible)
        {
            IsPressed = false;
            return false;
        }

        IsPressed = input.IsTouching(Bounds);
        _press = MathHelper.Clamp(_press + (IsPressed ? deltaSeconds : -deltaSeconds) * 10f, 0f, 1f);

        if (!input.WasTapped(Bounds))
            return false;
        AnyTapped?.Invoke();
        return true;
    }

    public void Draw(SpriteBatch spriteBatch, GameAssets assets, float alpha = 1f)
    {
        if (!Visible)
            return;

        // Shrink slightly while pressed so the player can feel the button respond.
        float scale = 1f - 0.05f * _press;
        int width = (int)(Bounds.Width * scale);
        int height = (int)(Bounds.Height * scale);
        var rectangle = new Rectangle(Bounds.Center.X - width / 2, Bounds.Center.Y - height / 2, width, height);

        Color tint = Color.Lerp(Color, Color.White, 0.25f * _press);
        NineSlice.Draw(spriteBatch, assets.Button, rectangle, 48, Math.Min(40, height / 2), tint * alpha);

        bool hasText = !string.IsNullOrEmpty(Text);
        if (Icon >= 0)
        {
            float iconSize = Math.Min(rectangle.Height * 0.6f, 96f);
            Vector2 iconCentre = hasText
                ? new Vector2(rectangle.X + rectangle.Height * 0.6f, rectangle.Center.Y)
                : rectangle.Center.ToVector2();
            spriteBatch.Draw(assets.Icons, iconCentre, Icons.Source(Icon), Color.White * alpha, 0f,
                new Vector2(Icons.CellSize / 2f), iconSize / Icons.CellSize, SpriteEffects.None, 0f);
        }

        if (hasText)
        {
            Vector2 textCentre = rectangle.Center.ToVector2();
            if (Icon >= 0)
                textCentre.X += rectangle.Height * 0.3f;
            assets.Fonts.DrawShadowed(spriteBatch, Text, textCentre, TextSize * scale, Palette.Text * alpha);
        }
    }
}
