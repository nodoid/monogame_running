// @since 38
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.Mazes;
using MonsterMaze.Monster;
using MonsterMaze.Rendering;

namespace MonsterMaze.Gameplay;

/// <summary>
/// The game playing itself behind the title screen, like an arcade machine's attract mode. A
/// computer "pilot" wanders the maze by keeping its right hand on the wall, while Rex hunts it
/// for real. When the pilot escapes or is caught, a fresh random maze is generated.
/// </summary>
public sealed class AttractMode : IDisposable
{
    private readonly MonsterMazeGame _game;
    private readonly MazeRenderer _renderer;
    private readonly RexRenderer _rexRenderer;
    private readonly Camera _camera = new();
    private readonly Queue<PlayerAction> _plan = new();
    private Maze _maze;
    private Player _pilot;
    private Rex _rex;
    private RexBrain _brain;
    private DistanceMap _distances;
    private float _mazeTime;

    public AttractMode(MonsterMazeGame game)
    {
        _game = game;
        _renderer = new MazeRenderer(game.GraphicsDevice, game.Assets)
        {
            Quality = QualityProfile.For(game.Settings.Quality)
        };
        _rexRenderer = new RexRenderer(game.GraphicsDevice, game.Assets);
        NewMaze();
    }

    public void Update(float deltaSeconds)
    {
        _mazeTime += deltaSeconds;

        if (!_pilot.IsBusy)
            _pilot.Queue(NextAction());
        _pilot.Update(deltaSeconds);
        _brain.Update(deltaSeconds, _pilot, _distances);
        _rex.Update(deltaSeconds);

        Vector3 gap = _rex.Position - _pilot.Position;
        gap.Y = 0f;
        if (_pilot.HasEscaped || gap.Length() < 1.1f || _mazeTime > 90f)
            NewMaze();
    }

    public void Draw(GameTime gameTime)
    {
        GraphicsDevice device = _game.GraphicsDevice;
        device.Clear(_renderer.FogColor);

        _camera.FitFieldOfView(device.Viewport.AspectRatio, 75f, 85f);
        _camera.FarPlane = _renderer.FogEnd + 2f;
        _camera.Position = _pilot.Position;
        _camera.Yaw = _pilot.Yaw;
        _camera.Update();

        float seconds = (float)gameTime.TotalGameTime.TotalSeconds;
        _renderer.Draw(_camera, seconds);
        _rexRenderer.Draw(_rex, _camera, _renderer);
    }

    public void Dispose()
    {
        _renderer.Dispose();
        _rexRenderer.Dispose();
    }

    private void NewMaze()
    {
        DifficultySettings settings = DifficultySettings.Normal;
        var random = new Random();
        _maze = MazeGenerator.Generate(settings.MazeWidth, settings.MazeHeight, random.Next(), settings.BraidChance);
        _renderer.SetMaze(_maze);
        _renderer.FogStart = settings.FogStart;
        _renderer.FogEnd = settings.FogEnd;

        _pilot = new Player(_maze, _maze.Start, _maze.StartFacing);
        _distances = new DistanceMap(_maze);
        _distances.Build(_pilot.Cell);
        _pilot.EnteredCell += player =>
        {
            if (_maze.InBounds(player.Cell))
                _distances.Build(player.Cell);
        };

        _rex = new Rex(RexSpawner.ChooseCell(_maze, settings.Rex, random), Direction.South);
        _brain = new RexBrain(_rex, _maze, settings.Rex, random);
        _plan.Clear();
        _mazeTime = 0f;
    }

    /// <summary>
    /// The "right-hand rule": turn right if you can, otherwise go straight, otherwise turn left,
    /// otherwise turn round. It isn't clever, but in a maze it always makes progress.
    /// </summary>
    private PlayerAction NextAction()
    {
        if (_plan.Count > 0)
            return _plan.Dequeue();

        Direction facing = _pilot.Facing;
        if (IsOpen(facing.TurnRight()))
        {
            _plan.Enqueue(PlayerAction.Forward);
            return PlayerAction.TurnRight;
        }

        if (IsOpen(facing))
            return PlayerAction.Forward;

        if (IsOpen(facing.TurnLeft()))
        {
            _plan.Enqueue(PlayerAction.Forward);
            return PlayerAction.TurnLeft;
        }

        _plan.Enqueue(PlayerAction.TurnRight);
        return PlayerAction.TurnRight;
    }

    private bool IsOpen(Direction direction) =>
        _maze.CanMove(_pilot.Cell, direction) || _maze.IsExitStep(_pilot.Cell, direction);
}
