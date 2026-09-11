using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonsterMaze.Audio;
using MonsterMaze.Gameplay;
using MonsterMaze.UI;

namespace MonsterMaze.Screens;

/// <summary>Three big cards: Easy, Normal and Hard. The last one played is highlighted.</summary>
public sealed class DifficultyScreen : GameScreen
{
    private readonly DifficultySettings[] _levels =
        { DifficultySettings.Easy, DifficultySettings.Normal, DifficultySettings.Hard };

    private readonly Rectangle[] _cards = new Rectangle[3];
    private readonly List<string>[] _descriptions = new List<string>[3];
    private readonly Button _back = new("BACK", Icons.Back);
    private float _time;

    public override void Layout()
    {
        int width = Math.Min(SafeArea.Width, 980);
        const int height = 380;
        const int gap = 44;
        float top = MathF.Max(SafeArea.Top + 250f, Size.Y / 2f - (height * 3 + gap * 2) / 2f);

        for (int i = 0; i < _cards.Length; i++)
        {
            _cards[i] = new Rectangle((int)(Size.X / 2f - width / 2f), (int)(top + i * (height + gap)), width, height);
            _descriptions[i] = Fonts.Wrap(_levels[i].Description, 44f, width - 100f);
        }

        _back.Bounds = CentredRect(SafeArea.Bottom - 80f, 520, 140);
        _back.Color = Palette.ButtonSecondary;
    }

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);
        _time += deltaSeconds;

        for (int i = 0; i < _cards.Length; i++)
        {
            if (Input.WasTapped(_cards[i]))
            {
                Start(_levels[i].Level);
                return;
            }
        }

        if (_back.Update(Input, deltaSeconds) || Input.BackPressed)
            Manager.SwitchTo(new MainMenuScreen());
    }

    private void Start(Difficulty difficulty)
    {
        // Remember the choice, so it's highlighted (and the high score table opens on it) next time.
        Game.Settings.LastDifficulty = difficulty;
        Game.Audio.Play(Sounds.UiSelect);
        var gameplay = new GameplayScreen(GameSession.NewGame(difficulty));
        Manager.SwitchTo(gameplay, TransitionStyle.Wipe);
    }

    public override void Draw(GameTime gameTime)
    {
        BeginSpriteBatch();
        DrawBackground(Assets.MenuBackground, new Color(130, 130, 130));
        Fonts.DrawOutlined(SpriteBatch, "CHOOSE YOUR FATE", new Vector2(Size.X / 2f, SafeArea.Top + 130f), 110f,
            Palette.Gold, Color.Black, 5f);

        for (int i = 0; i < _cards.Length; i++)
        {
            DifficultySettings level = _levels[i];
            Rectangle card = _cards[i];
            bool last = level.Level == Game.Settings.LastDifficulty;
            if (Input.IsTouching(card))
                card.Inflate(-10, -10);

            float glow = last ? 0.6f + 0.4f * MathF.Sin(_time * 4f) : 0.35f;
            NineSlice.Draw(SpriteBatch, Assets.Panel, card, 64, 48, level.Color * glow);

            Fonts.DrawOutlined(SpriteBatch, level.Name, new Vector2(card.X + 50f, card.Y + 75f), 100f, level.Color,
                Color.Black, 4f, TextAlign.Left);
            Fonts.DrawShadowed(SpriteBatch, $"x{level.ScoreMultiplier} SCORE", new Vector2(card.Right - 50f, card.Y + 60f),
                46f, Palette.Gold, TextAlign.Right);
            Fonts.Draw(SpriteBatch, $"{level.MazeWidth} x {level.MazeHeight} MAZE",
                new Vector2(card.Right - 50f, card.Y + 112f), 40f, Palette.TextMuted, TextAlign.Right);

            float y = card.Y + 180f;
            foreach (string line in _descriptions[i])
            {
                Fonts.Draw(SpriteBatch, line, new Vector2(card.X + 50f, y), 44f, Palette.Text, TextAlign.Left);
                y += 54f;
            }

            if (last)
                Fonts.Draw(SpriteBatch, "LAST PLAYED", new Vector2(card.Right - 50f, card.Bottom - 40f), 34f,
                    level.Color, TextAlign.Right);
        }

        _back.Draw(SpriteBatch, Assets);
        SpriteBatch.End();
    }
}
