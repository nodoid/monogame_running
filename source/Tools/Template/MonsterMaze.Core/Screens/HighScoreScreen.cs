// @since 04
using System;
using Microsoft.Xna.Framework;
#if CH35
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.Audio;
using MonsterMaze.Data;
using MonsterMaze.Effects;
using MonsterMaze.Gameplay;
#endif
using MonsterMaze.UI;

namespace MonsterMaze.Screens;
#if CH35

/// <summary>
/// The hall of fame: the top six scores for each difficulty, in full colour. The title ripples
/// through the rainbow, the rows slide in, and every score strobes through bright colours. A
/// score just achieved glows and throws off sparks.
/// </summary>
public sealed class HighScoreScreen : GameScreen
{
    private const string Title = "HIGH SCORES";
    private const float StrobeHz = 10f;

    private static readonly Color[] StrobeColors =
    {
        new(255, 60, 170), new(40, 230, 255), new(255, 240, 60), new(120, 255, 90),
        new(255, 140, 30), new(190, 110, 255), Color.White
    };

    private static readonly string[] Ranks = { "1ST", "2ND", "3RD", "4TH", "5TH", "6TH" };

    private static readonly Color[] RankColors =
    {
        new(255, 210, 60), new(215, 225, 240), new(230, 145, 70),
        new(110, 200, 255), new(150, 255, 170), new(255, 150, 220)
    };

    private readonly Difficulty? _requestedDifficulty;
    private readonly int _highlightRank;
    private readonly Button[] _tabs = new Button[3];
    private readonly Button _back = new("BACK", Icons.Back);
    private readonly Rectangle[] _rows = new Rectangle[HighScoreTable.MaxEntries];
    private readonly string[] _scoreText = new string[HighScoreTable.MaxEntries];
    private readonly string[] _titleLetters = new string[Title.Length];
    private readonly ParticleSystem2D _sparkles = new(400);
#if CH38
    private readonly bool _attract;
#endif
    private Difficulty _difficulty;
    private float _time;
    private float _tabTime;
    private float _sparkleTimer;

    public HighScoreScreen(Difficulty? difficulty = null, int highlightRank = -1
#if CH38
        , bool attract = false
#endif
    )
    {
        _requestedDifficulty = difficulty;
        _highlightRank = highlightRank;
#if CH38
        _attract = attract;
#endif
        for (int i = 0; i < Title.Length; i++)
            _titleLetters[i] = Title[i].ToString();
    }

    private IReadOnlyList<HighScoreEntry> Entries => Game.HighScores[_difficulty];

    public override void Load()
    {
        _difficulty = _requestedDifficulty ?? Game.Settings.LastDifficulty;
        foreach (Difficulty difficulty in Enum.GetValues<Difficulty>())
            _tabs[(int)difficulty] = new Button(DifficultySettings.For(difficulty).Name) { TextSize = 50f };

        if (_highlightRank >= 0)
            Game.Audio.Play(Sounds.NewHighScore);
        else
            Game.Audio.PlayMusic(Sounds.TitleMusic);
        RefreshScores();
    }

    public override void Layout()
    {
        int tabWidth = Math.Min(320, (SafeArea.Width - 40) / 3);
        float tabY = SafeArea.Top + 330f;
        for (int i = 0; i < _tabs.Length; i++)
            _tabs[i].Bounds = new Rectangle((int)(Size.X / 2f + (i - 1) * (tabWidth + 20) - tabWidth / 2f),
                (int)tabY - 60, tabWidth, 120);

        int rowWidth = Math.Min(SafeArea.Width, 1000);
        const int rowHeight = 170;
        float top = tabY + 110f;
        float available = SafeArea.Bottom - 190f - top;
        float spacing = MathF.Min(rowHeight + 24f, available / HighScoreTable.MaxEntries);
        for (int i = 0; i < _rows.Length; i++)
            _rows[i] = new Rectangle((int)(Size.X / 2f - rowWidth / 2f), (int)(top + i * spacing), rowWidth,
                (int)MathF.Min(rowHeight, spacing - 16f));

        _back.Bounds = CentredRect(SafeArea.Bottom - 80f, 520, 140);
        _back.Color = Palette.ButtonSecondary;
    }

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);
        _time += deltaSeconds;
        _tabTime += deltaSeconds;
