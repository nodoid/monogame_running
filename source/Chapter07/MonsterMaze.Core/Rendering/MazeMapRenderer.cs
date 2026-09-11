using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.Mazes;
using MonsterMaze.UI;

namespace MonsterMaze.Rendering;

/// <summary>
/// Draws the maze from above with SpriteBatch. It's a developer's tool: it lets us check the
/// generator, see where Rex is and understand what the game is doing.
/// </summary>
public sealed class MazeMapRenderer
{
    private Vector2 _origin;

    /// <summary>The size of one cell on screen, in virtual units.</summary>
    public float CellSize { get; private set; } = 32f;

    /// <summary>Scales and centres the map inside an area of the screen.</summary>
    public void Fit(Maze maze, Rectangle area)
    {
        CellSize = MathF.Floor(MathF.Min(area.Width / (float)maze.Width, area.Height / (float)maze.Height));
        var mapSize = new Vector2(maze.Width, maze.Height) * CellSize;
        _origin = area.Center.ToVector2() - mapSize / 2f;
    }

    /// <summary>The screen position of the centre of a (possibly fractional) cell position.</summary>
    public Vector2 CellCentre(Vector2 cell) => _origin + (cell + new Vector2(0.5f)) * CellSize;

    public void DrawBackground(SpriteBatch spriteBatch, Maze maze, Color color)
    {
        float border = CellSize * 0.3f;
        spriteBatch.FillRectangle(_origin - new Vector2(border),
            new Vector2(maze.Width, maze.Height) * CellSize + new Vector2(border * 2f), color);
    }

    /// <summary>
    /// Colours every cell by its distance from the map's origin: a heat map running from red
    /// (close) through the rainbow to violet (far away).
    /// </summary>
    public void DrawDistances(SpriteBatch spriteBatch, DistanceMap distances, float alpha)
    {
        float maxDistance = Math.Max(1, distances.MaxDistance);
        for (int y = 0; y < distances.Maze.Height; y++)
        {
            for (int x = 0; x < distances.Maze.Width; x++)
            {
                int distance = distances[new Point(x, y)];
                if (distance == DistanceMap.Unreachable)
                    continue;

                Color color = Palette.FromHsv(0.78f * distance / maxDistance, 0.75f, 0.55f, alpha);
                spriteBatch.FillRectangle(_origin + new Vector2(x, y) * CellSize, new Vector2(CellSize), color);
            }
        }
    }

    public void DrawWalls(SpriteBatch spriteBatch, Maze maze, Color color, float thickness)
    {
        for (int y = 0; y < maze.Height; y++)
        {
            for (int x = 0; x < maze.Width; x++)
            {
                var cell = new Point(x, y);
                Vector2 topLeft = _origin + new Vector2(x, y) * CellSize;
                Vector2 topRight = topLeft + new Vector2(CellSize, 0f);
                Vector2 bottomLeft = topLeft + new Vector2(0f, CellSize);
                Vector2 bottomRight = topLeft + new Vector2(CellSize);

                // Each wall is shared, so draw north and west for every cell and only draw
                // south and east along the bottom and right edges.
                if (maze.HasWall(cell, Direction.North))
                    DrawWall(spriteBatch, topLeft, topRight, color, thickness);
                if (maze.HasWall(cell, Direction.West))
                    DrawWall(spriteBatch, topLeft, bottomLeft, color, thickness);
                if (y == maze.Height - 1 && maze.HasWall(cell, Direction.South))
                    DrawWall(spriteBatch, bottomLeft, bottomRight, color, thickness);
                if (x == maze.Width - 1 && maze.HasWall(cell, Direction.East))
                    DrawWall(spriteBatch, topRight, bottomRight, color, thickness);
            }
        }
    }

    /// <summary>A square marker in the middle of a cell.</summary>
    public void DrawMarker(SpriteBatch spriteBatch, Vector2 cell, Color color, float size = 0.6f)
    {
        float side = CellSize * size;
        spriteBatch.FillRectangle(CellCentre(cell) - new Vector2(side / 2f), new Vector2(side), color);
    }

    private static void DrawWall(SpriteBatch spriteBatch, Vector2 from, Vector2 to, Color color, float thickness)
    {
        // Extend each wall by half its thickness so the corners join up neatly.
        Vector2 along = Vector2.Normalize(to - from) * thickness / 2f;
        spriteBatch.DrawLine(from - along, to + along, color, thickness);
    }
}
