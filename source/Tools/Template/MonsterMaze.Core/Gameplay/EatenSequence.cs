// @since 33
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonsterMaze.Gameplay;

/// <summary>
/// The first game over: Rex catches the player. Control is taken away, the view snaps round to
/// face him, he lunges, his jaws close over the screen and everything fades to red.
/// </summary>
public sealed class EatenSequence
{
    public const float Duration = 2.6f;

    /// <summary>The moment the jaws snap shut, for the crunch sound and the big shake.</summary>
    public const float ChompTime = 0.8f;

    public bool IsActive { get; private set; }
    public float Time { get; private set; }
    public bool IsFinished => IsActive && Time >= Duration;

    /// <summary>How far the camera has turned to face Rex (0 to 1).</summary>
    public float TurnAmount => MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(Time / 0.35f, 0f, 1f));

    /// <summary>How closed the jaws are (0 open, 1 shut).</summary>
    public float JawsClosed => Ease((Time - 0.45f) / 0.4f);

    /// <summary>The fade to darkness at the end.</summary>
    public float Darkness => Ease((Time - 1.3f) / 1.1f);

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
        float closed = JawsClosed;
        if (closed > 0f)
        {
            // Scale the jaws to the screen width. Each slides in until its teeth meet in the middle.
            float scale = screenSize.X / assets.JawsTop.Width;
            float height = assets.JawsTop.Height * scale;
            float middle = screenSize.Y / 2f;

            float topY = MathHelper.Lerp(-height, middle - height * 0.9f, closed);
            float bottomY = MathHelper.Lerp(screenSize.Y, middle - height * 0.1f, closed);
            spriteBatch.Draw(assets.JawsTop, new Vector2(0f, topY), null, Color.White, 0f, Vector2.Zero, scale,
                SpriteEffects.None, 0f);
            spriteBatch.Draw(assets.JawsBottom, new Vector2(0f, bottomY), null, Color.White, 0f, Vector2.Zero, scale,
                SpriteEffects.None, 0f);
        }

        float darkness = Darkness;
        if (darkness > 0f)
            spriteBatch.Draw(assets.Pixel, new Rectangle(0, 0, (int)screenSize.X + 1, (int)screenSize.Y + 1),
                Color.Lerp(new Color(90, 0, 0), Color.Black, darkness) * darkness);
    }

    private static float Ease(float t)
    {
        t = MathHelper.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
