// @since 34
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonsterMaze.Gameplay;

/// <summary>
/// The second game over: the player walks out of the maze. The camera keeps moving into the
/// light while the exposure climbs, the glow blooms and the screen fades to white.
/// </summary>
public sealed class EscapeSequence
{
    public const float Duration = 2.8f;

    public bool IsActive { get; private set; }
    public float Time { get; private set; }
    public bool IsFinished => IsActive && Time >= Duration;

    /// <summary>How bright the scene is, from 1 (normal) up to 3.5 (blinding).</summary>
    public float Exposure => 1f + 2.5f * Ease(Time / Duration);

    /// <summary>The fade to white at the end.</summary>
    public float WhiteOut => Ease((Time - 1.5f) / 1.3f);

    /// <summary>How far the camera has walked on into the light.</summary>
    public float WalkDistance => MathHelper.Min(Time, 1.6f) * 0.8f;

    public void Start()
    {
        IsActive = true;
        Time = 0f;
    }

    public void Update(float deltaSeconds)
    {
        if (IsActive)
            Time += deltaSeconds;
    }

    public void Draw(SpriteBatch spriteBatch, GameAssets assets, Vector2 screenSize)
    {
        float white = WhiteOut;
        if (white > 0f)
            spriteBatch.Draw(assets.Pixel, new Rectangle(0, 0, (int)screenSize.X + 1, (int)screenSize.Y + 1),
                new Color(255, 250, 235) * white);
    }

    private static float Ease(float t)
    {
        t = MathHelper.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
