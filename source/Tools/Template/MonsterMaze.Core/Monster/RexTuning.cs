// @since 17
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
#if CH18

    /// <summary>Seconds per cell while Rex searches the maze.</summary>
    public float WanderStepSeconds { get; init; } = 0.9f;

    /// <summary>Seconds per cell while he follows the player's footsteps.</summary>
    public float StalkStepSeconds { get; init; } = 0.72f;

    /// <summary>Seconds per cell once he has seen the player and charges.</summary>
    public float ChargeStepSeconds { get; init; } = 0.42f;
#endif
#if CH19

    /// <summary>Rex waits in his lair this long before hunting...</summary>
    public float LurkSeconds { get; init; } = 12f;

    /// <summary>...or until the player has taken this many steps.</summary>
    public int LurkSteps { get; init; } = 10;

    /// <summary>How long he keeps charging after losing sight of the player.</summary>
    public float LoseSightSeconds { get; init; } = 2.5f;

    /// <summary>The chance, each time he reaches a cell while searching, that he stops to sniff.</summary>
    public float SniffChance { get; init; } = 0.12f;

    /// <summary>How long he roars on spotting the player before charging: the player's head start.</summary>
    public float SpotPauseSeconds { get; init; } = 0.8f;
#endif
#if CH21

    /// <summary>Rex never starts closer to the player than this many steps.</summary>
    public int MinSpawnDistance { get; init; } = 10;
#endif
}
