using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.UI;

namespace MonsterMaze.Effects;

/// <summary>
/// Turns danger into things the player can feel: the screen shakes as Rex's feet land, the edges
/// pulse red with the heartbeat, and the view flashes, blurs and splits when he charges.
/// </summary>
public sealed class DangerEffects
{
    private readonly Random _random = new();
    private float _shake;
    private float _flash;
    private float _time;

    /// <summary>Tones down flashes and pulsing for players sensitive to them.</summary>
    public bool ReduceFlashing { get; set; }

    /// <summary>Add this to the camera position.</summary>
    public Vector3 ShakeOffset { get; private set; }

    /// <summary>Add this to the camera roll.</summary>
    public float ShakeRoll { get; private set; }

    public Color FlashColor { get; private set; } = Color.White;
    public float FlashAmount => ReduceFlashing ? _flash * 0.3f : _flash;

    /// <summary>A 0-1 pulse in time with the (imagined) heartbeat.</summary>
    public float Pulse { get; private set; }

    public void Shake(float amount) => _shake = MathF.Max(_shake, MathHelper.Clamp(amount, 0f, 1f));

    public void Flash(Color color, float amount)
    {
        FlashColor = color;
        _flash = MathF.Max(_flash, amount);
    }

    public void Update(float deltaSeconds, float danger, bool charging, PostProcessor post
        , float brightness
    )
    {
        _time += deltaSeconds;
        _shake = MathF.Max(0f, _shake - deltaSeconds * 2.2f);
        _flash = MathF.Max(0f, _flash - deltaSeconds * 3f);

        // Squaring the shake makes big jolts dramatic while small ones stay subtle.
        float strength = _shake * _shake;
        ShakeOffset = new Vector3(Random(), Random() * 0.6f, Random()) * 0.07f * strength;
        ShakeRoll = Random() * 0.035f * strength;

        float beatsPerSecond = MathHelper.Lerp(1f, 2.6f, danger);
        Pulse = MathF.Pow(0.5f + 0.5f * MathF.Sin(_time * MathHelper.TwoPi * beatsPerSecond), 4f);

        // The vignette closes in and turns blood red as Rex gets near.
        float red = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((danger - 0.35f) / 0.65f, 0f, 1f));
        float pulse = ReduceFlashing ? 0f : Pulse * 0.12f * red;
        post.VignetteIntensity = MathHelper.Lerp(0.45f, 0.9f, danger) + pulse;
        post.VignetteColor = Color.Lerp(Color.Black, new Color(140, 0, 0), red);

        // Panic: when he charges, the picture splits and smears.
        float calm = ReduceFlashing ? 0.4f : 1f;
        post.ChromaticAberration = (charging ? 0.012f : 0.004f * red) * calm;
        post.BlurAmount = charging ? (0.22f + 0.1f * Pulse) * calm : 0f;

        // Colour grading: the maze drains of colour and warms towards red as danger rises.
        post.Saturation = MathHelper.Lerp(1.1f, 0.6f, danger);
        post.Tint = Vector3.Lerp(new Vector3(1f, 0.98f, 0.96f), new Vector3(1.12f, 0.86f, 0.84f), red);
        post.Contrast = 1f + 0.18f * danger;
        post.Brightness = brightness;
    }

    /// <summary>Draws the flash as a full-screen colour (inside an open SpriteBatch).</summary>
    public void DrawFlash(SpriteBatch spriteBatch, Vector2 screenSize)
    {
        if (FlashAmount > 0.01f)
            spriteBatch.FillRectangle(Vector2.Zero, screenSize, FlashColor * FlashAmount);
    }

    private float Random() => (float)_random.NextDouble() * 2f - 1f;
}
