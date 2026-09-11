// @since 04
using Microsoft.Xna.Framework;
using MonsterMaze.UI;
#if !CH24
using MonsterMaze.Gameplay;
#endif
#if CH26
using MonsterMaze.Audio;
#endif

namespace MonsterMaze.Screens;

public sealed class MainMenuScreen : GameScreen
{
    private readonly Button _play = new("PLAY", Icons.Play);
    private readonly Button _highScores = new("HIGH SCORES", Icons.Trophy);
#if CH38
    private readonly Button _options = new("OPTIONS", Icons.Gear);
    private readonly Button _howToPlay = new("HOW TO PLAY", Icons.Star);
#endif
    private float _time;
#if CH26

    public override void Load()
    {
        Game.Audio.PlayMusic(Sounds.TitleMusic);
    }
#endif

    public override void Layout()
    {
        const int width = 780;
        const int height = 160;
        const int spacing = 200;
        float y = Size.Y * 0.46f;

        _play.Bounds = CentredRect(y, width, height);
        _highScores.Bounds = CentredRect(y + spacing, width, height);
        _highScores.Color = Palette.ButtonSecondary;
#if CH38
        _options.Bounds = CentredRect(y + spacing * 2, width, height);
        _options.Color = Palette.ButtonSecondary;
        _howToPlay.Bounds = CentredRect(y + spacing * 3, width, height);
        _howToPlay.Color = Palette.ButtonSecondary;
#endif
    }

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);
        _time += deltaSeconds;

        if (_play.Update(Input, deltaSeconds))
        {
#if CH24
            Manager.SwitchTo(new DifficultyScreen());
#elif CH23
            Manager.SwitchTo(new GameplayScreen(GameSession.NewGame(Difficulty.Normal)));
#else
            Manager.SwitchTo(new GameplayScreen(GameSession.NewGame()));
#endif
        }
        else if (_highScores.Update(Input, deltaSeconds))
        {
            Manager.SwitchTo(new HighScoreScreen());
        }
#if CH38
        else if (_options.Update(Input, deltaSeconds))
        {
            Manager.SwitchTo(new OptionsScreen());
        }
        else if (_howToPlay.Update(Input, deltaSeconds))
        {
            Manager.SwitchTo(new HowToPlayScreen());
        }
#endif
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
#if CH38
        _options.Draw(SpriteBatch, Assets);
        _howToPlay.Draw(SpriteBatch, Assets);
#endif
        SpriteBatch.End();
    }
}
