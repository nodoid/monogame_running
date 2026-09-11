using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonsterMaze.Mazes;
using MonsterMaze.Monster;
using Xunit;

namespace MonsterMaze.Tests;

public class PathfindingTests
{
    [Fact]
    public void AStarFindsTheSameLengthAsBreadthFirstSearch()
    {
        Maze maze = MazeGenerator.Generate(15, 15, 11, braidChance: 0.3f);
        var pathfinder = new Pathfinder(maze);
        var distances = new DistanceMap(maze);
        distances.Build(maze.Start);
        var path = new List<Point>();

        for (int y = 0; y < maze.Height; y++)
        {
            for (int x = 0; x < maze.Width; x++)
            {
                var goal = new Point(x, y);
                Assert.True(pathfinder.FindPath(maze.Start, goal, path));
                Assert.Equal(distances[goal], path.Count);
            }
        }
    }

    [Fact]
    public void EveryStepOfAPathIsAValidMove()
    {
        Maze maze = MazeGenerator.Generate(15, 15, 21);
        var path = new List<Point>();
        Assert.True(new Pathfinder(maze).FindPath(maze.Start, maze.Exit, path));

        Point previous = maze.Start;
        foreach (Point cell in path)
        {
            Assert.Contains(DirectionExtensions.All, direction =>
                previous + direction.ToOffset() == cell && maze.CanMove(previous, direction));
            previous = cell;
        }

        Assert.Equal(maze.Exit, previous);
    }

    [Fact]
    public void FollowingTheDistanceMapReachesTheOrigin()
    {
        Maze maze = MazeGenerator.Generate(15, 15, 8);
        var distances = new DistanceMap(maze);
        distances.Build(maze.Exit);

        Point cell = maze.Start;
        int steps = 0;
        while (cell != maze.Exit && distances.TryStepTowardOrigin(cell, out Direction direction))
        {
            cell += direction.ToOffset();
            steps++;
        }

        Assert.Equal(maze.Exit, cell);
        Assert.Equal(distances[maze.Start], steps);
    }

    [Fact]
    public void RexCanSeeDownAStraightCorridorButNotThroughWalls()
    {
        Maze maze = Maze.FromText(new[]
        {
            "#########",
            "#S      #",
            "####### #",
            "#E      #",
            "# #######"
        });

        Assert.True(RexSenses.CanSee(maze, new Point(0, 0), new Point(3, 0), 8));
        Assert.False(RexSenses.CanSee(maze, new Point(0, 0), new Point(3, 0), 2));
        Assert.False(RexSenses.CanSee(maze, new Point(0, 0), new Point(0, 1), 8));
        Assert.False(RexSenses.CanSee(maze, new Point(0, 0), new Point(3, 1), 8));
    }

    [Fact]
    public void RexSpawnsOutOfSightAndFarFromTheStart()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            Maze maze = MazeGenerator.Generate(15, 15, seed);
            var tuning = new RexTuning { MinSpawnDistance = 10 };
            Point spawn = RexSpawner.ChooseCell(maze, tuning, new System.Random(seed));

            var distances = new DistanceMap(maze);
            distances.Build(maze.Start);
            Assert.False(RexSenses.CanSee(maze, spawn, maze.Start, int.MaxValue));
            Assert.True(distances[spawn] >= System.Math.Min(10, (int)(distances.MaxDistance * 0.6f)));
        }
    }
}
