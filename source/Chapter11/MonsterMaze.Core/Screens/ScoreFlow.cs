using MonsterMaze.Gameplay;

namespace MonsterMaze.Screens;

/// <summary>What happens after a game ends, once the player taps "continue".</summary>
public static class ScoreFlow
{
    public static void Continue(ScreenManager manager, GameSession session)
    {
        manager.SwitchTo(new HighScoreScreen());
    }
}
