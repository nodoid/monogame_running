using System;
using Microsoft.Xna.Framework;
using MonsterMaze.Mazes;
using MonsterMaze.Monster;

namespace MonsterMaze.Gameplay;

public enum WarningLevel
{
    Calm,
    Uneasy,
    Danger,
    Critical
}

public readonly record struct Warning(string Text, WarningLevel Level);

/// <summary>
/// The original game kept the player informed (and terrified) with messages about Rex. We do the
/// same, and also work out a single "danger" number that sound and screen effects can follow.
/// </summary>
public sealed class WarningSystem
{
    public static readonly Warning None = new("", WarningLevel.Calm);
    public static readonly Warning LiesInWait = new("REX LIES IN WAIT", WarningLevel.Calm);
    public static readonly Warning Hunting = new("HE IS HUNTING FOR YOU", WarningLevel.Uneasy);
    public static readonly Warning Footsteps = new("FOOTSTEPS APPROACHING", WarningLevel.Danger);
    public static readonly Warning Seen = new("REX HAS SEEN YOU", WarningLevel.Critical);
    public static readonly Warning Behind = new("RUN, HE IS BEHIND YOU", WarningLevel.Critical);

    public Warning Current { get; private set; } = LiesInWait;

    /// <summary>How long the current warning has been showing.</summary>
    public float TimeShown { get; private set; }

    /// <summary>0 when Rex is far away, rising to 1 when he's about to catch the player.</summary>
    public float Danger { get; private set; }

    /// <summary>"Footsteps approaching" appears when Rex is this many steps away or closer.</summary>
    public int FootstepRange { get; set; } = 6;

    public event Action<Warning> Changed;

    public void Update(float deltaSeconds, RexBrain brain, Rex rex, Player player, DistanceMap fromPlayer)
    {
        int steps = fromPlayer[rex.Cell];
        bool inFront = RexSenses.IsInFront(player.Cell, player.Facing, rex.Cell);

        Warning next;
        switch (brain.State)
        {
            case RexState.Lurking:
                next = LiesInWait;
                break;

            case RexState.Charging:
            case RexState.Attacking:
                next = inFront ? Seen : Behind;
                break;

            default:
                if (steps <= 2 && !inFront)
                    next = Behind;
                else if (steps <= FootstepRange && brain.State == RexState.Stalking)
                    next = Footsteps;
                else
                    next = Hunting;
                break;
        }

        TimeShown += deltaSeconds;
        if (next != Current)
        {
            Current = next;
            TimeShown = 0f;
            Changed?.Invoke(next);
        }

        // Danger rises as Rex gets closer, is damped while he lurks and jumps when he charges.
        float target = steps == DistanceMap.Unreachable ? 0f : MathHelper.Clamp(1f - (steps - 1) / 10f, 0f, 1f);
        if (brain.State == RexState.Lurking)
            target *= 0.3f;
        if (brain.State is RexState.Charging or RexState.Attacking)
            target = MathF.Max(target, 0.75f);

        // Ease towards the target so effects swell and fade rather than jumping.
        Danger = MathHelper.Lerp(Danger, target, 1f - MathF.Exp(-deltaSeconds * 3f));
    }
}
