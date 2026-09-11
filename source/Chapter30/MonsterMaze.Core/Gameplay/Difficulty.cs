using Microsoft.Xna.Framework;
using MonsterMaze.Monster;
using MonsterMaze.UI;

namespace MonsterMaze.Gameplay;

public enum Difficulty
{
    Easy,
    Normal,
    Hard
}

/// <summary>
/// Everything that changes between Easy, Normal and Hard. Every system reads its numbers from
/// here, so balancing a level means editing just one block of values.
/// </summary>
public sealed class DifficultySettings
{
    public static readonly DifficultySettings Easy = new()
    {
        Level = Difficulty.Easy,
        Name = "EASY",
        Description = "A small maze with lots of loops. Rex is slow and gives plenty of warning.",
        Color = Palette.Easy,
        MazeWidth = 11,
        MazeHeight = 11,
        BraidChance = 0.35f,
        FogStart = 2f,
        FogEnd = 13f,
        FootstepWarningRange = 8,
        ShowHuntingWarning = true,
        ScoreMultiplier = 1,
        Rex = new RexTuning
        {
            HearingRange = 5,
            SightRange = 6,
            WanderStepSeconds = 1.05f,
            StalkStepSeconds = 0.85f,
            ChargeStepSeconds = 0.52f,
            LurkSeconds = 20f,
            LurkSteps = 16,
            LoseSightSeconds = 1.8f,
            SniffChance = 0.2f,
            SpotPauseSeconds = 1.3f,
            MinSpawnDistance = 12
        }
    };

    public static readonly DifficultySettings Normal = new()
    {
        Level = Difficulty.Normal,
        Name = "NORMAL",
        Description = "The classic challenge. Rex hunts by sound and charges on sight.",
        Color = Palette.Normal,
        MazeWidth = 15,
        MazeHeight = 15,
        BraidChance = 0.18f,
        FogStart = 1.5f,
        FogEnd = 11f,
        FootstepWarningRange = 6,
        ShowHuntingWarning = true,
        ScoreMultiplier = 2,
        Rex = new RexTuning
        {
            HearingRange = 7,
            SightRange = 8,
            WanderStepSeconds = 0.9f,
            StalkStepSeconds = 0.72f,
            ChargeStepSeconds = 0.42f,
            LurkSeconds = 12f,
            LurkSteps = 10,
            LoseSightSeconds = 2.5f,
            SniffChance = 0.12f,
            SpotPauseSeconds = 0.85f,
            MinSpawnDistance = 14
        }
    };

    public static readonly DifficultySettings Hard = new()
    {
        Level = Difficulty.Hard,
        Name = "HARD",
        Description = "A vast, dark maze with few escape routes. Rex is fast, sharp-eared and quiet.",
        Color = Palette.Hard,
        MazeWidth = 19,
        MazeHeight = 19,
        BraidChance = 0.06f,
        FogStart = 1f,
        FogEnd = 8.5f,
        FootstepWarningRange = 3,
        ShowHuntingWarning = false,
        ScoreMultiplier = 3,
        Rex = new RexTuning
        {
            HearingRange = 10,
            SightRange = 12,
            WanderStepSeconds = 0.75f,
            StalkStepSeconds = 0.6f,
            ChargeStepSeconds = 0.36f,
            LurkSeconds = 6f,
            LurkSteps = 5,
            LoseSightSeconds = 3.5f,
            SniffChance = 0.06f,
            SpotPauseSeconds = 0.55f,
            MinSpawnDistance = 16
        }
    };

    public Difficulty Level { get; private init; }
    public string Name { get; private init; }
    public string Description { get; private init; }
    public Color Color { get; private init; }

    public int MazeWidth { get; private init; }
    public int MazeHeight { get; private init; }

    /// <summary>The fraction of dead ends knocked through to make loops.</summary>
    public float BraidChance { get; private init; }

    public float FogStart { get; private init; }
    public float FogEnd { get; private init; }

    public RexTuning Rex { get; private init; }

    public int FootstepWarningRange { get; private init; }
    public bool ShowHuntingWarning { get; private init; }
    public int ScoreMultiplier { get; private init; }

    public static DifficultySettings For(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => Easy,
        Difficulty.Hard => Hard,
        _ => Normal
    };
}
