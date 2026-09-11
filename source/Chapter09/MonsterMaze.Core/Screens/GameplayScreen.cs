using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    private readonly MazeMapRenderer _map = new();
    private readonly DebugOverlay _debug = new();
    private readonly Camera _camera = new();
    private float _time;
    private bool _gameOver;
    private Maze _maze;
    private DistanceMap _startDistances;
    private MazeRenderer _renderer;

    public GameplayScreen(GameSession session)
    {
        _session = session;
    }

    public override GameOrientation Orientation => GameOrientation.Landscape;

    public override void Load()
    {
        BuildMaze(_session.Seed);
        _renderer = new MazeRenderer(GraphicsDevice, Assets);
        _renderer.SetMaze(_maze);
    }

    public override void Unload()
    {
        _renderer.Dispose();
    }

    public override void Layout()
    {
        _escapeButton.Bounds = new Rectangle((int)(Size.X / 2f) - 440, SafeArea.Bottom - 150, 420, 150);
        _escapeButton.Color = Palette.Button;
        _eatenButton.Bounds = new Rectangle((int)(Size.X / 2f) + 20, SafeArea.Bottom - 150, 420, 150);
        _eatenButton.Color = Palette.Danger;
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
        UpdateDebugText();
    }

    public override void Draw(GameTime gameTime)
    {
        DrawWorld(gameTime);

        BeginSpriteBatch();
        DrawMap();
        _escapeButton.Draw(SpriteBatch, Assets);
        _eatenButton.Draw(SpriteBatch, Assets);
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
        // Once there is a 3D view, the map becomes a small developer's overlay.
        int size = (int)(Size.Y * 0.45f);
        _map.Fit(_maze, new Rectangle(SafeArea.Right - size, SafeArea.Top + 170, size, size));
    }

    private void DrawMap()
    {
        if (!_debug.Visible)
            return;
        _map.DrawBackground(SpriteBatch, _maze, Color.Black * 0.75f);
        _map.DrawDistances(SpriteBatch, _startDistances, 0.75f);
        _map.DrawWalls(SpriteBatch, _maze, Color.White, MathF.Max(2f, _map.CellSize * 0.12f));
        _map.DrawMarker(SpriteBatch, _maze.Start.ToVector2(), Palette.Accent);
        _map.DrawMarker(SpriteBatch, _maze.Exit.ToVector2(), Palette.Gold);
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

    private void DrawWorld(GameTime gameTime)
    {
        float seconds = (float)gameTime.TotalGameTime.TotalSeconds;
        GraphicsDevice.Clear(new Color(12, 8, 20));
        float aspectRatio = GraphicsDevice.Viewport.AspectRatio;

        // Circle slowly above the maze to admire it.
        var centre = new Vector3(_maze.Width, 0f, _maze.Height) * WorldSpace.CellSize / 2f;
        float radius = MathF.Max(_maze.Width, _maze.Height) * WorldSpace.CellSize * 0.75f;
        _camera.AspectRatio = aspectRatio;
        _camera.FarPlane = radius * 4f;
        _camera.LookAt(centre + new Vector3(MathF.Sin(_time * 0.25f) * radius, radius, MathF.Cos(_time * 0.25f) * radius),
            centre);
        _renderer.Draw(_camera, seconds);
    }
}
