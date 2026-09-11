using System;
using Microsoft.Xna.Framework;
using MonsterMaze.UI;

namespace MonsterMaze.Screens;

/// <summary>A placeholder for the high score table.</summary>
public sealed class HighScoreScreen : GameScreen
{
    private readonly Button _back = new("BACK", Icons.Back);

    public override void Layout()
    {
        _back.Bounds = CentredRect(SafeArea.Bottom - 100f, 520, 140);
        _back.Color = Palette.ButtonSecondary;
    }

    public override void Update(GameTime gameTime)
    {
        if (_back.Update(Input, Seconds(gameTime)) || Input.BackPressed)
            Manager.SwitchTo(new MainMenuScreen());
    }

    public override void Draw(GameTime gameTime)
    {
        BeginSpriteBatch();
        DrawBackground(Assets.MenuBackground, new Color(110, 110, 110));
        Fonts.DrawOutlined(SpriteBatch, "HIGH SCORES", new Vector2(Size.X / 2f, SafeArea.Top + 150f), 140f, Palette.Gold,
            Color.Black, 6f);
        Fonts.DrawShadowed(SpriteBatch, "NO SCORES YET", new Vector2(Size.X / 2f, Size.Y / 2f), 72f, Palette.Text);
        _back.Draw(SpriteBatch, Assets);
        SpriteBatch.End();
    }
}
