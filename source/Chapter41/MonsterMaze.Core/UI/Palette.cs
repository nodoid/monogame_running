using System;
using Microsoft.Xna.Framework;

namespace MonsterMaze.UI;

/// <summary>The game's colour scheme, kept in one place so every screen feels consistent.</summary>
public static class Palette
{
    public static readonly Color Background = new(12, 6, 18);
    public static readonly Color Text = new(244, 238, 226);
    public static readonly Color TextMuted = new(170, 160, 185);
    public static readonly Color Accent = new(130, 230, 80);
    public static readonly Color Gold = new(255, 206, 64);
    public static readonly Color Warning = new(255, 170, 40);
    public static readonly Color Danger = new(240, 52, 44);
    public static readonly Color Button = new(52, 150, 120);
    public static readonly Color ButtonSecondary = new(88, 72, 140);
    public static readonly Color Panel = new(30, 20, 44);
    public static readonly Color Easy = new(90, 214, 120);
    public static readonly Color Normal = new(252, 190, 50);
    public static readonly Color Hard = new(242, 72, 62);

    /// <summary>Converts hue (0-1, wrapping), saturation and value to a colour.</summary>
    public static Color FromHsv(float hue, float saturation, float value, float alpha = 1f)
    {
        hue -= MathF.Floor(hue);
        float h = hue * 6f;
        int sector = (int)h;
        float f = h - sector;
        float p = value * (1f - saturation);
        float q = value * (1f - saturation * f);
        float t = value * (1f - saturation * (1f - f));
        var (r, g, b) = sector switch
        {
            0 => (value, t, p),
            1 => (q, value, p),
            2 => (p, value, t),
            3 => (p, q, value),
            4 => (t, p, value),
            _ => (value, p, q)
        };
        return new Color(r, g, b) * alpha;
    }

    /// <summary>A fully saturated colour that cycles through the rainbow as <paramref name="t"/> increases.</summary>
    public static Color Rainbow(float t, float alpha = 1f) => FromHsv(t, 0.85f, 1f, alpha);
}
