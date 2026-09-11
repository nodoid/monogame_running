using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MonsterMaze.Mazes;

/// <summary>
/// Finds the shortest route between two cells with the A* algorithm. It explores the most
/// promising cells first, guided by the straight-line ("Manhattan") distance to the goal.
/// </summary>
public sealed class Pathfinder
{
    private readonly Maze _maze;
    private readonly int[] _cost;
    private readonly Point[] _cameFrom;
    private readonly bool[] _closed;
    private readonly PriorityQueue<Point, int> _open = new();

    public Pathfinder(Maze maze)
    {
        _maze = maze;
        int cells = maze.Width * maze.Height;
        _cost = new int[cells];
        _cameFrom = new Point[cells];
        _closed = new bool[cells];
    }

    /// <summary>
    /// Fills <paramref name="path"/> with the cells from the start (not included) to the goal.
    /// Returns false if the goal can't be reached.
    /// </summary>
    public bool FindPath(Point start, Point goal, List<Point> path)
    {
        path.Clear();
        Array.Fill(_cost, int.MaxValue);
        Array.Clear(_closed);
        _open.Clear();

        _cost[Index(start)] = 0;
        _open.Enqueue(start, Heuristic(start, goal));

        while (_open.TryDequeue(out Point cell, out _))
        {
            if (cell == goal)
            {
                // Walk back along the trail of "came from" links, then reverse it.
                for (Point step = goal; step != start; step = _cameFrom[Index(step)])
                    path.Add(step);
                path.Reverse();
                return true;
            }

            if (_closed[Index(cell)])
                continue;
            _closed[Index(cell)] = true;

            int nextCost = _cost[Index(cell)] + 1;
            foreach (Direction direction in DirectionExtensions.All)
            {
                if (!_maze.CanMove(cell, direction))
                    continue;

                Point neighbour = cell + direction.ToOffset();
                if (nextCost >= _cost[Index(neighbour)])
                    continue;

                _cost[Index(neighbour)] = nextCost;
                _cameFrom[Index(neighbour)] = cell;
                _open.Enqueue(neighbour, nextCost + Heuristic(neighbour, goal));
            }
        }

        return false;
    }

    private int Index(Point cell) => cell.Y * _maze.Width + cell.X;

    private static int Heuristic(Point a, Point b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}