#if CH38

        // In attract mode the table shows for a while, then hands back to the title screen.
        if (_attract && (_time > 12f || Input.AnyTap))
        {
            Manager.SwitchTo(new TitleScreen());
            return;
        }
#endif

        for (int i = 0; i < _tabs.Length; i++)
        {
            var level = DifficultySettings.For((Difficulty)i);
            _tabs[i].Color = (Difficulty)i == _difficulty ? level.Color : level.Color * 0.35f;
            if (_tabs[i].Update(Input, deltaSeconds) && (Difficulty)i != _difficulty)
            {
                _difficulty = (Difficulty)i;
                _tabTime = 0f;
                RefreshScores();
            }
        }

        if (_back.Update(Input, deltaSeconds) || Input.BackPressed)
        {
            Manager.SwitchTo(new MainMenuScreen());
            return;
        }

        EmitSparkles(deltaSeconds);
        _sparkles.Update(deltaSeconds);
    }

    public override void Draw(GameTime gameTime)
    {
#if CH30
        bool calm = Game.Settings.ReduceFlashing;
#else
        const bool calm = false;
#endif

        BeginSpriteBatch();
        DrawBackground(Assets.MenuBackground, new Color(110, 110, 110));
        DrawRainbowTitle(new Vector2(Size.X / 2f, SafeArea.Top + 150f));

        foreach (Button tab in _tabs)
            tab.Draw(SpriteBatch, Assets);

        IReadOnlyList<HighScoreEntry> entries = Entries;
        for (int i = 0; i < _rows.Length; i++)
            DrawRow(i, i < entries.Count ? entries[i] : null, calm);

        Fonts.Draw(SpriteBatch, $"THE TOP {HighScoreTable.MaxEntries} ON {DifficultySettings.For(_difficulty).Name}",
            new Vector2(Size.X / 2f, _rows[_rows.Length - 1].Bottom + 50f), 40f, Palette.TextMuted);
#if CH38
        if (!_attract)
            _back.Draw(SpriteBatch, Assets);
#else
        _back.Draw(SpriteBatch, Assets);
#endif
        SpriteBatch.End();

        // Glow and sparks, added on top so they brighten whatever is underneath.
        BeginSpriteBatch(BlendState.Additive);
        for (int i = 0; i < Math.Min(entries.Count, _rows.Length); i++)
        {
            Color strobe = StrobeColor(i, calm, out float flash);
            Rectangle row = RowBounds(i);
            var glowCentre = new Vector2(row.Right - 190f, row.Center.Y);
            SpriteBatch.Draw(Assets.ParticleSoft, glowCentre, null, strobe * (0.35f * flash), 0f, new Vector2(32f),
                new Vector2(9f, 3f), SpriteEffects.None, 0f);
        }

        _sparkles.Draw(SpriteBatch, Assets.ParticleSpark);
        SpriteBatch.End();
    }

    private void DrawRow(int index, HighScoreEntry entry, bool calm)
    {
        Rectangle row = RowBounds(index);
        bool highlighted = index == _highlightRank && _difficulty == (_requestedDifficulty ?? _difficulty);
        Color rankColor = RankColors[index];

        // The newest score's panel pulses so the player can find it at a glance.
        float pulse = highlighted ? 0.5f + 0.5f * MathF.Sin(_time * 8f) : 0f;
        NineSlice.Draw(SpriteBatch, Assets.Panel, row, 64, 40, Color.Lerp(rankColor, Color.White, pulse));

        float middle = row.Center.Y;
        Fonts.DrawOutlined(SpriteBatch, Ranks[index], new Vector2(row.X + 40f, middle), 64f, rankColor, Color.Black, 3f,
            TextAlign.Left);

        if (entry == null)
        {
            Fonts.Draw(SpriteBatch, "- - -", new Vector2(row.Center.X, middle), 56f, Palette.TextMuted);
            return;
        }

        // Names cycle gently through the rainbow; each row is a little further round the wheel.
        Color nameColor = calm ? Palette.Text : Palette.FromHsv(_time * 0.12f + index * 0.14f, 0.35f, 1f);
        Fonts.DrawShadowed(SpriteBatch, entry.Name, new Vector2(row.X + 200f, middle), 62f, nameColor, TextAlign.Left);

        // The strobe: the score flicks through bright colours ten times a second.
        Color strobe = StrobeColor(index, calm, out float flash);
        Fonts.DrawOutlined(SpriteBatch, _scoreText[index], new Vector2(row.Right - 110f, middle), 70f,
            new Color(strobe.ToVector3() * flash), Color.Black, 4f, TextAlign.Right);

        int icon = entry.Outcome == GameOutcome.Escaped ? Icons.Exit : Icons.Skull;
        Color iconColor = entry.Outcome == GameOutcome.Escaped ? Palette.Accent : Palette.Danger;
        SpriteBatch.Draw(Assets.Icons, new Vector2(row.Right - 55f, middle), Icons.Source(icon), iconColor, 0f,
            new Vector2(Icons.CellSize / 2f), 0.5f, SpriteEffects.None, 0f);
    }

    /// <summary>Rows slide in from the right one after another whenever the table changes.</summary>
    private Rectangle RowBounds(int index)
    {
        float t = MathHelper.Clamp((_tabTime - index * 0.07f) / 0.35f, 0f, 1f);
        float slide = (1f - t) * (1f - t) * Size.X;
        Rectangle row = _rows[index];
        row.X += (int)slide;
        return row;
    }

    private Color StrobeColor(int index, bool calm, out float flash)
    {
        if (calm)
        {
            // Reduced flashing: a slow, smooth colour cycle instead of a strobe.
            flash = 1f;
            return Palette.Rainbow(_time * 0.15f + index * 0.12f);
        }

        float speed = index == _highlightRank ? StrobeHz * 1.5f : StrobeHz;
        int step = (int)(_time * speed + index * 0.7f);
        flash = step % 2 == 0 ? 1f : 0.55f;
        return StrobeColors[(step + index) % StrobeColors.Length];
    }

    private void DrawRainbowTitle(Vector2 centre)
    {
        const float size = 150f;
        float width = Fonts.Measure(Title, size).X;
        float x = centre.X - width / 2f;

        for (int i = 0; i < _titleLetters.Length; i++)
        {
            string letter = _titleLetters[i];
            float letterWidth = Fonts.Measure(letter, size).X;
            float bounce = MathF.Sin(_time * 4f + i * 0.5f) * 12f;
            Color color = Palette.Rainbow(_time * 0.4f - i * 0.07f);
            Fonts.DrawOutlined(SpriteBatch, letter, new Vector2(x + letterWidth / 2f, centre.Y + bounce), size, color,
                Color.Black, 6f);
            x += letterWidth;
        }
    }

    private void EmitSparkles(float deltaSeconds)
    {
        _sparkleTimer -= deltaSeconds;
        if (_sparkleTimer > 0f)
            return;
        _sparkleTimer = 0.04f;

        // Sparks fly off the title, and off the newest score if there is one.
        Vector2 source = new Vector2(Size.X / 2f, SafeArea.Top + 150f) + _sparkles.RandomOffset(420f) * new Vector2(1f, 0.2f);
        _sparkles.Emit(source, _sparkles.RandomOffset(120f), 0.9f, 40f, 0f, Palette.Rainbow((float)_sparkles.Random.NextDouble()),
            Color.Transparent, new Vector2(0f, 200f), 3f);

        if (_highlightRank >= 0 && _highlightRank < _rows.Length)
        {
            Rectangle row = RowBounds(_highlightRank);
            for (int i = 0; i < 3; i++)
            {
                Vector2 edge = new Vector2(row.X + (float)_sparkles.Random.NextDouble() * row.Width,
                    _sparkles.Random.Next(2) == 0 ? row.Top : row.Bottom);
                _sparkles.Emit(edge, _sparkles.RandomOffset(160f), 0.8f, 46f, 0f, Palette.Rainbow((float)_sparkles.Random.NextDouble()),
                    Color.Transparent, Vector2.Zero, 4f);
            }
        }
    }

    private void RefreshScores()
    {
        IReadOnlyList<HighScoreEntry> entries = Entries;
        for (int i = 0; i < _scoreText.Length; i++)
            _scoreText[i] = i < entries.Count ? entries[i].Score.ToString("N0") : "";
    }
}
#else

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
#endif
