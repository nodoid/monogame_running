using Microsoft.Xna.Framework;
using MonsterMaze.Gameplay;
using MonsterMaze.UI;

namespace MonsterMaze.Screens;

/// <summary>A simple "game over" screen that tells the player how the game ended.</summary>
public sealed class GameOverScreen : GameScreen
{
    private readonly GameSession _session;
    private readonly Button _continue = new("CONTINUE", Icons.Play);

    public GameOverScreen(GameSession session)
    {
        _session = session;
    }

    public override void Layout()
    {
        _continue.Bounds = CentredRect(Size.Y * 0.78f, 720, 160);
    }

    public override void Update(GameTime gameTime)
    {
        if (_continue.Update(Input, Seconds(gameTime)) || Input.ConfirmPressed || Input.BackPressed)
            ScoreFlow.Continue(Manager, _session);
    }

    public override void Draw(GameTime gameTime)
    {
        BeginSpriteBatch();
        DrawBackground(Assets.MenuBackground, new Color(120, 120, 120));

        bool escaped = _session.Outcome == GameOutcome.Escaped;
        Fonts.DrawOutlined(SpriteBatch, escaped ? "YOU ESCAPED!" : "YOU WERE EATEN!",
            new Vector2(Size.X / 2f, Size.Y * 0.3f), 130f, escaped ? Palette.Gold : Palette.Danger, Color.Black, 6f);

        Fonts.DrawShadowed(SpriteBatch, "SCORE", new Vector2(Size.X / 2f, Size.Y * 0.45f), 56f, Palette.TextMuted);
        Fonts.DrawShadowed(SpriteBatch, _session.Score.ToString("N0"), new Vector2(Size.X / 2f, Size.Y * 0.52f), 130f,
            Palette.Gold);
        Fonts.DrawShadowed(SpriteBatch, $"{_session.CellsExplored} CELLS EXPLORED IN {_session.Seconds:0} SECONDS",
            new Vector2(Size.X / 2f, Size.Y * 0.6f), 44f, Palette.Text);

        _continue.Draw(SpriteBatch, Assets);
        SpriteBatch.End();
    }
}
