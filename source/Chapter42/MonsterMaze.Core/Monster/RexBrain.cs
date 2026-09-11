using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonsterMaze.Gameplay;
using MonsterMaze.Mazes;

namespace MonsterMaze.Monster;

public enum RexState
{
    /// <summary>Waiting in the dark before the hunt begins.</summary>
    Lurking,

    /// <summary>Roaming the maze, drifting towards where the player might be.</summary>
    Hunting,

    /// <summary>He can hear the player and follows the sound.</summary>
    Stalking,

    /// <summary>He has seen the player and runs straight for them.</summary>
    Charging,

    /// <summary>He has caught the player. Game over.</summary>
    Attacking
}

/// <summary>
/// Rex's decision making, as a finite state machine. Each state has its own behaviour and its own
/// rules for moving to another state. Decisions are made whenever Rex arrives in a new cell.
/// </summary>
public sealed class RexBrain
{
    private readonly Rex _rex;
    private readonly Maze _maze;
    private readonly RexTuning _tuning;
    private readonly Random _random;
    private readonly Pathfinder _pathfinder;
    private readonly List<Point> _path = new();
    private int _pathIndex;
    private float _pause;
    private float _lostSightTime;

    public RexBrain(Rex rex, Maze maze, RexTuning tuning, Random random)
    {
        _rex = rex;
        _maze = maze;
        _tuning = tuning;
        _random = random;
        _pathfinder = new Pathfinder(maze);
    }

    public RexState State { get; private set; } = RexState.Lurking;

    /// <summary>Seconds spent in the current state.</summary>
    public float StateTime { get; private set; }

    public bool CanSeePlayer { get; private set; }
    public bool CanHearPlayer { get; private set; }

    public event Action<RexState> StateChanged;

    /// <summary>Raised when Rex stops to sniff the air.</summary>
    public event Action Sniffed;

    public void Update(float deltaSeconds, Player player, DistanceMap fromPlayer)
    {
        StateTime += deltaSeconds;
        if (State == RexState.Attacking)
            return;

        CanSeePlayer = RexSenses.CanSee(_maze, _rex.Cell, player.Cell, _tuning.SightRange);
        CanHearPlayer = RexSenses.CanHear(fromPlayer, _rex.Cell, _tuning.HearingRange);

        // First decide whether to change state...
        switch (State)
        {
            case RexState.Lurking:
                if (StateTime >= _tuning.LurkSeconds || player.StepsTaken >= _tuning.LurkSteps)
                    ChangeState(RexState.Hunting);
                break;

            case RexState.Hunting:
                if (CanSeePlayer)
                    ChangeState(RexState.Charging);
                else if (CanHearPlayer)
                    ChangeState(RexState.Stalking);
                break;

            case RexState.Stalking:
                if (CanSeePlayer)
                    ChangeState(RexState.Charging);
                else if (fromPlayer[_rex.Cell] > _tuning.HearingRange + 3)
                    ChangeState(RexState.Hunting);
                break;

            case RexState.Charging:
                if (CanSeePlayer)
                {
                    _lostSightTime = 0f;
                }
                else
                {
                    _lostSightTime += deltaSeconds;
                    if (_lostSightTime > _tuning.LoseSightSeconds)
                        ChangeState(RexState.Stalking);
                }

                break;
        }

        // ...then, if he is standing still, decide where to go next.
        if (_rex.IsMoving)
            return;
        if (_pause > 0f)
        {
            _pause -= deltaSeconds;
            return;
        }

        switch (State)
        {
            case RexState.Lurking:
                _rex.Play(RexAnimation.Idle);
                break;
            case RexState.Hunting:
                Wander(fromPlayer);
                break;
            case RexState.Stalking:
                StepTowardPlayer(fromPlayer, _tuning.StalkStepSeconds);
                break;
            case RexState.Charging:
                StepTowardPlayer(fromPlayer, _tuning.ChargeStepSeconds);
                break;
        }
    }

    /// <summary>Called by the game when Rex reaches the player.</summary>
    public void Attack()
    {
        ChangeState(RexState.Attacking);
        _rex.Play(RexAnimation.Lunge);
    }

    private void ChangeState(RexState state)
    {
        State = state;
        StateTime = 0f;
        _lostSightTime = 0f;
        _path.Clear();
        _pathIndex = 0;

        switch (state)
        {
            case RexState.Hunting:
                // Wake up with a sniff.
                _pause = 0.8f;
                _rex.Play(RexAnimation.Idle);
                Sniffed?.Invoke();
                break;

            case RexState.Charging:
                // Roar before charging. This moment is the player's chance to run.
                _pause = _tuning.SpotPauseSeconds;
                _rex.Play(RexAnimation.Roar);
                break;
        }

        StateChanged?.Invoke(state);
    }

    private void StepTowardPlayer(DistanceMap fromPlayer, float secondsPerCell)
    {
        // The distance map already knows the shortest way to the player from every cell.
        if (fromPlayer.TryStepTowardOrigin(_rex.Cell, out Direction direction))
            _rex.MoveTo(_rex.Cell + direction.ToOffset(), secondsPerCell);
        else
            _rex.Play(RexAnimation.Idle);
    }

    private void Wander(DistanceMap fromPlayer)
    {
        // Every so often, stop and sniff the air, which keeps him unpredictable.
        if (_random.NextDouble() < _tuning.SniffChance)
        {
            _pause = 1f + (float)_random.NextDouble();
            _rex.Play(RexAnimation.Idle);
            Sniffed?.Invoke();
            return;
        }

        if (_pathIndex >= _path.Count)
        {
            // Pick somewhere to search. Favour cells in the player's part of the maze, so the
            // hunt slowly closes in without Rex knowing exactly where the player is.
            Point target = RandomCell();
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Point candidate = RandomCell();
                if (fromPlayer[candidate] <= _tuning.HearingRange * 2)
                {
                    target = candidate;
                    break;
                }
            }

            _pathIndex = 0;
            if (!_pathfinder.FindPath(_rex.Cell, target, _path) || _path.Count == 0)
            {
                _pause = 0.5f;
                return;
            }
        }

        _rex.MoveTo(_path[_pathIndex++], _tuning.WanderStepSeconds);
    }

    private Point RandomCell() => new(_random.Next(_maze.Width), _random.Next(_maze.Height));
}
