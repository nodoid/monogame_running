// @since 09
using Microsoft.Xna.Framework;

namespace MonsterMaze.Rendering;

/// <summary>
/// How the maze grid maps onto the 3D world. Cell (x, y) covers x..x+1 and y..y+1 in cells,
/// laid out along the X and Z axes; Y is up.
/// </summary>
public static class WorldSpace
{
    /// <summary>Width of one cell in world units (think of them as metres).</summary>
    public const float CellSize = 2f;

    public const float WallHeight = 2.2f;

    /// <summary>How high the player's eyes are above the floor.</summary>
    public const float EyeHeight = 1.05f;

    public static Vector3 CellCentre(Point cell, float height = 0f) =>
        new(cell.X * CellSize + CellSize / 2f, height, cell.Y * CellSize + CellSize / 2f);

    /// <summary>Converts a world position into fractional cell coordinates (for the map).</summary>
    public static Vector2 ToCellCoordinates(Vector3 position) =>
        new(position.X / CellSize - 0.5f, position.Z / CellSize - 0.5f);
}
