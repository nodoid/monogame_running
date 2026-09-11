// @since 04
using Microsoft.Xna.Framework;
using MonsterMaze.Platform;
using MonsterMaze.UI;

namespace MonsterMaze.Screens;

/// <summary>
/// The pause menu. It is a pop-up, so the frozen game stays visible behind it, and it keeps the
/// landscape orientation of the game underneath.
/// </summary>
public sealed class PauseScreen : GameScreen
{
    private readonly GameplayScreen _gameplay;
    private readonly Button _resume = new("RESUME", Icons.Play);
#if CH39
    private readonly Button _restart = new("NEW MAZE", Icons.Star);
#endif
    private readonly Button _quit = new("QUIT", Icons.Back);

    public PauseScreen(GameplayScreen gameplay)
    {
        _gameplay = gameplay;
    }

    public override bool IsPopup => true;
    public override GameOrientation Orientation => GameOrientation.Landscape;

    public override void Layout()
    {
        const int width = 640;
        const int height = 150;
        float y = Size.Y * 0.44f;
        _resume.Bounds = CentredRect(y, width, height);
#if CH39
        _restart.Bounds = CentredRect(y + 180, width, height);
        _restart.Color = Palette.ButtonSecondary;
        _quit.Bounds = CentredRect(y + 360, width, height);
#else
        _quit.Bounds = CentredRect(y + 180, width, height);
#endif
        _quit.Color = Palette.Danger;
    }

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);

        if (_resume.Update(Input, deltaSeconds) || Input.BackPressed)
        {
            Close();
#if CH39
            _gameplay.Resume();
#endif
        }
#if CH39
        else if (_restart.Update(Input, deltaSeconds))
        {
            _gameplay.Restart();
        }
#endif
        else if (_quit.Update(Input, deltaSeconds))
        {
            Manager.SwitchTo(new MainMenuScreen());
        }
    }

    public override void Draw(GameTime gameTime)
    {
        BeginSpriteBatch();
        SpriteBatch.FillRectangle(Vector2.Zero, Size, Color.Black * 0.65f);
        Fonts.DrawOutlined(SpriteBatch, "PAUSED", new Vector2(Size.X / 2f, Size.Y * 0.2f), 140f, Palette.Gold,
            Color.Black, 6f);
        _resume.Draw(SpriteBatch, Assets);
#if CH39
        _restart.Draw(SpriteBatch, Assets);
#endif
        _quit.Draw(SpriteBatch, Assets);
        SpriteBatch.End();
    }
}
