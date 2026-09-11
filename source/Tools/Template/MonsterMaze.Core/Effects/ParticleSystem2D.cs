// @since 32
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonsterMaze.Effects;

/// <summary>One particle: a sprite that moves, grows or shrinks and fades over its life.</summary>
public struct Particle2D
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Acceleration;
    public float Age;
    public float Life;
    public float StartSize;
    public float EndSize;
    public float Rotation;
    public float Spin;
    public Color StartColor;
    public Color EndColor;
}

/// <summary>
/// A simple, garbage-free particle system for 2D screens: sparks on the high score table, embers
/// on the title, sparkles when you escape. Particles live in a fixed array; a dead particle is
/// replaced by the last live one, so there is never any shuffling or allocation.
/// </summary>
public sealed class ParticleSystem2D
{
    private readonly Particle2D[] _particles;
    private int _count;

    public ParticleSystem2D(int capacity)
    {
        _particles = new Particle2D[capacity];
    }

    public Random Random { get; } = new();

    public int Count => _count;

    public void Emit(in Particle2D particle)
    {
        if (_count < _particles.Length)
            _particles[_count++] = particle;
    }

    public void Emit(Vector2 position, Vector2 velocity, float life, float startSize, float endSize,
        Color startColor, Color endColor, Vector2 acceleration = default, float spin = 0f)
    {
        Emit(new Particle2D
        {
            Position = position,
            Velocity = velocity,
            Acceleration = acceleration,
            Life = life,
            StartSize = startSize,
            EndSize = endSize,
            StartColor = startColor,
            EndColor = endColor,
            Rotation = (float)Random.NextDouble() * MathHelper.TwoPi,
            Spin = spin
        });
    }

    /// <summary>A random point within a radius, handy for scattering emitters.</summary>
    public Vector2 RandomOffset(float radius)
    {
        float angle = (float)Random.NextDouble() * MathHelper.TwoPi;
        float distance = MathF.Sqrt((float)Random.NextDouble()) * radius;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
    }

    public void Update(float deltaSeconds)
    {
        for (int i = 0; i < _count; i++)
        {
            ref Particle2D particle = ref _particles[i];
            particle.Age += deltaSeconds;
            if (particle.Age >= particle.Life)
            {
                _particles[i] = _particles[--_count];
                i--;
                continue;
            }

            particle.Velocity += particle.Acceleration * deltaSeconds;
            particle.Position += particle.Velocity * deltaSeconds;
            particle.Rotation += particle.Spin * deltaSeconds;
        }
    }

    /// <summary>Draws every particle. Call inside a SpriteBatch, usually with additive blending.</summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D texture)
    {
        var origin = new Vector2(texture.Width, texture.Height) / 2f;
        for (int i = 0; i < _count; i++)
        {
            ref Particle2D particle = ref _particles[i];
            float t = particle.Age / particle.Life;
            float size = MathHelper.Lerp(particle.StartSize, particle.EndSize, t);
            Color color = Color.Lerp(particle.StartColor, particle.EndColor, t);
            spriteBatch.Draw(texture, particle.Position, null, color, particle.Rotation, origin, size / texture.Width,
                SpriteEffects.None, 0f);
        }
    }

    public void Clear() => _count = 0;
}
