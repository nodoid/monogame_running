using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.Diagnostics;
using MonsterMaze.Gameplay;
using MonsterMaze.Input;
using MonsterMaze.Mazes;
using MonsterMaze.Monster;
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
    private static readonly RexTuning Tuning = new();

    private readonly GameSession _session;
    private readonly MazeMapRenderer _map = new();
    private readonly DebugOverlay _debug = new();
    private readonly Camera _camera = new();
    private readonly TouchControls _controls = new();
    private float _time;
    private bool _gameOver;
    private Maze _maze;
    private MazeRenderer _renderer;
    private Player _player;
    private Rex _rex;
    private RexRenderer _rexRenderer;
    private DistanceMap _distances;

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
        _renderer.Quality = QualityProfile.For(Game.Settings.Quality);
        _player = new Player(_maze, _maze.Start, _maze.StartFacing);
        _player.EnteredCell += OnPlayerEnteredCell;
        _player.Bumped += OnPlayerBumped;
        _controls.Scheme = Game.Settings.Controls;
        _rex = new Rex(ChooseRexCell(), Direction.South);
        _rexRenderer = new RexRenderer(GraphicsDevice, Assets);
        _distances = new DistanceMap(_maze);
        _distances.Build(_player.Cell);
    }

    public override void Unload()
    {
        _renderer.Dispose();
        _rexRenderer.Dispose();
    }

    public override void Layout()
    {
        FitMap();
        _controls.Layout(SafeArea);
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

        UpdatePlayer(deltaSeconds);
        if (_gameOver)
            return;
        UpdateRex(deltaSeconds);
        UpdateDebugText();
    }

    public override void Draw(GameTime gameTime)
    {
        DrawWorld(gameTime);

        BeginSpriteBatch();
        DrawMap();
        if (!_gameOver && !_player.Frozen)
            _controls.Draw(SpriteBatch, Assets, Size);
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
        _map.DrawDistances(SpriteBatch, _distances, 0.75f);
        _map.DrawWalls(SpriteBatch, _maze, Color.White, MathF.Max(2f, _map.CellSize * 0.12f));
        _map.DrawMarker(SpriteBatch, _maze.Start.ToVector2(), Palette.Accent);
        _map.DrawMarker(SpriteBatch, _maze.Exit.ToVector2(), Palette.Gold);
        _map.DrawArrow(SpriteBatch, WorldSpace.ToCellCoordinates(_player.Position), _player.Yaw, Color.White);
        _map.DrawMarker(SpriteBatch, WorldSpace.ToCellCoordinates(_rex.Position), Palette.Danger, 0.7f);
    }

    /// <summary>Fills the developer overlay (three-finger tap or M) with facts about the game.</summary>
    private void UpdateDebugText()
    {
        _debug.Clear();
        if (!_debug.Visible)
            return;

        _debug.Add($"SEED {_maze.Seed}  SIZE {_maze.Width}x{_maze.Height}");
        _debug.Add($"START {_maze.Start}  EXIT {_maze.Exit} {_maze.ExitSide}");
        _debug.Add($"QUALITY {Game.Settings.Quality}  TRIANGLES {_renderer.Mesh.TriangleCount}");
        _debug.Add($"PLAYER {_player.Cell} FACING {_player.Facing}  STEPS {_player.StepsTaken}");
        _debug.Add($"REX {_rex.Cell}  {_distances[_rex.Cell]} STEPS AWAY");
        _debug.Add($"SEES {RexSenses.CanSee(_maze, _rex.Cell, _player.Cell, Tuning.SightRange)}  " +
                   $"HEARS {RexSenses.CanHear(_distances, _rex.Cell, Tuning.HearingRange)}");
    }

    private void DrawWorld(GameTime gameTime)
    {
        float seconds = (float)gameTime.TotalGameTime.TotalSeconds;
        GraphicsDevice.Clear(_renderer.FogColor);
        float aspectRatio = GraphicsDevice.Viewport.AspectRatio;

        PositionCamera(aspectRatio);
        _renderer.Draw(_camera, seconds);
        _rexRenderer.Draw(_rex, _camera, _renderer);
    }

    private void PositionCamera(float aspectRatio)
    {
        _camera.FitFieldOfView(aspectRatio);
        _camera.FarPlane = _renderer.FogEnd + 2f;
        _camera.Position = _player.Position;
        _camera.Yaw = _player.Yaw;
        _camera.Update();
    }

    private void UpdatePlayer(float deltaSeconds)
    {
        _player.Queue(_controls.Poll(Input, deltaSeconds));
        _player.Update(deltaSeconds);

        if (_player.HasEscaped && !_gameOver)
        {
            EndGame(GameOutcome.Escaped);
        }
    }

    private void OnPlayerEnteredCell(Player player)
    {
        // Sound travels along corridors, so re-measure walking distances from the player's new cell.
        if (_maze.InBounds(player.Cell))
            _distances.Build(player.Cell);
    }

    private void OnPlayerBumped(Player player)
    {
        if (Game.Settings.Haptics)
            Game.Platform.Vibrate(HapticStrength.Light);
    }

    private Point ChooseRexCell()
    {
        // For now, Rex waits in the dead end farthest from the start.
        var distances = new DistanceMap(_maze);
        distances.Build(_maze.Start);
        return distances.FindFarthest(_maze.IsDeadEnd);
    }

    private void UpdateRex(float deltaSeconds)
    {
        // Rex can't move yet, but he roars when he sees the player.
        bool seesPlayer = RexSenses.CanSee(_maze, _rex.Cell, _player.Cell, Tuning.SightRange);
        _rex.Play(seesPlayer ? RexAnimation.Roar : RexAnimation.Idle);
        _rex.Update(deltaSeconds);
    }
}
