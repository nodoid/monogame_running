// @since 33
using System;
using Microsoft.Xna.Framework;
using MonsterMaze.Gameplay;
using MonsterMaze.UI;

namespace MonsterMaze.Screens;

/// <summary>Game over, the hard way. Shows how long the player lasted and their final score.</summary>
public sealed class EatenScreen : GameScreen
{
    private readonly GameSession _session;
    private readonly Button _continue = new("CONTINUE", Icons.Play);
    private float _time;

    public EatenScreen(GameSession session)
    {
        _session = session;
    }

    public override void Layout()
    {
        _continue.Bounds = CentredRect(SafeArea.Bottom - 130f, 720, 160);
        _continue.Color = Palette.Danger;
    }

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);
        _time += deltaSeconds;

        // Wait a moment before accepting input, so a panicked tap doesn't skip the screen.
        _continue.Visible = _time > 1.2f;
        if (_continue.Update(Input, deltaSeconds) || (_time > 1.2f && (Input.ConfirmPressed || Input.BackPressed)))
            ScoreFlow.Continue(Manager, _session);
    }

    public override void Draw(GameTime gameTime)
    {
        BeginSpriteBatch();
        DrawBackground(Assets.EatenBackground, Color.White);

        // The title shakes violently at first and settles down.
        float shake = MathF.Max(0f, 1f - _time) * 18f;
        var jitter = new Vector2(MathF.Sin(_time * 97f), MathF.Cos(_time * 89f)) * shake;

        Fonts.DrawShadowed(SpriteBatch, "YOU HAVE BEEN", new Vector2(Size.X / 2f, Size.Y * 0.2f) + jitter, 80f, Palette.Text);

        // "EATEN!" drips: fainter copies slide slowly down behind the word.
        var eatenPosition = new Vector2(Size.X / 2f, Size.Y * 0.29f) + jitter;
        for (int i = 3; i >= 1; i--)
        {
            float drip = (_time * 14f + i * 9f) % 40f;
            Fonts.Draw(SpriteBatch, "EATEN!", eatenPosition + new Vector2(0f, drip + i * 6f), 230f,
                new Color(120, 0, 0) * (0.25f * (4 - i) / 3f));
        }

        Fonts.DrawOutlined(SpriteBatch, "EATEN!", eatenPosition, 230f, Palette.Danger, Color.Black, 8f);

        float y = Size.Y * 0.46f;
        int minutes = (int)(_session.Seconds / 60f);
        int seconds = (int)_session.Seconds % 60;
        DrawStat("SURVIVED", $"{minutes}:{seconds:00}", ref y);
        DrawStat("CELLS EXPLORED", _session.CellsExplored.ToString(), ref y);
        DrawStat("NEAR MISSES", _session.NearMisses.ToString(), ref y);

        y += 40f;
        Fonts.DrawShadowed(SpriteBatch, "FINAL SCORE", new Vector2(Size.X / 2f, y), 56f, Palette.TextMuted);
        Fonts.DrawOutlined(SpriteBatch, _session.Score.ToString("N0"), new Vector2(Size.X / 2f, y + 110f), 150f,
            Palette.Gold, Color.Black, 6f);

        _continue.Draw(SpriteBatch, Assets, MathHelper.Clamp((_time - 1.2f) * 3f, 0f, 1f));
        SpriteBatch.End();
    }

    private void DrawStat(string label, string value, ref float y)
    {
        float left = Size.X / 2f - 400f;
        float right = Size.X / 2f + 400f;
        Fonts.Draw(SpriteBatch, label, new Vector2(left, y), 52f, Palette.Text, TextAlign.Left);
        Fonts.DrawShadowed(SpriteBatch, value, new Vector2(right, y), 60f, Palette.Gold, TextAlign.Right);
        y += 90f;
    }
}
