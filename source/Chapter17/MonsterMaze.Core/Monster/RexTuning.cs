namespace MonsterMaze.Monster;

/// <summary>
/// Every number that decides how dangerous Rex is, gathered in one place so the game can be
/// balanced without hunting through the code. Each difficulty level has its own set.
/// </summary>
public sealed class RexTuning
{
    /// <summary>How many steps away, walking through the maze, Rex can hear the player.</summary>
    public int HearingRange { get; init; } = 7;

    /// <summary>How many cells down a straight corridor Rex can see.</summary>
    public int SightRange { get; init; } = 8;
}
