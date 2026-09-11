// @since 21
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonsterMaze.Mazes;

namespace MonsterMaze.Monster;

/// <summary>
/// Chooses where Rex waits at the start of a game. It has to be fair: far enough away to give the
/// player a chance, never within sight of the start, and ideally tucked away in a dead end.
/// </summary>
public static class RexSpawner
{
    public static Point ChooseCell(Maze maze, RexTuning tuning, Random random)
    {
        var distances = new DistanceMap(maze);
        distances.Build(maze.Start);

        // In a small maze the full distance may be impossible; never ask for more than 60% of the longest walk.
        int minimum = Math.Min(tuning.MinSpawnDistance, (int)(distances.MaxDistance * 0.6f));

        var candidates = new List<Point>();
        var deadEnds = new List<Point>();
        for (int y = 0; y < maze.Height; y++)
        {
            for (int x = 0; x < maze.Width; x++)
            {
                var cell = new Point(x, y);
                if (cell == maze.Exit || distances[cell] < minimum)
                    continue;
                if (RexSenses.CanSee(maze, cell, maze.Start, int.MaxValue))
                    continue;

                candidates.Add(cell);
                if (maze.IsDeadEnd(cell))
                    deadEnds.Add(cell);
            }
        }

        if (deadEnds.Count > 0)
            return deadEnds[random.Next(deadEnds.Count)];
        if (candidates.Count > 0)
            return candidates[random.Next(candidates.Count)];
        return distances.FindFarthest(_ => true);
    }
}
