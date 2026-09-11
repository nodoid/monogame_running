using Microsoft.Xna.Framework;
using MonsterMaze.Data;
using MonsterMaze.Gameplay;
using MonsterMaze.Mazes;
using MonsterMaze.Screens;
using Xunit;

namespace MonsterMaze.Tests;

public class ScoringTests
{
    private static Maze SmallMaze() => MazeGenerator.Generate(5, 5, 1);

    /// <summary>A cell the player hasn't visited yet (anywhere but the start).</summary>
    private static Point NewCell(Maze maze) => maze.Start == new Point(1, 0) ? new Point(2, 0) : new Point(1, 0);

    [Fact]
    public void NewCellsScoreMoreThanOldOnes()
    {
        Maze maze = SmallMaze();
        var score = new ScoreKeeper(maze, 1);
        score.OnStep(NewCell(maze));
        int afterFirstVisit = score.Score;
        score.OnStep(NewCell(maze));

        Assert.Equal(ScoreKeeper.StepPoints + ScoreKeeper.NewCellPoints, afterFirstVisit);
        Assert.Equal(afterFirstVisit + ScoreKeeper.StepPoints, score.Score);
    }

    [Fact]
    public void TheDifficultyMultiplierAppliesToEverything()
    {
        Maze maze = SmallMaze();
        var single = new ScoreKeeper(maze, 1);
        var triple = new ScoreKeeper(maze, 3);
        single.OnStep(NewCell(maze));
        triple.OnStep(NewCell(maze));

        Assert.Equal(single.Score * 3, triple.Score);
    }

    [Fact]
    public void ANearMissIsAwardedOnceRexIsShakenOff()
    {
        var score = new ScoreKeeper(SmallMaze(), 1);
        string reason = null;
        score.Awarded += (_, text) => reason = text;

        score.Update(0.1f, true, 1);
        score.Update(0.1f, true, 2);
        Assert.Null(reason);
        score.Update(0.1f, true, 4);

        Assert.Equal(1, score.NearMisses);
        Assert.Equal("NEAR MISS!", reason);
    }

    [Fact]
    public void EscapingQuicklyEarnsASpeedBonus()
    {
        var fast = new ScoreKeeper(SmallMaze(), 1).AwardEscape(10f);
        var slow = new ScoreKeeper(SmallMaze(), 1).AwardEscape(250f);

        Assert.Equal(ScoreKeeper.EscapePoints, fast.EscapeBonus);
        Assert.True(fast.TimeBonus > slow.TimeBonus);
        Assert.True(slow.TimeBonus >= 0);
    }

    [Fact]
    public void TheHighScoreTableKeepsOnlyTheTopSix()
    {
        var table = new HighScoreTable();
        for (int i = 1; i <= 10; i++)
            table.Add(Difficulty.Normal, new HighScoreEntry { Name = $"P{i}", Score = i * 100 });

        Assert.Equal(HighScoreTable.MaxEntries, table[Difficulty.Normal].Count);
        Assert.Equal(1000, table[Difficulty.Normal][0].Score);
        Assert.Equal(500, table[Difficulty.Normal][5].Score);
    }

    [Fact]
    public void ScoresMustBeatTheLowestEntryToQualify()
    {
        HighScoreTable table = HighScoreTable.CreateDefault();
        int lowest = table[Difficulty.Easy][HighScoreTable.MaxEntries - 1].Score;

        Assert.False(table.Qualifies(Difficulty.Easy, lowest));
        Assert.True(table.Qualifies(Difficulty.Easy, lowest + 1));
        Assert.Equal(-1, table.Add(Difficulty.Easy, new HighScoreEntry { Name = "LOW", Score = lowest }));
        Assert.Equal(0, table.Add(Difficulty.Easy, new HighScoreEntry { Name = "TOP", Score = 1_000_000 }));
    }

    [Theory]
    [InlineData("rex", "REX")]
    [InlineData("  Ada   Lovelace!! ", "ADA LOVELA")]
    [InlineData("12345678901234", "1234567890")]
    [InlineData("", "")]
    [InlineData("???", "")]
    public void NamesAreSanitised(string typed, string expected)
    {
        Assert.Equal(expected, NameEntryScreen.Sanitise(typed));
    }
}
