using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.UI;

namespace MonsterMaze.Diagnostics;

/// <summary>
/// Measures real frames per second and counts garbage collections. A stutter on a phone is
/// very often the garbage collector, so watching the GC count while playing is revealing.
/// </summary>
public sealed class FrameCounter
{
    private readonly StringBuilder _text = new(48);
    private long _lastTimestamp = Stopwatch.GetTimestamp();
    private float _elapsed;
    private int _frames;
    private int _gcAtStart = GC.CollectionCount(0);

    public float FramesPerSecond { get; private set; }

    /// <summary>Call once at the end of every Draw.</summary>
    public void FrameDrawn(GameTime gameTime)
    {
        long now = Stopwatch.GetTimestamp();
        _elapsed += (float)(now - _lastTimestamp) / Stopwatch.Frequency;
        _lastTimestamp = now;
        _frames++;

        if (_elapsed < 0.5f)
            return;

        FramesPerSecond = _frames / _elapsed;
        _frames = 0;
        _elapsed = 0f;

        // Rebuild the text in place: no new strings, so the counter itself makes no garbage.
        _text.Clear();
        _text.Append("FPS ").Append((int)MathF.Round(FramesPerSecond));
        _text.Append("   GC ").Append(GC.CollectionCount(0) - _gcAtStart);
    }

    public void Draw(SpriteBatch spriteBatch, FontSet fonts, Rectangle safeArea)
    {
        var position = new Vector2(safeArea.Center.X, safeArea.Bottom - 30f);
        Color color = FramesPerSecond >= 55f ? Color.LimeGreen : FramesPerSecond >= 28f ? Color.Yellow : Color.Red;
        fonts.DrawShadowed(spriteBatch, _text, position, 40f, color);
    }
}
