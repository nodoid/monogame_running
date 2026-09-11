// @since 06
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MonsterMaze.Mazes;

/// <summary>
/// Creates random mazes. The same seed always produces the same maze, which is invaluable for
/// testing; the game passes a fresh random seed every time so no two games are alike.
/// </summary>
public static class MazeGenerator
{
    /// <param name="braidChance">
    /// 0 gives a "perfect" maze with exactly one route between any two cells. Higher values
    /// knock through that fraction of dead ends, adding loops and escape routes.
    /// </param>
    public static Maze Generate(int width, int height, int seed, float braidChance = 0f)
    {
        var random = new Random(seed);
        var maze = new Maze(width, height) { Seed = seed };

        CarvePassages(maze, random);
        if (braidChance > 0f)
            RemoveDeadEnds(maze, random, braidChance);
        PlaceStartAndExit(maze, random);

        return maze;
    }

    /// <summary>Checks that the exit can be reached from the start.</summary>
    public static bool IsSolvable(Maze maze)
    {
        var distances = new DistanceMap(maze);
        distances.Build(maze.Start);
        return distances[maze.Exit] != DistanceMap.Unreachable;
    }

    /// <summary>
    /// The recursive backtracker. Starting anywhere, keep walking to a random unvisited
    /// neighbour, knocking down the wall between. At a dead end, back up until a cell with an
    /// unvisited neighbour turns up. When the stack is empty, every cell has been joined.
    /// A stack stands in for recursion so large mazes can't overflow the call stack.
    /// </summary>
    private static void CarvePassages(Maze maze, Random random)
    {
        var visited = new bool[maze.Width * maze.Height];
        var stack = new Stack<Point>();
        Span<Direction> choices = stackalloc Direction[4];

        var first = new Point(random.Next(maze.Width), random.Next(maze.Height));
        visited[first.Y * maze.Width + first.X] = true;
        stack.Push(first);

        while (stack.Count > 0)
        {
            Point cell = stack.Peek();

            int count = 0;
            foreach (Direction direction in DirectionExtensions.All)
            {
                Point next = cell + direction.ToOffset();
                if (maze.InBounds(next) && !visited[next.Y * maze.Width + next.X])
                    choices[count++] = direction;
            }

            if (count == 0)
            {
                stack.Pop();
                continue;
            }

            Direction chosen = choices[random.Next(count)];
            Point neighbour = cell + chosen.ToOffset();
            maze.SetWall(cell, chosen, false);
            visited[neighbour.Y * maze.Width + neighbour.X] = true;
            stack.Push(neighbour);
        }
    }

    /// <summary>"Braiding": open up some dead ends so the maze has loops.</summary>
    private static void RemoveDeadEnds(Maze maze, Random random, float chance)
    {
        Span<Direction> choices = stackalloc Direction[4];

        for (int y = 0; y < maze.Height; y++)
        {
            for (int x = 0; x < maze.Width; x++)
            {
                var cell = new Point(x, y);
                if (!maze.IsDeadEnd(cell) || random.NextDouble() >= chance)
                    continue;

                // Prefer joining two dead ends together; otherwise knock through any inner wall.
                int count = 0;
                foreach (Direction direction in DirectionExtensions.All)
                {
                    Point next = cell + direction.ToOffset();
                    if (maze.InBounds(next) && maze.HasWall(cell, direction) && maze.IsDeadEnd(next))
                        choices[count++] = direction;
                }

                if (count == 0)
                {
                    foreach (Direction direction in DirectionExtensions.All)
                    {
                        if (maze.InBounds(cell + direction.ToOffset()) && maze.HasWall(cell, direction))
                            choices[count++] = direction;
                    }
                }

                if (count > 0)
                    maze.SetWall(cell, choices[random.Next(count)], false);
            }
        }
    }

    /// <summary>
    /// Puts the start and exit as far apart as possible. A breadth-first search from a random
    /// cell finds the far end of a long corridor: that's the start. A second search from the start
    /// finds the most distant cell on the edge of the maze: that's the exit.
    /// </summary>
    private static void PlaceStartAndExit(Maze maze, Random random)
    {
        var distances = new DistanceMap(maze);

        distances.Build(new Point(random.Next(maze.Width), random.Next(maze.Height)));
        maze.Start = distances.FindFarthest(_ => true);

        distances.Build(maze.Start);
        maze.Exit = distances.FindFarthest(maze.IsOnEdge);

        // Open the outer wall of the exit cell. Corner cells have two outer walls; pick either.
        Span<Direction> outward = stackalloc Direction[2];
        int count = 0;
        foreach (Direction direction in DirectionExtensions.All)
        {
            if (!maze.InBounds(maze.Exit + direction.ToOffset()))
                outward[count++] = direction;
        }

        maze.ExitSide = outward[random.Next(count)];
        maze.SetWall(maze.Exit, maze.ExitSide, false);

        // Face the player down an open corridor.
        foreach (Direction direction in DirectionExtensions.All)
        {
            if (maze.CanMove(maze.Start, direction))
            {
                maze.StartFacing = direction;
                break;
            }
        }
    }
}
