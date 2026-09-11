using MonsterMaze.Gameplay;

namespace MonsterMaze.Screens;

/// <summary>What happens after a game ends, once the player taps "continue".</summary>
public static class ScoreFlow
{
    public static void Continue(ScreenManager manager, GameSession session)
    {
        // A score good enough for the table earns the chance to enter a name.
        if (manager.Game.HighScores.Qualifies(session.Difficulty, session.Score))
        {
            manager.SwitchTo(new NameEntryScreen(session));
            return;
        }

        manager.SwitchTo(new HighScoreScreen(session.Difficulty));
    }
}
