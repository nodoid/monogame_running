using System;
using Microsoft.Xna.Framework;
using MonsterMaze.Diagnostics;
using MonsterMaze.Gameplay;
using MonsterMaze.Mazes;
using MonsterMaze.Platform;
using MonsterMaze.Rendering;
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
    private readonly Button _newMazeButton = new("NEW MAZE", Icons.Star);
    private readonly MazeMapRenderer _map = new();
    private readonly DebugOverlay _debug = new();
    private float _time;
    private bool _gameOver;
    private Maze _maze;
    private DistanceMap _startDistances;

    public GameplayScreen(GameSession session)
    {
        _session = session;
    }

    public override GameOrientation Orientation => GameOrientation.Landscape;

    public override void Load()
    {
        BuildMaze(_session.Seed);
    }

    public override void Layout()
    {
        _escapeButton.Bounds = new Rectangle((int)(Size.X / 2f) - 440, SafeArea.Bottom - 150, 420, 150);
        _escapeButton.Color = Palette.Button;
        _eatenButton.Bounds = new Rectangle((int)(Size.X / 2f) + 20, SafeArea.Bottom - 150, 420, 150);
        _eatenButton.Color = Palette.Danger;
        _newMazeButton.Bounds = new Rectangle(SafeArea.Right - 460, SafeArea.Top, 460, 140);
        FitMap();
    }

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);
        _debug.Update(Input);

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

        if (_newMazeButton.Update(Input, deltaSeconds))
        {
            // A new seed means a completely new maze.
            BuildMaze(Random.Shared.Next());
            FitMap();
        }
        UpdateDebugText();
    }

    public override void Draw(GameTime gameTime)
    {
        BeginSpriteBatch();
        DrawMap();
        _escapeButton.Draw(SpriteBatch, Assets);
        _eatenButton.Draw(SpriteBatch, Assets);
        _newMazeButton.Draw(SpriteBatch, Assets);
        _debug.Draw(SpriteBatch, Fonts, SafeArea);
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

    private void BuildMaze(int seed)
    {
        _maze = MazeGenerator.Generate(15, 15, seed, 0.15f);
        _startDistances = new DistanceMap(_maze);
        _startDistances.Build(_maze.Start);
    }

    private void FitMap()
    {
        _map.Fit(_maze, new Rectangle(SafeArea.X, SafeArea.Top + 120, SafeArea.Width, SafeArea.Height - 300));
    }

    private void DrawMap()
    {
        _map.DrawBackground(SpriteBatch, _maze, Color.Black * 0.75f);
        _map.DrawDistances(SpriteBatch, _startDistances, 0.75f);
        _map.DrawWalls(SpriteBatch, _maze, Color.White, MathF.Max(2f, _map.CellSize * 0.12f));
        _map.DrawMarker(SpriteBatch, _maze.Start.ToVector2(), Palette.Accent);
        _map.DrawMarker(SpriteBatch, _maze.Exit.ToVector2(), Palette.Gold);
        Fonts.DrawShadowed(SpriteBatch, $"SEED {_maze.Seed}   {_maze.Width} x {_maze.Height}   LONGEST WALK {_startDistances.MaxDistance}",
            new Vector2(Size.X / 2f, SafeArea.Top + 60f), 52f, Palette.Text);
    }

    /// <summary>Fills the developer overlay (three-finger tap or M) with facts about the game.</summary>
    private void UpdateDebugText()
    {
        _debug.Clear();
        if (!_debug.Visible)
            return;

        _debug.Add($"SEED {_maze.Seed}  SIZE {_maze.Width}x{_maze.Height}");
        _debug.Add($"START {_maze.Start}  EXIT {_maze.Exit} {_maze.ExitSide}");
    }
}
