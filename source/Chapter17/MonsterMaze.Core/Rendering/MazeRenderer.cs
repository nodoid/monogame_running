using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.Mazes;

namespace MonsterMaze.Rendering;

/// <summary>Draws the maze mesh from the camera's point of view using MonoGame's BasicEffect.</summary>
public sealed class MazeRenderer : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly BasicEffect _effect;
    private readonly GameAssets _assets;
    private MazeMesh _mesh;

    public MazeRenderer(GraphicsDevice device, GameAssets assets)
    {
        _device = device;
        _assets = assets;
        _effect = new BasicEffect(device)
        {
            VertexColorEnabled = true,
            LightingEnabled = false,
            TextureEnabled = true,
            FogEnabled = true
        };
    }

    /// <summary>Distance at which the darkness begins to swallow the walls.</summary>
    public float FogStart { get; set; } = 1.2f;

    /// <summary>Distance beyond which everything is lost in darkness.</summary>
    public float FogEnd { get; set; } = 11f;

    public Color FogColor { get; set; } = new(5, 3, 9);

    /// <summary>The warm colour of the player's torch.</summary>
    public Vector3 TorchColor { get; set; } = new(1f, 0.9f, 0.78f);

    /// <summary>Overall light level; the escape sequence turns this up.</summary>
    public float Brightness { get; set; } = 1f;

    public QualityProfile Quality { get; set; } = QualityProfile.High;

    public MazeMesh Mesh => _mesh;

    /// <summary>Builds the 3D mesh for a new maze.</summary>
    public void SetMaze(Maze maze)
    {
        _mesh?.Dispose();
        _mesh = MazeMeshBuilder.Build(_device, maze);
    }

    public void Draw(Camera camera, float seconds)
    {
        if (_mesh == null)
            return;

        _device.BlendState = BlendState.Opaque;
        _device.DepthStencilState = DepthStencilState.Default;
        _device.RasterizerState = RasterizerState.CullCounterClockwise;
        _device.SamplerStates[0] = Quality.WallSampler;

        _effect.World = Matrix.Identity;
        _effect.View = camera.View;
        _effect.Projection = camera.Projection;

        // A slight, irregular flicker makes the light feel like a flame rather than a bulb.
        float flicker = 0.93f + 0.04f * MathF.Sin(seconds * 7.3f) + 0.03f * MathF.Sin(seconds * 13.1f + 1.7f);
        Vector3 light = TorchColor * flicker * Brightness;
        _effect.FogStart = FogStart;
        _effect.FogEnd = FogEnd;
        _effect.FogColor = FogColor.ToVector3();

        _mesh.Bind(_device);
        for (int i = 0; i < _mesh.Batches.Count; i++)
        {
            MazeMesh.Batch batch = _mesh.Batches[i];
            _effect.Texture = TextureFor(batch.Surface);
            _effect.DiffuseColor = light;
            _effect.FogEnabled = true;

            // The light beyond the exit glows through the darkness, so it ignores fog and the torch.
            if (batch.Surface == Surface.ExitLight)
            {
                _effect.DiffuseColor = Vector3.One;
                _effect.FogEnabled = false;
            }

            _effect.CurrentTechnique.Passes[0].Apply();
            _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, batch.StartIndex, batch.TriangleCount);
        }
    }

    public void Dispose()
    {
        _mesh?.Dispose();
        _effect.Dispose();
    }

    private Texture2D TextureFor(Surface surface) => surface switch
    {
        Surface.Moss => _assets.WallMoss,
        Surface.Crystal => _assets.WallCrystal,
        Surface.Floor => _assets.Floor,
        Surface.Ceiling => _assets.Ceiling,
        Surface.ExitWall => _assets.WallExit,
        Surface.ExitLight => _assets.ExitLight,
        _ => _assets.WallStone
    };
}
