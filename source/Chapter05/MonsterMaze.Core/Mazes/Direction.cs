using Microsoft.Xna.Framework;

namespace MonsterMaze.Mazes;

/// <summary>The four compass directions. North is "up" on the map, which is -Z in the 3D world.</summary>
public enum Direction
{
    North = 0,
    East = 1,
    South = 2,
    West = 3
}

public static class DirectionExtensions
{
    public static readonly Direction[] All = { Direction.North, Direction.East, Direction.South, Direction.West };

    /// <summary>The step to the neighbouring cell in this direction.</summary>
    public static Point ToOffset(this Direction direction) => direction switch
    {
        Direction.North => new Point(0, -1),
        Direction.East => new Point(1, 0),
        Direction.South => new Point(0, 1),
        _ => new Point(-1, 0)
    };

    public static Direction Opposite(this Direction direction) => (Direction)(((int)direction + 2) & 3);

    public static Direction TurnLeft(this Direction direction) => (Direction)(((int)direction + 3) & 3);

    public static Direction TurnRight(this Direction direction) => (Direction)(((int)direction + 1) & 3);
}
