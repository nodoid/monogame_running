using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.Rendering;

namespace MonsterMaze.Effects;

/// <summary>
/// Dust motes drifting through the torchlight, and grit shaken from the ceiling by Rex's
/// footsteps. They are drawn as small camera-facing quads in the 3D world, so walls hide them
/// and the fog swallows them just like everything else.
/// </summary>
public sealed class DustParticles : IDisposable
{
    private const int Capacity = 400;
    private const float Radius = 3.5f;

    private struct Mote
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Age;
        public float Life;
        public float Size;
        public Color Color;
        public bool Falling;
    }

    private readonly GraphicsDevice _device;
    private readonly BasicEffect _effect;
    private readonly Mote[] _motes = new Mote[Capacity];
    private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[Capacity * 4];
    private readonly short[] _indices = new short[Capacity * 6];
    private readonly Random _random = new();
    private int _count;

    public DustParticles(GraphicsDevice device, Texture2D texture)
    {
        _device = device;
        _effect = new BasicEffect(device)
        {
            TextureEnabled = true,
            Texture = texture,
            VertexColorEnabled = true,
            LightingEnabled = false
        };

        for (int i = 0; i < Capacity; i++)
        {
            int v = i * 4;
            int n = i * 6;
            _indices[n] = (short)v;
            _indices[n + 1] = (short)(v + 1);
            _indices[n + 2] = (short)(v + 2);
            _indices[n + 3] = (short)v;
            _indices[n + 4] = (short)(v + 2);
            _indices[n + 5] = (short)(v + 3);
        }
    }

    /// <summary>How many floating motes to keep around the player (set from the quality level).</summary>
    public int FloatingMotes { get; set; } = 120;

    /// <summary>Shakes grit loose from the ceiling near a point.</summary>
    public void Burst(Vector3 near, int count)
    {
        for (int i = 0; i < count && _count < Capacity; i++)
        {
            _motes[_count++] = new Mote
            {
                Position = near + new Vector3(Random() * 1.6f, WorldSpace.WallHeight - 0.05f, Random() * 1.6f),
                Velocity = new Vector3(Random() * 0.1f, -0.3f - (float)_random.NextDouble() * 0.6f, Random() * 0.1f),
                Life = 1.5f + (float)_random.NextDouble(),
                Size = 0.015f + (float)_random.NextDouble() * 0.02f,
                Color = new Color(150, 120, 95),
                Falling = true
            };
        }
    }

    public void Update(float deltaSeconds, Vector3 centre, float time)
    {
        int floating = 0;
        for (int i = 0; i < _count; i++)
        {
            ref Mote mote = ref _motes[i];
            mote.Age += deltaSeconds;

            // Floating motes drift in lazy swirls; falling grit accelerates downwards.
            if (mote.Falling)
                mote.Velocity.Y -= 2.5f * deltaSeconds;
            else
                mote.Velocity += new Vector3(MathF.Sin(time * 0.7f + i), MathF.Cos(time * 0.5f + i * 1.3f), 0f) * 0.02f * deltaSeconds;
            mote.Position += mote.Velocity * deltaSeconds;

            bool tooFar = !mote.Falling && Vector3.DistanceSquared(mote.Position, centre) > Radius * Radius;
            if (mote.Age >= mote.Life || mote.Position.Y < 0f || tooFar)
            {
                _motes[i] = _motes[--_count];
                i--;
                continue;
            }

            if (!mote.Falling)
                floating++;
        }

        // Top up the floating dust around the player.
        for (; floating < FloatingMotes && _count < Capacity; floating++)
        {
            _motes[_count++] = new Mote
            {
                Position = centre + new Vector3(Random() * Radius, Random() * 0.9f, Random() * Radius),
                Velocity = new Vector3(Random(), Random(), Random()) * 0.05f,
                Life = 4f + (float)_random.NextDouble() * 5f,
                Size = 0.008f + (float)_random.NextDouble() * 0.012f,
                Color = new Color(255, 225, 180)
            };
        }
    }

    public void Draw(Camera camera, MazeRenderer maze)
    {
        if (_count == 0)
            return;

        // Build quads that face the camera, using the camera's own right and up directions.
        Matrix view = camera.View;
        var right = new Vector3(view.M11, view.M21, view.M31);
        var up = new Vector3(view.M12, view.M22, view.M32);

        for (int i = 0; i < _count; i++)
        {
            ref Mote mote = ref _motes[i];

            // Fade in at birth and out at death so motes never pop.
            float life = mote.Age / mote.Life;
            float fade = MathF.Min(1f, MathF.Min(life * 5f, (1f - life) * 3f));
            Color color = mote.Color * (fade * 0.8f);

            Vector3 r = right * mote.Size;
            Vector3 u = up * mote.Size;
            int v = i * 4;
            _vertices[v] = new VertexPositionColorTexture(mote.Position - r - u, color, new Vector2(0f, 1f));
            _vertices[v + 1] = new VertexPositionColorTexture(mote.Position - r + u, color, new Vector2(0f, 0f));
            _vertices[v + 2] = new VertexPositionColorTexture(mote.Position + r + u, color, new Vector2(1f, 0f));
            _vertices[v + 3] = new VertexPositionColorTexture(mote.Position + r - u, color, new Vector2(1f, 1f));
        }

        _device.BlendState = BlendState.Additive;
        _device.DepthStencilState = DepthStencilState.DepthRead;
        _device.RasterizerState = RasterizerState.CullNone;
        _device.SamplerStates[0] = SamplerState.LinearClamp;

        _effect.World = Matrix.Identity;
        _effect.View = camera.View;
        _effect.Projection = camera.Projection;
        _effect.FogEnabled = true;
        _effect.FogStart = maze.FogStart;
        _effect.FogEnd = maze.FogEnd * 0.6f;
        _effect.FogColor = Vector3.Zero;
        _effect.CurrentTechnique.Passes[0].Apply();
        _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertices, 0, _count * 4, _indices, 0, _count * 2);
    }

    public void Dispose() => _effect.Dispose();

    private float Random() => (float)_random.NextDouble() * 2f - 1f;
}
