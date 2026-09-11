using System;
using Microsoft.Xna.Framework;
using MonsterMaze.Mazes;
using MonsterMaze.Rendering;

namespace MonsterMaze.Gameplay;

public enum PlayerAction
{
    None,
    Forward,
    Back,
    TurnLeft,
    TurnRight
}

/// <summary>
/// The player moves one whole cell or turns a quarter-turn at a time, just like the original
/// game, but each move is animated smoothly. One extra action can be queued while a move is
/// playing, so quick taps never get lost.
/// </summary>
public sealed class Player
{
    public const float StepSeconds = 0.3f;
    public const float TurnSeconds = 0.22f;
    public const float BumpSeconds = 0.2f;

    private const float BobHeight = 0.045f;

    private enum Motion
    {
        None,
        Step,
        Turn,
        Bump
    }

    private readonly Maze _maze;
    private Motion _motion;
    private float _time;
    private float _duration;
    private Vector3 _from;
    private Vector3 _to;
    private float _fromYaw;
    private float _toYaw;
    private bool _footstepPlayed;
    private PlayerAction _queued;

    public Player(Maze maze, Point cell, Direction facing)
    {
        _maze = maze;
        Cell = cell;
        PreviousCell = cell;
        Facing = facing;
        Yaw = facing.ToYaw();
        Position = EyePosition(cell);
    }

    /// <summary>The cell the player is in (or moving into).</summary>
    public Point Cell { get; private set; }

    public Point PreviousCell { get; private set; }
    public Direction Facing { get; private set; }

    /// <summary>The position of the player's eyes, animated between cells.</summary>
    public Vector3 Position { get; private set; }

    /// <summary>Which way the player is looking, animated during turns.</summary>
    public float Yaw { get; private set; }

    public bool IsBusy => _motion != Motion.None;
    public bool IsStepping => _motion == Motion.Step;
    public int StepsTaken { get; private set; }

    /// <summary>When frozen the player ignores input (used by cut-scenes).</summary>
    public bool Frozen { get; set; }

    /// <summary>Raised halfway through each step, when the foot hits the ground.</summary>
    public event Action<Player> Footstep;

    /// <summary>Raised when a step finishes and the player is standing in a new cell.</summary>
    public event Action<Player> EnteredCell;

    /// <summary>Raised when the player walks into a wall.</summary>
    public event Action<Player> Bumped;

    public static Vector3 EyePosition(Point cell) => WorldSpace.CellCentre(cell, WorldSpace.EyeHeight);

    public void Queue(PlayerAction action)
    {
        if (Frozen || action == PlayerAction.None)
            return;

        if (IsBusy)
            _queued = action;
        else
            Begin(action);
    }

    public void Update(float deltaSeconds)
    {
        if (_motion == Motion.None)
            return;

        _time += deltaSeconds;
        float progress = MathHelper.Clamp(_time / _duration, 0f, 1f);
        float eased = MathHelper.SmoothStep(0f, 1f, progress);

        switch (_motion)
        {
            case Motion.Step:
                // A little up-and-down bob makes each step feel like a footfall.
                float bob = MathF.Sin(progress * MathHelper.Pi) * BobHeight;
                Position = Vector3.Lerp(_from, _to, eased) + Vector3.Up * bob;
                if (!_footstepPlayed && progress >= 0.5f)
                {
                    _footstepPlayed = true;
                    Footstep?.Invoke(this);
                }

                break;

            case Motion.Turn:
                Yaw = MathHelper.Lerp(_fromYaw, _toYaw, eased);
                break;

            case Motion.Bump:
                // Lurch towards the wall and back again.
                Position = Vector3.Lerp(_from, _to, MathF.Sin(progress * MathHelper.Pi));
                break;
        }

        if (progress < 1f)
            return;

        Motion finished = _motion;
        _motion = Motion.None;
        if (finished == Motion.Step)
        {
            Position = _to;
            EnteredCell?.Invoke(this);
        }
        else if (finished == Motion.Bump)
        {
            Position = _from;
        }

        if (_queued != PlayerAction.None && !Frozen)
        {
            PlayerAction next = _queued;
            _queued = PlayerAction.None;
            Begin(next);
        }
    }

    private void Begin(PlayerAction action)
    {
        _time = 0f;
        switch (action)
        {
            case PlayerAction.TurnLeft:
            case PlayerAction.TurnRight:
                bool left = action == PlayerAction.TurnLeft;
                Facing = left ? Facing.TurnLeft() : Facing.TurnRight();
                _fromYaw = Yaw;
                _toYaw = Yaw + (left ? -MathHelper.PiOver2 : MathHelper.PiOver2);
                _duration = TurnSeconds;
                _motion = Motion.Turn;
                break;

            case PlayerAction.Forward:
            case PlayerAction.Back:
                Direction direction = action == PlayerAction.Forward ? Facing : Facing.Opposite();
                _from = EyePosition(Cell);
                bool canMove = _maze.CanMove(Cell, direction);
                if (canMove)
                {
                    PreviousCell = Cell;
                    Cell += direction.ToOffset();
                    _to = EyePosition(Cell);
                    _duration = StepSeconds;
                    _motion = Motion.Step;
                    _footstepPlayed = false;
                    StepsTaken++;
                }
                else
                {
                    _to = _from + direction.ToVector3() * 0.25f;
                    _duration = BumpSeconds;
                    _motion = Motion.Bump;
                    _queued = PlayerAction.None;
                    Bumped?.Invoke(this);
                }

                break;
        }
    }
}
