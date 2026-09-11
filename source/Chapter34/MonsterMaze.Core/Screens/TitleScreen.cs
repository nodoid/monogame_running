using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.UI;
using MonsterMaze.Audio;
using MonsterMaze.Effects;

namespace MonsterMaze.Screens;

/// <summary>The first screen the player sees. Tap anywhere to continue.</summary>
public sealed class TitleScreen : GameScreen
{
    private readonly ParticleSystem2D _embers = new(260);
    private float _emberTimer;
    private float _time;

    public override void Load()
    {
        Game.Audio.PlayMusic(Sounds.TitleMusic);
    }

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);
        _time += deltaSeconds;

        // Embers drift up from the bottom of the screen.
        _emberTimer -= deltaSeconds;
        while (_emberTimer <= 0f)
        {
            _emberTimer += 0.035f;
            var random = _embers.Random;
            var position = new Vector2((float)random.NextDouble() * Size.X, Size.Y + 20f);
            var velocity = new Vector2(((float)random.NextDouble() - 0.5f) * 60f, -120f - (float)random.NextDouble() * 180f);
            _embers.Emit(position, velocity, 3f + (float)random.NextDouble() * 3f, 14f + (float)random.NextDouble() * 18f, 2f,
                new Color(255, 150, 40), new Color(255, 40, 20) * 0f, new Vector2(0f, -10f));
        }

        _embers.Update(deltaSeconds);

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

        BeginSpriteBatch(BlendState.Additive);
        _embers.Draw(SpriteBatch, Assets.ParticleSoft);
        SpriteBatch.End();
    }
}
