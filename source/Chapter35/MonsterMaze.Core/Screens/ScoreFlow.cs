using System;
using MonsterMaze.Data;
using MonsterMaze.Gameplay;

namespace MonsterMaze.Screens;

/// <summary>What happens after a game ends, once the player taps "continue".</summary>
public static class ScoreFlow
{
    public static void Continue(ScreenManager manager, GameSession session)
    {
        // Until players can type their name, new high scores are entered as "PLAYER".
        int rank = manager.Game.HighScores.Add(session.Difficulty, new HighScoreEntry
        {
            Name = "PLAYER",
            Score = session.Score,
            Outcome = session.Outcome,
            Date = DateTime.Now
        });
        manager.SwitchTo(new HighScoreScreen(session.Difficulty, rank));
    }
}
