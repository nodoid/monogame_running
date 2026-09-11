using System;
using Microsoft.Xna.Framework;
using MonsterMaze.Gameplay;
using MonsterMaze.Platform;
using MonsterMaze.UI;

namespace MonsterMaze.Screens;

/// <summary>
/// The game itself, played in landscape. This screen owns the maze, the player and Rex, and
/// wires together every system that makes the game: rendering, AI, warnings, scoring, sound
/// and effects.
/// </summary>
public sealed class GameplayScreen : GameScreen
{
    private readonly GameSession _session;
    private readonly Button _escapeButton = new("ESCAPE", Icons.Exit);
    private readonly Button _eatenButton = new("GET EATEN", Icons.Skull);
    private float _time;
    private bool _gameOver;

    public GameplayScreen(GameSession session)
    {
        _session = session;
    }

    public override GameOrientation Orientation => GameOrientation.Landscape;

    public override void Layout()
    {
        _escapeButton.Bounds = new Rectangle((int)(Size.X / 2f) - 440, SafeArea.Bottom - 150, 420, 150);
        _escapeButton.Color = Palette.Button;
        _eatenButton.Bounds = new Rectangle((int)(Size.X / 2f) + 20, SafeArea.Bottom - 150, 420, 150);
        _eatenButton.Color = Palette.Danger;
    }

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);

        bool pause = Input.BackPressed;
        if (pause && !_gameOver)
        {
            Pause();
            return;
        }

        _time += deltaSeconds;

        // Until the game has a maze to escape from and a monster to be eaten by, two buttons
        // let us try out both endings.
        if (_escapeButton.Update(Input, deltaSeconds))
            EndGame(GameOutcome.Escaped);
        else if (_eatenButton.Update(Input, deltaSeconds))
            EndGame(GameOutcome.Eaten);
    }

    public override void Draw(GameTime gameTime)
    {
        BeginSpriteBatch();
        Fonts.DrawOutlined(SpriteBatch, "GAMEPLAY", new Vector2(Size.X / 2f, Size.Y * 0.35f), 160f, Palette.Accent,
            Color.Black, 6f);
        Fonts.DrawShadowed(SpriteBatch, "THIS SCREEN IS ALWAYS LANDSCAPE", new Vector2(Size.X / 2f, Size.Y * 0.5f), 56f,
            Palette.Text);
        _escapeButton.Draw(SpriteBatch, Assets);
        _eatenButton.Draw(SpriteBatch, Assets);
        SpriteBatch.End();
    }

    private void Pause()
    {
        Manager.Add(new PauseScreen(this));
    }

    private void EndGame(GameOutcome outcome)
    {
        if (_gameOver)
            return;

        _gameOver = true;
        _session.Outcome = outcome;

        GameScreen next = new GameOverScreen(_session);
        Manager.SwitchTo(next);
    }
}
