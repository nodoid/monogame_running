// @since 06
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MonsterMaze.Mazes;

/// <summary>
/// How many steps it takes to walk from one origin cell to every other cell, found with a
/// breadth-first search. The arrays are reused, so rebuilding it every move costs no garbage.
/// </summary>
public sealed class DistanceMap
{
    public const int Unreachable = int.MaxValue;

    private readonly int[] _distances;
    private readonly Queue<Point> _queue = new();

    public DistanceMap(Maze maze)
    {
        Maze = maze;
        _distances = new int[maze.Width * maze.Height];
    }

    public Maze Maze { get; }
    public Point Origin { get; private set; }

    /// <summary>The largest distance found by the last <see cref="Build"/>.</summary>
    public int MaxDistance { get; private set; }

    public int this[Point cell] => Maze.InBounds(cell) ? _distances[cell.Y * Maze.Width + cell.X] : Unreachable;

    /// <summary>Breadth-first search: visit cells in rings of equal distance from the origin.</summary>
    public void Build(Point origin)
    {
        Array.Fill(_distances, Unreachable);
        Origin = origin;
        MaxDistance = 0;

        _distances[origin.Y * Maze.Width + origin.X] = 0;
        _queue.Clear();
        _queue.Enqueue(origin);

        while (_queue.Count > 0)
        {
            Point cell = _queue.Dequeue();
            int next = _distances[cell.Y * Maze.Width + cell.X] + 1;

            foreach (Direction direction in DirectionExtensions.All)
            {
                if (!Maze.CanMove(cell, direction))
                    continue;

                Point neighbour = cell + direction.ToOffset();
                int index = neighbour.Y * Maze.Width + neighbour.X;
                if (_distances[index] != Unreachable)
                    continue;

                _distances[index] = next;
                MaxDistance = Math.Max(MaxDistance, next);
                _queue.Enqueue(neighbour);
            }
        }
    }

    /// <summary>The reachable cell farthest from the origin that passes the filter.</summary>
    public Point FindFarthest(Func<Point, bool> filter)
    {
        Point best = Origin;
        int bestDistance = -1;
        for (int y = 0; y < Maze.Height; y++)
        {
            for (int x = 0; x < Maze.Width; x++)
            {
                var cell = new Point(x, y);
                int distance = this[cell];
                if (distance != Unreachable && distance > bestDistance && filter(cell))
                {
                    best = cell;
                    bestDistance = distance;
                }
            }
        }

        return best;
    }
#if CH18

    /// <summary>
    /// Which way to step from <paramref name="from"/> to get one cell closer to the origin.
    /// Following this repeatedly walks the shortest path to the origin.
    /// </summary>
    public bool TryStepTowardOrigin(Point from, out Direction direction)
    {
        direction = Direction.North;
        int best = this[from];
        bool found = false;

        foreach (Direction candidate in DirectionExtensions.All)
        {
            if (!Maze.CanMove(from, candidate))
                continue;

            int distance = this[from + candidate.ToOffset()];
            if (distance < best)
            {
                best = distance;
                direction = candidate;
                found = true;
            }
        }

        return found;
    }
#endif
}
