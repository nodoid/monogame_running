using Microsoft.Xna.Framework;

namespace MonsterMaze.UI;

/// <summary>Indices of the glyphs in UI/icons.png (an 8 x 2 grid of 128-pixel cells).</summary>
public static class Icons
{
    public const int CellSize = 128;

    public const int Up = 0;
    public const int Down = 1;
    public const int TurnLeft = 2;
    public const int TurnRight = 3;
    public const int Pause = 4;
    public const int Play = 5;
    public const int Sound = 6;
    public const int Mute = 7;
    public const int Gear = 8;
    public const int Trophy = 9;
    public const int Skull = 10;
    public const int Exit = 11;
    public const int Back = 12;
    public const int Check = 13;
    public const int Star = 14;
    public const int Keyboard = 15;

    public static Rectangle Source(int index) =>
        new((index % 8) * CellSize, (index / 8) * CellSize, CellSize, CellSize);
}
