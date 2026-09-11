using Microsoft.Xna.Framework;
using MonsterMaze.UI;
using MonsterMaze.Gameplay;

namespace MonsterMaze.Screens;

public sealed class MainMenuScreen : GameScreen
{
    private readonly Button _play = new("PLAY", Icons.Play);
    private readonly Button _highScores = new("HIGH SCORES", Icons.Trophy);
    private float _time;

    public override void Layout()
    {
        const int width = 780;
        const int height = 160;
        const int spacing = 200;
        float y = Size.Y * 0.46f;

        _play.Bounds = CentredRect(y, width, height);
        _highScores.Bounds = CentredRect(y + spacing, width, height);
        _highScores.Color = Palette.ButtonSecondary;
    }

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);
        _time += deltaSeconds;

        if (_play.Update(Input, deltaSeconds))
        {
            Manager.SwitchTo(new GameplayScreen(GameSession.NewGame(Difficulty.Normal)));
        }
        else if (_highScores.Update(Input, deltaSeconds))
        {
            Manager.SwitchTo(new HighScoreScreen());
        }
        else if (Input.BackPressed)
        {
            Manager.SwitchTo(new TitleScreen());
        }
    }

    public override void Draw(GameTime gameTime)
    {
        BeginSpriteBatch();
        DrawBackground(Assets.MenuBackground, Color.White);

        float bob = System.MathF.Sin(_time * 2f) * 6f;
        Fonts.DrawOutlined(SpriteBatch, "MONSTER MAZE", new Vector2(Size.X / 2f, SafeArea.Top + 170f + bob), 150f,
            Palette.Accent, Color.Black, 6f);

        _play.Draw(SpriteBatch, Assets);
        _highScores.Draw(SpriteBatch, Assets);
        SpriteBatch.End();
    }
}
