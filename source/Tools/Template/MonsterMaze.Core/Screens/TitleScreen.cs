// @since 04
using System;
using Microsoft.Xna.Framework;
#if CH32 || CH38
using Microsoft.Xna.Framework.Graphics;
#endif
using MonsterMaze.UI;
#if CH26
using MonsterMaze.Audio;
#endif
#if CH32
using MonsterMaze.Effects;
#endif
#if CH38
using MonsterMaze.Gameplay;
#endif

namespace MonsterMaze.Screens;

/// <summary>The first screen the player sees. Tap anywhere to continue.</summary>
public sealed class TitleScreen : GameScreen
{
#if CH38
    /// <summary>Left alone this long, the title cycles to the high score table.</summary>
    private const float AttractSeconds = 30f;

    private AttractMode _attract;
#endif
#if CH32
    private readonly ParticleSystem2D _embers = new(260);
    private float _emberTimer;
#endif
    private float _time;

#if CH26 || CH38

    public override void Load()
    {
#if CH26
        Game.Audio.PlayMusic(Sounds.TitleMusic);
#endif
#if CH38
        _attract = new AttractMode(Game);
#endif
    }
#endif
#if CH38

    public override void Unload()
    {
        _attract.Dispose();
    }
#endif

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);
        _time += deltaSeconds;
#if CH38

        _attract.Update(deltaSeconds);
        if (_time > AttractSeconds)
        {
            Manager.SwitchTo(new HighScoreScreen(attract: true));
            return;
        }
#endif
#if CH32

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
#endif

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
#if CH38
        _attract.Draw(gameTime);
#endif
        BeginSpriteBatch();
#if CH38

        // Darken the attract mode behind the logo.
        SpriteBatch.FillRectangle(Vector2.Zero, Size, Color.Black * 0.3f);
        SpriteBatch.Draw(Assets.Vignette, new Rectangle(0, 0, (int)Size.X + 1, (int)Size.Y + 1), Color.Black * 0.95f);
        float logoScale = Size.X * 0.92f / Assets.Logo.Width * (1f + 0.02f * MathF.Sin(_time * 2f));
        SpriteBatch.Draw(Assets.Logo, new Vector2(Size.X / 2f, Size.Y * 0.28f), null, Color.White, 0f,
            new Vector2(Assets.Logo.Width, Assets.Logo.Height) / 2f, logoScale, SpriteEffects.None, 0f);
#else
        DrawBackground(Assets.MenuBackground, Color.White);
        float bob = MathF.Sin(_time * 2f) * 10f;
        Fonts.DrawOutlined(SpriteBatch, "MONSTER", new Vector2(Size.X / 2f, Size.Y * 0.25f + bob), 230f, Palette.Accent,
            Color.Black, 8f);
        Fonts.DrawOutlined(SpriteBatch, "MAZE", new Vector2(Size.X / 2f, Size.Y * 0.36f + bob), 270f, Palette.Gold,
            Color.Black, 8f);
#endif

        float pulse = 0.55f + 0.45f * MathF.Sin(_time * 4f);
        Fonts.DrawShadowed(SpriteBatch, "TAP TO PLAY", new Vector2(Size.X / 2f, Size.Y * 0.72f), 84f, Color.White * pulse);
        Fonts.Draw(SpriteBatch, "INSPIRED BY THE CLASSIC 3D MONSTER MAZE",
            new Vector2(Size.X / 2f, SafeArea.Bottom - 30f), 34f, Palette.TextMuted);
        SpriteBatch.End();
#if CH32

        BeginSpriteBatch(BlendState.Additive);
        _embers.Draw(SpriteBatch, Assets.ParticleSoft);
        SpriteBatch.End();
#endif
    }
}
