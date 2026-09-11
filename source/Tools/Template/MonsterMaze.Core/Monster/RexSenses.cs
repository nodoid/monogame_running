// @since 17
using Microsoft.Xna.Framework;
using MonsterMaze.Mazes;

namespace MonsterMaze.Monster;

/// <summary>What Rex can see and hear. Both senses work on the maze grid.</summary>
public static class RexSenses
{
    /// <summary>
    /// Sight: in a maze you can only see along a straight corridor, so Rex sees the player when
    /// they share a row or column with no wall in between and are within range.
    /// </summary>
    public static bool CanSee(Maze maze, Point from, Point to, int range)
    {
        if (from == to)
            return true;
        if (from.X != to.X && from.Y != to.Y)
            return false;

        Direction direction = from.X == to.X
            ? (to.Y < from.Y ? Direction.North : Direction.South)
            : (to.X < from.X ? Direction.West : Direction.East);

        Point cell = from;
        for (int steps = 0; steps < range; steps++)
        {
            if (maze.HasWall(cell, direction))
                return false;
            cell += direction.ToOffset();
            if (cell == to)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Hearing: sound travels along the corridors, so what matters is the walking distance
    /// between Rex and the player, not the straight-line distance through the walls.
    /// </summary>
    public static bool CanHear(DistanceMap fromPlayer, Point rexCell, int range) => fromPlayer[rexCell] <= range;

    /// <summary>True if <paramref name="target"/> is somewhere ahead of someone facing this way.</summary>
    public static bool IsInFront(Point cell, Direction facing, Point target)
    {
        Point offset = target - cell;
        Point forward = facing.ToOffset();
        return offset.X * forward.X + offset.Y * forward.Y > 0;
    }
}
