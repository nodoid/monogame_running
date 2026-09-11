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
    private MazeMesh _mesh;

    public MazeRenderer(GraphicsDevice device, GameAssets assets)
    {
        _device = device;
        _effect = new BasicEffect(device)
        {
            VertexColorEnabled = true,
            LightingEnabled = false,
        };
    }

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

        _effect.World = Matrix.Identity;
        _effect.View = camera.View;
        _effect.Projection = camera.Projection;

        _mesh.Bind(_device);
        for (int i = 0; i < _mesh.Batches.Count; i++)
        {
            MazeMesh.Batch batch = _mesh.Batches[i];

            _effect.CurrentTechnique.Passes[0].Apply();
            _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, batch.StartIndex, batch.TriangleCount);
        }
    }

    public void Dispose()
    {
        _mesh?.Dispose();
        _effect.Dispose();
    }
}
