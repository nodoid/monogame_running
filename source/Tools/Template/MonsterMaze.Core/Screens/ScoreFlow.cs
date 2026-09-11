// @since 04
#if CH35 && !CH36
using System;
using MonsterMaze.Data;
#endif
using MonsterMaze.Gameplay;

namespace MonsterMaze.Screens;

/// <summary>What happens after a game ends, once the player taps "continue".</summary>
public static class ScoreFlow
{
    public static void Continue(ScreenManager manager, GameSession session)
    {
#if CH36
        // A score good enough for the table earns the chance to enter a name.
        if (manager.Game.HighScores.Qualifies(session.Difficulty, session.Score))
        {
            manager.SwitchTo(new NameEntryScreen(session));
            return;
        }

        manager.SwitchTo(new HighScoreScreen(session.Difficulty));
#endif
#if CH35 && !CH36
        // Until players can type their name, new high scores are entered as "PLAYER".
        int rank = manager.Game.HighScores.Add(session.Difficulty, new HighScoreEntry
        {
            Name = "PLAYER",
            Score = session.Score,
            Outcome = session.Outcome,
            Date = DateTime.Now
        });
        manager.SwitchTo(new HighScoreScreen(session.Difficulty, rank));
#endif
#if !CH35
        manager.SwitchTo(new HighScoreScreen());
#endif
    }
}
