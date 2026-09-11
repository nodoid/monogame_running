// @since 04
using System;

namespace MonsterMaze.Gameplay;

/// <summary>How a game ended.</summary>
public enum GameOutcome
{
    None,
    Escaped,
    Eaten
}

/// <summary>
/// Everything about one game from start to finish. It is created by the menu, filled in by
/// the gameplay screen and then handed on to the game over and high score screens.
/// </summary>
public sealed class GameSession
{
#if CH23
    public GameSession(Difficulty difficulty, int seed)
    {
        Difficulty = difficulty;
        Settings = DifficultySettings.For(difficulty);
        Seed = seed;
    }

    public Difficulty Difficulty { get; }

    /// <summary>The numbers that make this difficulty level easy, normal or hard.</summary>
    public DifficultySettings Settings { get; }
#else
    public GameSession(int seed)
    {
        Seed = seed;
    }
#endif

    /// <summary>The random seed that creates this game's maze. Every game gets a new one.</summary>
    public int Seed { get; }

    public GameOutcome Outcome { get; set; }
#if CH22

    public int Score { get; set; }
    public float Seconds { get; set; }
    public int Steps { get; set; }
    public int CellsExplored { get; set; }
    public int NearMisses { get; set; }
#endif
#if CH34
    public int EscapeBonus { get; set; }
    public int TimeBonus { get; set; }
#endif

#if CH23
    /// <summary>Starts a new game with a brand-new random maze.</summary>
    public static GameSession NewGame(Difficulty difficulty) => new(difficulty, Random.Shared.Next());
#else
    /// <summary>Starts a new game with a brand-new random maze.</summary>
    public static GameSession NewGame() => new(Random.Shared.Next());
#endif
}
