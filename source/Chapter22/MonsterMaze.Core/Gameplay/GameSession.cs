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
    public GameSession(int seed)
    {
        Seed = seed;
    }

    /// <summary>The random seed that creates this game's maze. Every game gets a new one.</summary>
    public int Seed { get; }

    public GameOutcome Outcome { get; set; }

    public int Score { get; set; }
    public float Seconds { get; set; }
    public int Steps { get; set; }
    public int CellsExplored { get; set; }
    public int NearMisses { get; set; }

    /// <summary>Starts a new game with a brand-new random maze.</summary>
    public static GameSession NewGame() => new(Random.Shared.Next());
}
