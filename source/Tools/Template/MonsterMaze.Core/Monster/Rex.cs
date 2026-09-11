// @since 16
using System;
using Microsoft.Xna.Framework;
using MonsterMaze.Mazes;
using MonsterMaze.Rendering;

namespace MonsterMaze.Monster;

public enum RexAnimation
{
    Idle,
    Walk,
    Roar,
    Lunge
}

/// <summary>
/// Rex's body: where he is, where he's heading and which animation frame to show. The decisions
/// about where to go live in <c>RexBrain</c>; this class only carries them out.
/// </summary>
public sealed class Rex
{
    // Frames in the sprite sheet (4 columns x 2 rows).
    public const int FrameIdleA = 0;
    public const int FrameIdleB = 1;
    public const int FrameWalkFirst = 2;
    public const int FrameRoar = 6;
    public const int FrameLunge = 7;

    private float _animationTime;
    private int _lastWalkFrame = -1;
#if CH18
    private float _walkCycle;
    private float _moveTime;
    private float _moveDuration;
    private Vector3 _moveFrom;
    private Vector3 _moveTo;
#endif

    public Rex(Point cell, Direction facing)
    {
        Cell = cell;
        FromCell = cell;
        Facing = facing;
        Position = WorldSpace.CellCentre(cell);
    }

    /// <summary>The cell Rex occupies, or is walking into.</summary>
    public Point Cell { get; private set; }

    public Point FromCell { get; private set; }
    public Direction Facing { get; private set; }

    /// <summary>The point on the floor between Rex's feet.</summary>
    public Vector3 Position { get; private set; }

    public RexAnimation Animation { get; private set; } = RexAnimation.Idle;

    /// <summary>The sprite sheet frame to draw.</summary>
    public int Frame { get; private set; }

    /// <summary>He rears up larger as he lunges.</summary>
    public float Scale => Animation == RexAnimation.Lunge ? 1.25f : 1f;
#if CH18

    public bool IsMoving => _moveTime < _moveDuration;
#endif

    /// <summary>Raised each time one of Rex's feet hits the ground.</summary>
    public event Action<Rex> Footstep;

    public void Play(RexAnimation animation)
    {
        if (Animation == animation)
            return;
        Animation = animation;
        _animationTime = 0f;
    }
#if CH18

    /// <summary>Walks to a neighbouring cell over the given time.</summary>
    public void MoveTo(Point cell, float seconds)
    {
        Point step = cell - Cell;
        foreach (Direction direction in DirectionExtensions.All)
        {
            if (direction.ToOffset() == step)
                Facing = direction;
        }

        FromCell = Cell;
        Cell = cell;
        _moveFrom = Position;
        _moveTo = WorldSpace.CellCentre(cell);
        _moveTime = 0f;
        _moveDuration = seconds;
        Play(RexAnimation.Walk);
    }
#endif

    public void Update(float deltaSeconds)
    {
        _animationTime += deltaSeconds;
#if CH18

        if (IsMoving)
        {
            _moveTime += deltaSeconds;
            Position = Vector3.Lerp(_moveFrom, _moveTo, MathHelper.Clamp(_moveTime / _moveDuration, 0f, 1f));

            // Half a walk cycle (one footstep) per cell, whatever his speed.
            _walkCycle += deltaSeconds / _moveDuration * 0.5f;
        }
#endif

        switch (Animation)
        {
            case RexAnimation.Idle:
                // Slow breathing: swap between the two idle frames.
                Frame = (int)(_animationTime / 0.7f) % 2 == 0 ? FrameIdleA : FrameIdleB;
                break;

            case RexAnimation.Walk:
#if CH18
                int frame = FrameWalkFirst + (int)(_walkCycle * 4f) % 4;
#else
                int frame = FrameWalkFirst;
#endif
                if (frame != _lastWalkFrame && (frame == FrameWalkFirst || frame == FrameWalkFirst + 2))
                    Footstep?.Invoke(this);
                _lastWalkFrame = frame;
                Frame = frame;
                break;

            case RexAnimation.Roar:
                Frame = FrameRoar;
                break;

            case RexAnimation.Lunge:
                Frame = FrameLunge;
                break;
        }
    }
}
