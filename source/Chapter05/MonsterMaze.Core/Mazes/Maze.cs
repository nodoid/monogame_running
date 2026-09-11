using System;
using Microsoft.Xna.Framework;

namespace MonsterMaze.Mazes;

/// <summary>
/// A rectangular grid of cells. Each cell remembers which of its four walls are standing.
/// Walls are shared: knocking down the east wall of one cell also knocks down the west wall of
/// its neighbour. One wall on the outside of the maze is missing: that is the exit.
/// </summary>
public sealed class Maze
{
    // One byte per cell. Bit n is set when the wall in Direction n is standing.
    private readonly byte[] _walls;

    public Maze(int width, int height)
    {
        if (width < 2 || height < 2)
            throw new ArgumentOutOfRangeException(nameof(width), "A maze must be at least 2 x 2.");

        Width = width;
        Height = height;
        _walls = new byte[width * height];
        Array.Fill(_walls, (byte)0b1111);
    }

    public int Width { get; }
    public int Height { get; }

    /// <summary>Where the player starts.</summary>
    public Point Start { get; set; }

    /// <summary>Which way the player faces at the start (always down an open corridor).</summary>
    public Direction StartFacing { get; set; }

    /// <summary>The cell on the edge of the maze that contains the exit.</summary>
    public Point Exit { get; set; }

    /// <summary>Which outer wall of the exit cell is open.</summary>
    public Direction ExitSide { get; set; }

    /// <summary>The imaginary cell just outside the exit.</summary>
    public Point OutsideExit => Exit + ExitSide.ToOffset();

    public bool InBounds(Point cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;

    public bool IsOnEdge(Point cell) => cell.X == 0 || cell.Y == 0 || cell.X == Width - 1 || cell.Y == Height - 1;

    public bool HasWall(Point cell, Direction direction)
    {
        if (!InBounds(cell))
            return true;
        return (_walls[cell.Y * Width + cell.X] & (1 << (int)direction)) != 0;
    }

    /// <summary>Adds or removes a wall, keeping the neighbouring cell in step.</summary>
    public void SetWall(Point cell, Direction direction, bool standing)
    {
        SetSingleWall(cell, direction, standing);

        Point neighbour = cell + direction.ToOffset();
        if (InBounds(neighbour))
            SetSingleWall(neighbour, direction.Opposite(), standing);
    }

    /// <summary>True when there is no wall between this cell and the next one in that direction.</summary>
    public bool CanMove(Point cell, Direction direction)
    {
        return !HasWall(cell, direction) && InBounds(cell + direction.ToOffset());
    }

    /// <summary>True if stepping this way from this cell leaves the maze through the exit.</summary>
    public bool IsExitStep(Point cell, Direction direction) => cell == Exit && direction == ExitSide;

    /// <summary>How many of this cell's sides lead somewhere.</summary>
    public int CountOpenSides(Point cell)
    {
        int open = 0;
        foreach (Direction direction in DirectionExtensions.All)
        {
            if (CanMove(cell, direction))
                open++;
        }

        return open;
    }

    public bool IsDeadEnd(Point cell) => CountOpenSides(cell) == 1;

    /// <summary>
    /// Builds a maze from a picture made of text. Each cell is a character at an odd position;
    /// '#' is a wall, a space is a gap, 'S' marks the start and 'E' marks the exit cell. The exit
    /// opening is the gap in the outer wall beside the 'E'.
    /// </summary>
    public static Maze FromText(string[] rows)
    {
        int width = (rows[0].Length - 1) / 2;
        int height = (rows.Length - 1) / 2;
        var maze = new Maze(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var cell = new Point(x, y);
                int column = x * 2 + 1;
                int row = y * 2 + 1;

                foreach (Direction direction in DirectionExtensions.All)
                {
                    Point offset = direction.ToOffset();
                    if (rows[row + offset.Y][column + offset.X] != '#')
                        maze.SetSingleWall(cell, direction, false);
                }

                char marker = rows[row][column];
                if (marker == 'S')
                    maze.Start = cell;
                else if (marker == 'E')
                    maze.Exit = cell;
            }
        }

        foreach (Direction direction in DirectionExtensions.All)
        {
            if (!maze.HasWall(maze.Exit, direction) && !maze.InBounds(maze.Exit + direction.ToOffset()))
                maze.ExitSide = direction;
            if (maze.CanMove(maze.Start, direction))
                maze.StartFacing = direction;
        }

        return maze;
    }

    private void SetSingleWall(Point cell, Direction direction, bool standing)
    {
        int index = cell.Y * Width + cell.X;
        byte bit = (byte)(1 << (int)direction);
        _walls[index] = standing ? (byte)(_walls[index] | bit) : (byte)(_walls[index] & ~bit);
    }
}
