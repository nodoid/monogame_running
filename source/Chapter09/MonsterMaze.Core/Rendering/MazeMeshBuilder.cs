using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.Mazes;

namespace MonsterMaze.Rendering;

/// <summary>
/// Turns the maze grid into triangles. Every standing wall becomes a quad facing into its cell,
/// and every cell gets a floor and a ceiling quad. Faces that can never be seen (the outside of
/// the outer wall, the back of a wall) are simply never built.
/// </summary>
public static class MazeMeshBuilder
{
    // Until we have textures, vertex colours give each surface its colour.
    private static readonly Color WallTint = new(120, 140, 185);
    private static readonly Color FloorTint = new(135, 95, 70);
    private static readonly Color CeilingTint = new(70, 55, 105);

    public static MazeMesh Build(GraphicsDevice device, Maze maze)
    {
        int surfaceCount = Enum.GetValues<Surface>().Length;
        var surfaces = new SurfaceBuilder[surfaceCount];
        for (int i = 0; i < surfaceCount; i++)
            surfaces[i] = new SurfaceBuilder();

        for (int y = 0; y < maze.Height; y++)
        {
            for (int x = 0; x < maze.Width; x++)
            {
                var cell = new Point(x, y);
                const float glow = 0f;

                AddFloor(surfaces[(int)Surface.Floor], maze, cell, glow);
                AddCeiling(surfaces[(int)Surface.Ceiling], maze, cell, glow);

                foreach (Direction direction in DirectionExtensions.All)
                {
                    if (maze.HasWall(cell, direction))
                        AddWall(surfaces[(int)ChooseWallSurface(maze, cell, direction)], cell, direction, glow);
                }
            }
        }

        return Combine(device, surfaces);
    }

    private static void AddWall(SurfaceBuilder builder, Point cell, Direction direction, float glow)
    {
        Vector3 forward = direction.ToVector3();
        var right = new Vector3(-forward.Z, 0f, forward.X);
        const float half = WorldSpace.CellSize / 2f;
        Vector3 wallCentre = WorldSpace.CellCentre(cell) + forward * half;

        Vector3 bottomLeft = wallCentre - right * half;
        Vector3 bottomRight = wallCentre + right * half;
        Vector3 up = Vector3.Up * WorldSpace.WallHeight;

        // North/south faces are a little brighter than east/west ones, which helps the eye read
        // the corners, and the bottom of each wall is darker, as if in the shadow of the floor.
        float facing = direction is Direction.North or Direction.South ? 1f : 0.84f;
        Color top = Shade(WallTint, facing * 0.8f, glow);
        Color bottom = Shade(WallTint, facing * 0.62f, glow);

        builder.AddQuad(bottomLeft, bottomRight, bottomRight + up, bottomLeft + up, bottom, bottom, top, top);
    }

