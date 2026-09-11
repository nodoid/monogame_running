using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.Data;
using MonsterMaze.Gameplay;
using MonsterMaze.Input;

namespace MonsterMaze.UI;

/// <summary>
/// The heads-up display drawn over the maze: score and difficulty top left, Rex's warnings top
/// centre, awards floating up the middle and the pause button top right. Everything stays inside
/// the safe area so notches and rounded corners never cover it.
/// </summary>
public sealed class Hud
{
    private const int MaxAwards = 4;

    private readonly Button _pause = new(null, Icons.Pause);
    private readonly string[] _awardText = new string[MaxAwards];
    private readonly float[] _awardAge = new float[MaxAwards];
    private Rectangle _safeArea;
    private Vector2 _size;
    private int _shownScore = -1;
    private string _scoreText = "0";
    private float _scoreBump;

    public void Layout(Rectangle safeArea, Vector2 size)
    {
        _safeArea = safeArea;
        _size = size;
        _pause.Bounds = new Rectangle(safeArea.Right - 150, safeArea.Top, 150, 150);
        _pause.Color = Color.White * 0.3f;
    }

    public bool PauseTapped(InputManager input, float deltaSeconds) => _pause.Update(input, deltaSeconds);

    /// <summary>Shows a special award such as "+500 NEAR MISS!".</summary>
    public void ShowAward(int points, string reason)
    {
        for (int i = 0; i < MaxAwards; i++)
        {
            if (_awardText[i] == null)
            {
                _awardText[i] = $"+{points:N0}  {reason}";
                _awardAge[i] = 0f;
                return;
            }
        }
    }

    public void Update(float deltaSeconds, int score)
    {
        // Only make a new string when the score actually changes.
        if (score != _shownScore)
        {
            if (_shownScore >= 0 && score > _shownScore)
                _scoreBump = 1f;
            _shownScore = score;
            _scoreText = score.ToString("N0");
        }

        _scoreBump = MathF.Max(0f, _scoreBump - deltaSeconds * 4f);

        for (int i = 0; i < MaxAwards; i++)
        {
            if (_awardText[i] == null)
                continue;
            _awardAge[i] += deltaSeconds;
            if (_awardAge[i] > 2f)
                _awardText[i] = null;
        }
    }

    public void Draw(SpriteBatch spriteBatch, GameAssets assets, WarningSystem warnings, DifficultySettings difficulty,
        Settings settings, float time)
    {
        FontSet fonts = assets.Fonts;
        const bool calm = false;

        // Score and difficulty, top left.
        float left = _safeArea.X;
        fonts.Draw(spriteBatch, "SCORE", new Vector2(left, _safeArea.Y + 22f), 38f, Palette.TextMuted, TextAlign.Left);
        fonts.DrawShadowed(spriteBatch, _scoreText, new Vector2(left, _safeArea.Y + 82f), 76f * (1f + 0.12f * _scoreBump),
            Palette.Gold, TextAlign.Left);
        fonts.DrawShadowed(spriteBatch, difficulty.Name, new Vector2(left, _safeArea.Y + 146f), 38f, difficulty.Color,
            TextAlign.Left);

        // Rex's warning, top centre. New warnings pop in; critical ones flash.
        Warning warning = warnings.Current;
        if (!string.IsNullOrEmpty(warning.Text))
        {
            float appear = MathHelper.Clamp(warnings.TimeShown * 4f, 0f, 1f);
            Color color = warning.Level switch
            {
                WarningLevel.Calm => Palette.Accent,
                WarningLevel.Uneasy => Palette.Gold,
                WarningLevel.Danger => Palette.Warning,
                _ => Palette.Danger
            };
            if (warning.Level == WarningLevel.Critical && !calm && (int)(time * 6f) % 2 == 0)
                color = Color.White;

            float size = 72f * (1f + (1f - appear) * 0.4f);
            fonts.DrawOutlined(spriteBatch, warning.Text, new Vector2(_size.X / 2f, _safeArea.Y + 64f), size,
                color * appear, Color.Black * (0.8f * appear), 4f);
        }

        // Awards float up from the middle of the screen and fade away.
        for (int i = 0; i < MaxAwards; i++)
        {
            if (_awardText[i] == null)
                continue;
            float age = _awardAge[i];
            float alpha = MathHelper.Clamp(2f - age, 0f, 1f);
            var position = new Vector2(_size.X / 2f, _size.Y * 0.42f - age * 90f);
            fonts.DrawOutlined(spriteBatch, _awardText[i], position, 64f, Palette.Gold * alpha, Color.Black * alpha, 3f);
        }

        _pause.Draw(spriteBatch, assets);
    }
}
