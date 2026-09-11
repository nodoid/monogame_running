using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonsterMaze.Input;
using MonsterMaze.UI;

namespace MonsterMaze.Diagnostics;

/// <summary>
/// A developer's panel of information drawn over the game. Toggle it with a three-finger tap
/// (or M on a keyboard). Nothing here is meant for players.
/// </summary>
public sealed class DebugOverlay
{
    private readonly List<string> _lines = new();
    private bool _wasMultiTouch;

    public bool Visible { get; set; }

    /// <summary>Checks for the toggle gesture. Returns true on the frame the overlay is toggled.</summary>
    public bool Update(InputManager input)
    {
        bool multiTouch = input.ActiveTouches.Count >= 3;
        bool toggled = (multiTouch && !_wasMultiTouch) || input.IsKeyPressed(Keys.M) || input.IsKeyPressed(Keys.F1);
        _wasMultiTouch = multiTouch;

        if (toggled)
            Visible = !Visible;
        return toggled;
    }

    public void Clear() => _lines.Clear();

    public void Add(string line) => _lines.Add(line);

    public void Draw(SpriteBatch spriteBatch, FontSet fonts, Rectangle safeArea)
    {
        if (!Visible || _lines.Count == 0)
            return;

        const float lineHeight = 44f;
        var panel = new Rectangle(safeArea.X, safeArea.Bottom - (int)(lineHeight * _lines.Count) - 24, 760,
            (int)(lineHeight * _lines.Count) + 16);
        spriteBatch.FillRectangle(panel, Color.Black * 0.6f);

        float y = panel.Y + 8 + lineHeight / 2f;
        foreach (string line in _lines)
        {
            fonts.Draw(spriteBatch, line, new Vector2(panel.X + 16, y), 38f, new Color(150, 255, 150), TextAlign.Left);
            y += lineHeight;
        }
    }
}
