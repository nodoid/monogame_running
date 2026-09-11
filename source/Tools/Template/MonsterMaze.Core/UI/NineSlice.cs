// @since 04
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonsterMaze.UI;

/// <summary>
/// Draws a texture stretched to any size without distorting its corners. The texture is cut
/// into a 3 x 3 grid: corners stay the same size, edges stretch in one direction and the
/// middle stretches in both.
/// </summary>
public static class NineSlice
{
    public static void Draw(SpriteBatch spriteBatch, Texture2D texture, Rectangle destination, int sourceBorder,
        int destinationBorder, Color color)
    {
        int border = Math.Min(destinationBorder, Math.Min(destination.Width, destination.Height) / 2);

        Span<int> sourceX = stackalloc int[] { 0, sourceBorder, texture.Width - sourceBorder, texture.Width };
        Span<int> sourceY = stackalloc int[] { 0, sourceBorder, texture.Height - sourceBorder, texture.Height };
        Span<int> destX = stackalloc int[]
            { destination.Left, destination.Left + border, destination.Right - border, destination.Right };
        Span<int> destY = stackalloc int[]
            { destination.Top, destination.Top + border, destination.Bottom - border, destination.Bottom };

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                var source = new Rectangle(sourceX[x], sourceY[y], sourceX[x + 1] - sourceX[x], sourceY[y + 1] - sourceY[y]);
                var target = new Rectangle(destX[x], destY[y], destX[x + 1] - destX[x], destY[y + 1] - destY[y]);
                if (target.Width > 0 && target.Height > 0)
                    spriteBatch.Draw(texture, target, source, color);
            }
        }
    }
}
