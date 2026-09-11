// @since 34
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.Audio;
using MonsterMaze.Effects;
using MonsterMaze.Gameplay;
using MonsterMaze.UI;

namespace MonsterMaze.Screens;

/// <summary>
/// Game over, the good way. Out in the sunshine the bonuses are added up one at a time, with a
/// ticking counter and a shower of confetti.
/// </summary>
public sealed class EscapedScreen : GameScreen
{
    private const float TallySeconds = 2.2f;

    private readonly GameSession _session;
    private readonly Button _continue = new("CONTINUE", Icons.Play);
    private readonly ParticleSystem2D _confetti = new(500);
    private float _time;
    private float _tickTimer;
    private int _shownScore;

    public EscapedScreen(GameSession session)
    {
        _session = session;
    }

    private bool TallyDone => _shownScore >= _session.Score;

    public override void Layout()
    {
        _continue.Bounds = CentredRect(SafeArea.Bottom - 130f, 720, 160);
        _continue.Color = Palette.Button;
    }

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);
        _time += deltaSeconds;

        // Count the score up, ticking as it goes. A tap skips straight to the end.
        if (!TallyDone)
        {
            float progress = MathHelper.Clamp((_time - 0.6f) / TallySeconds, 0f, 1f);
            _shownScore = (int)(_session.Score * progress);
            _tickTimer -= deltaSeconds;
            if (progress > 0f && _tickTimer <= 0f)
            {
                _tickTimer = 0.05f;
                Game.Audio.Play(Sounds.ScoreTick, 0.6f, progress * 0.5f);
            }

            if (Input.AnyTap || Input.ConfirmPressed)
                _shownScore = _session.Score;
            if (TallyDone)
                Game.Audio.Play(Sounds.UiSelect);
        }
        else if (_continue.Update(Input, deltaSeconds) || Input.ConfirmPressed || Input.BackPressed)
        {
            ScoreFlow.Continue(Manager, _session);
            return;
        }

        _continue.Visible = TallyDone;

        // Confetti rains down from above.
        var random = _confetti.Random;
        for (int i = 0; i < 3; i++)
        {
            var position = new Vector2((float)random.NextDouble() * Size.X, -30f);
            var velocity = new Vector2(((float)random.NextDouble() - 0.5f) * 120f, 150f + (float)random.NextDouble() * 200f);
            _confetti.Emit(position, velocity, 5f, 30f, 24f, Palette.Rainbow((float)random.NextDouble()),
                Palette.Rainbow((float)random.NextDouble()) * 0.5f, new Vector2(0f, 40f), ((float)random.NextDouble() - 0.5f) * 10f);
        }

        _confetti.Update(deltaSeconds);
    }

    public override void Draw(GameTime gameTime)
    {
        BeginSpriteBatch();
        DrawBackground(Assets.EscapeSky, Color.White);

        float bounce = MathF.Abs(MathF.Sin(_time * 3f)) * 16f;
        Fonts.DrawOutlined(SpriteBatch, "YOU ESCAPED!", new Vector2(Size.X / 2f, Size.Y * 0.18f - bounce), 150f,
            Palette.Rainbow(_time * 0.3f), Color.Black, 7f);

        int exploration = _session.Score - _session.EscapeBonus - _session.TimeBonus;
        float y = Size.Y * 0.36f;
        DrawLine("EXPLORATION", exploration, 0.6f, ref y);
        DrawLine("ESCAPE BONUS", _session.EscapeBonus, 1.2f, ref y);
        DrawLine("SPEED BONUS", _session.TimeBonus, 1.8f, ref y);

        y += 50f;
        Fonts.DrawShadowed(SpriteBatch, "FINAL SCORE", new Vector2(Size.X / 2f, y), 56f, Palette.Text);
        Fonts.DrawOutlined(SpriteBatch, _shownScore.ToString("N0"), new Vector2(Size.X / 2f, y + 110f),
            TallyDone ? 160f + MathF.Sin(_time * 6f) * 6f : 150f, Palette.Gold, Color.Black, 6f);

        _continue.Draw(SpriteBatch, Assets);
        SpriteBatch.End();

        SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null,
            Game.Scaler.Transform);
        _confetti.Draw(SpriteBatch, Assets.ParticleDust);
        SpriteBatch.End();
    }

    /// <summary>Each line of the tally appears in turn.</summary>
    private void DrawLine(string label, int points, float appearAt, ref float y)
    {
        float alpha = MathHelper.Clamp((_time - appearAt) * 3f, 0f, 1f);
        if (TallyDone)
            alpha = 1f;
        float left = Size.X / 2f - 420f;
        float right = Size.X / 2f + 420f;
        Fonts.DrawShadowed(SpriteBatch, label, new Vector2(left, y), 56f, Palette.Text * alpha, TextAlign.Left);
        Fonts.DrawShadowed(SpriteBatch, points.ToString("N0"), new Vector2(right, y), 60f, Palette.Gold * alpha,
            TextAlign.Right);
        y += 100f;
    }
}