    private static void AddFloor(SurfaceBuilder builder, Maze maze, Point cell, float glow)
    {
        GetCellBounds(cell, out float x0, out float z0, out float x1, out float z1);
        const float level = 0.92f;

        // Seen from above with north at the top of the screen.
        builder.AddQuad(
            new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1), new Vector3(x1, 0f, z0), new Vector3(x0, 0f, z0),
            Shade(FloorTint, level * CornerLight(maze, cell, Direction.South, Direction.West), glow),
            Shade(FloorTint, level * CornerLight(maze, cell, Direction.South, Direction.East), glow),
            Shade(FloorTint, level * CornerLight(maze, cell, Direction.North, Direction.East), glow),
            Shade(FloorTint, level * CornerLight(maze, cell, Direction.North, Direction.West), glow));
    }

    private static void AddCeiling(SurfaceBuilder builder, Maze maze, Point cell, float glow)
    {
        GetCellBounds(cell, out float x0, out float z0, out float x1, out float z1);
        const float height = WorldSpace.WallHeight;
        const float level = 0.72f;

        // Seen from below: north is still at the top, but east is now on the left.
        builder.AddQuad(
            new Vector3(x1, height, z1), new Vector3(x0, height, z1), new Vector3(x0, height, z0), new Vector3(x1, height, z0),
            Shade(CeilingTint, level * CornerLight(maze, cell, Direction.South, Direction.East), glow),
            Shade(CeilingTint, level * CornerLight(maze, cell, Direction.South, Direction.West), glow),
            Shade(CeilingTint, level * CornerLight(maze, cell, Direction.North, Direction.West), glow),
            Shade(CeilingTint, level * CornerLight(maze, cell, Direction.North, Direction.East), glow));
    }

    /// <summary>
    /// A cheap stand-in for ambient occlusion: corners where walls meet catch less light,
    /// so darken floor and ceiling vertices by how many walls touch them.
    /// </summary>
    private static float CornerLight(Maze maze, Point cell, Direction a, Direction b)
    {
        int walls = (maze.HasWall(cell, a) ? 1 : 0) + (maze.HasWall(cell, b) ? 1 : 0);
        return 1f - 0.17f * walls;
    }

    private static Surface ChooseWallSurface(Maze maze, Point cell, Direction direction)
    {
        return Surface.Stone;
    }

    private static Color Shade(Color tint, float light, float glow)
    {
        var color = new Color(tint.ToVector3() * light);
        return color;
    }

    private static void GetCellBounds(Point cell, out float x0, out float z0, out float x1, out float z1)
    {
        x0 = cell.X * WorldSpace.CellSize;
        z0 = cell.Y * WorldSpace.CellSize;
        x1 = x0 + WorldSpace.CellSize;
        z1 = z0 + WorldSpace.CellSize;
    }

    /// <summary>Joins every surface's triangles into one buffer, remembering where each starts.</summary>
    private static MazeMesh Combine(GraphicsDevice device, SurfaceBuilder[] surfaces)
    {
        var vertices = new List<VertexPositionColorTexture>();
        var indices = new List<short>();
        var batches = new List<MazeMesh.Batch>();

        for (int i = 0; i < surfaces.Length; i++)
        {
            SurfaceBuilder surface = surfaces[i];
            if (surface.Indices.Count == 0)
                continue;

            int baseVertex = vertices.Count;
            batches.Add(new MazeMesh.Batch((Surface)i, indices.Count, surface.Indices.Count / 3));
            vertices.AddRange(surface.Vertices);
            foreach (short index in surface.Indices)
                indices.Add((short)(index + baseVertex));
        }

        return new MazeMesh(device, vertices.ToArray(), indices.ToArray(), batches);
    }

    /// <summary>Collects the quads for one surface.</summary>
    private sealed class SurfaceBuilder
    {
        public readonly List<VertexPositionColorTexture> Vertices = new();
        public readonly List<short> Indices = new();

        /// <summary>
        /// Adds a quad given its corners as seen from the front. The two triangles wind
        /// clockwise, which is what MonoGame treats as front-facing.
        /// </summary>
        public void AddQuad(Vector3 bottomLeft, Vector3 bottomRight, Vector3 topRight, Vector3 topLeft,
            Color bottomLeftColor, Color bottomRightColor, Color topRightColor, Color topLeftColor)
        {
            var first = (short)Vertices.Count;
            Vertices.Add(new VertexPositionColorTexture(bottomLeft, bottomLeftColor, new Vector2(0f, 1f)));
            Vertices.Add(new VertexPositionColorTexture(bottomRight, bottomRightColor, new Vector2(1f, 1f)));
            Vertices.Add(new VertexPositionColorTexture(topRight, topRightColor, new Vector2(1f, 0f)));
            Vertices.Add(new VertexPositionColorTexture(topLeft, topLeftColor, new Vector2(0f, 0f)));

            Indices.Add(first);
            Indices.Add((short)(first + 2));
            Indices.Add((short)(first + 1));
            Indices.Add(first);
            Indices.Add((short)(first + 3));
            Indices.Add((short)(first + 2));
        }
    }
}
