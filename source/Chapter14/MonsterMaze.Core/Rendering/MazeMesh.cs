using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace MonsterMaze.Rendering;

/// <summary>The kinds of surface in the maze. Each is drawn with its own texture.</summary>
public enum Surface
{
    Stone,
    Moss,
    Crystal,
    Floor,
    Ceiling,
}

/// <summary>
/// The whole maze as one vertex buffer and one index buffer on the GPU. The triangles are sorted
/// by surface, so each surface is a single contiguous "batch" we can draw in one call.
/// </summary>
public sealed class MazeMesh : IDisposable
{
    private readonly VertexBuffer _vertices;
    private readonly IndexBuffer _indices;
    private readonly List<Batch> _batches;

    public MazeMesh(GraphicsDevice device, VertexPositionColorTexture[] vertices, short[] indices, List<Batch> batches)
    {
        _vertices = new VertexBuffer(device, VertexPositionColorTexture.VertexDeclaration, vertices.Length,
            BufferUsage.WriteOnly);
        _vertices.SetData(vertices);
        _indices = new IndexBuffer(device, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        _indices.SetData(indices);
        _batches = batches;
        TriangleCount = indices.Length / 3;
    }

    public IReadOnlyList<Batch> Batches => _batches;

    public int TriangleCount { get; }

    public void Bind(GraphicsDevice device)
    {
        device.SetVertexBuffer(_vertices);
        device.Indices = _indices;
    }

    public void Dispose()
    {
        _vertices.Dispose();
        _indices.Dispose();
    }

    /// <summary>A run of triangles that share a surface.</summary>
    public readonly record struct Batch(Surface Surface, int StartIndex, int TriangleCount);
}
