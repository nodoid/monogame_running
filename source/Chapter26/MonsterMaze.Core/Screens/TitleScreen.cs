using System;
using Microsoft.Xna.Framework;
using MonsterMaze.UI;
using MonsterMaze.Audio;

namespace MonsterMaze.Screens;

/// <summary>The first screen the player sees. Tap anywhere to continue.</summary>
public sealed class TitleScreen : GameScreen
{
    private float _time;

    public override void Load()
    {
        Game.Audio.PlayMusic(Sounds.TitleMusic);
    }

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);
        _time += deltaSeconds;

        if (Input.AnyTap || Input.ConfirmPressed)
        {
            Manager.SwitchTo(new MainMenuScreen());
            return;
        }

        // The Android back button leaves the game from here. iOS apps never quit themselves.
        if (Input.BackPressed && !OperatingSystem.IsIOS())
            Game.Exit();
    }

    public override void Draw(GameTime gameTime)
    {
        BeginSpriteBatch();
        DrawBackground(Assets.MenuBackground, Color.White);
        float bob = MathF.Sin(_time * 2f) * 10f;
        Fonts.DrawOutlined(SpriteBatch, "MONSTER", new Vector2(Size.X / 2f, Size.Y * 0.25f + bob), 230f, Palette.Accent,
            Color.Black, 8f);
        Fonts.DrawOutlined(SpriteBatch, "MAZE", new Vector2(Size.X / 2f, Size.Y * 0.36f + bob), 270f, Palette.Gold,
            Color.Black, 8f);

        float pulse = 0.55f + 0.45f * MathF.Sin(_time * 4f);
        Fonts.DrawShadowed(SpriteBatch, "TAP TO PLAY", new Vector2(Size.X / 2f, Size.Y * 0.72f), 84f, Color.White * pulse);
        Fonts.Draw(SpriteBatch, "INSPIRED BY THE CLASSIC 3D MONSTER MAZE",
            new Vector2(Size.X / 2f, SafeArea.Bottom - 30f), 34f, Palette.TextMuted);
        SpriteBatch.End();
    }
}
