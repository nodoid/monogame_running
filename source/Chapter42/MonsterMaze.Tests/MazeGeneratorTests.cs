using Microsoft.Xna.Framework;
using MonsterMaze.Mazes;
using Xunit;

namespace MonsterMaze.Tests;

public class MazeGeneratorTests
{
    [Theory]
    [InlineData(11, 11, 1)]
    [InlineData(15, 15, 42)]
    [InlineData(19, 19, 12345)]
    public void EveryGeneratedMazeCanBeSolved(int width, int height, int seed)
    {
        Maze maze = MazeGenerator.Generate(width, height, seed);
        Assert.True(MazeGenerator.IsSolvable(maze));
    }

    [Fact]
    public void ThousandsOfRandomMazesAreAllSolvable()
    {
        for (int seed = 0; seed < 2000; seed++)
        {
            Maze maze = MazeGenerator.Generate(15, 15, seed, braidChance: seed % 3 * 0.2f);
            Assert.True(MazeGenerator.IsSolvable(maze), $"Seed {seed} produced an unsolvable maze.");
        }
    }

    [Fact]
    public void TheSameSeedAlwaysGivesTheSameMaze()
    {
        Maze first = MazeGenerator.Generate(15, 15, 2026);
        Maze second = MazeGenerator.Generate(15, 15, 2026);

        Assert.Equal(first.Start, second.Start);
        Assert.Equal(first.Exit, second.Exit);
        for (int y = 0; y < 15; y++)
        {
            for (int x = 0; x < 15; x++)
            {
                foreach (Direction direction in DirectionExtensions.All)
                    Assert.Equal(first.HasWall(new Point(x, y), direction), second.HasWall(new Point(x, y), direction));
            }
        }
    }

    [Fact]
    public void DifferentSeedsGiveDifferentMazes()
    {
        Maze first = MazeGenerator.Generate(15, 15, 1);
        Maze second = MazeGenerator.Generate(15, 15, 2);

        bool anyDifference = false;
        for (int y = 0; y < 15 && !anyDifference; y++)
        {
            for (int x = 0; x < 15 && !anyDifference; x++)
            {
                foreach (Direction direction in DirectionExtensions.All)
                    anyDifference |= first.HasWall(new Point(x, y), direction) != second.HasWall(new Point(x, y), direction);
            }
        }

        Assert.True(anyDifference);
    }

    [Fact]
    public void ThePerfectMazeJoinsEveryCellWithoutLoops()
    {
        Maze maze = MazeGenerator.Generate(15, 15, 7);

        // A perfect maze is a tree: it has exactly (cells - 1) open inner walls.
        int openings = 0;
        for (int y = 0; y < maze.Height; y++)
        {
            for (int x = 0; x < maze.Width; x++)
            {
                var cell = new Point(x, y);
                if (maze.CanMove(cell, Direction.East))
                    openings++;
                if (maze.CanMove(cell, Direction.South))
                    openings++;
            }
        }

        Assert.Equal(maze.Width * maze.Height - 1, openings);
    }

    [Fact]
    public void TheExitIsAnOpeningOnTheEdge()
    {
        Maze maze = MazeGenerator.Generate(15, 15, 99);

        Assert.True(maze.IsOnEdge(maze.Exit));
        Assert.False(maze.HasWall(maze.Exit, maze.ExitSide));
        Assert.False(maze.InBounds(maze.OutsideExit));
        Assert.True(maze.IsExitStep(maze.Exit, maze.ExitSide));
    }

    [Fact]
    public void TheStartFacesAnOpenCorridor()
    {
        Maze maze = MazeGenerator.Generate(15, 15, 3);
        Assert.True(maze.CanMove(maze.Start, maze.StartFacing));
    }

    [Fact]
    public void BraidingRemovesDeadEnds()
    {
        Maze perfect = MazeGenerator.Generate(19, 19, 5);
        Maze braided = MazeGenerator.Generate(19, 19, 5, braidChance: 1f);

        Assert.True(CountDeadEnds(braided) < CountDeadEnds(perfect));
    }

    [Fact]
    public void TextMazesAreReadCorrectly()
    {
        Maze maze = Maze.FromText(new[]
        {
            "#####",
            "#S  #",
            "### #",
            "#E  #",
            "# ###"
        });

        Assert.Equal(new Point(0, 0), maze.Start);
        Assert.Equal(new Point(0, 1), maze.Exit);
        Assert.Equal(Direction.South, maze.ExitSide);
        Assert.True(maze.CanMove(new Point(0, 0), Direction.East));
        Assert.False(maze.CanMove(new Point(0, 0), Direction.South));
    }

    private static int CountDeadEnds(Maze maze)
    {
        int count = 0;
        for (int y = 0; y < maze.Height; y++)
        {
            for (int x = 0; x < maze.Width; x++)
            {
                if (maze.IsDeadEnd(new Point(x, y)))
                    count++;
            }
        }

        return count;
    }
}
