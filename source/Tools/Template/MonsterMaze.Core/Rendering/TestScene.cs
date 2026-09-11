// @since 08 @until 08
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonsterMaze.Rendering;

/// <summary>
/// Our first 3D scene: a spinning, multi-coloured cube hovering over a chequered floor. It shows
/// vertices, index buffers, BasicEffect and the world, view and projection matrices at work.
/// </summary>
public sealed class TestScene
{
    private readonly GraphicsDevice _device;
    private readonly BasicEffect _effect;
    private readonly VertexPositionColor[] _cube;
    private readonly short[] _cubeIndices;
    private readonly VertexPositionColor[] _floor;

    public TestScene(GraphicsDevice device)
    {
        _device = device;
        _effect = new BasicEffect(device) { VertexColorEnabled = true };

        // Eight corners, each a different colour; the GPU blends the colours across each face.
        _cube = new[]
        {
            new VertexPositionColor(new Vector3(-1, -1, -1), Color.Red),
            new VertexPositionColor(new Vector3(1, -1, -1), Color.Orange),
            new VertexPositionColor(new Vector3(1, 1, -1), Color.Yellow),
            new VertexPositionColor(new Vector3(-1, 1, -1), Color.Lime),
            new VertexPositionColor(new Vector3(-1, -1, 1), Color.Cyan),
            new VertexPositionColor(new Vector3(1, -1, 1), Color.Blue),
            new VertexPositionColor(new Vector3(1, 1, 1), Color.Magenta),
            new VertexPositionColor(new Vector3(-1, 1, 1), Color.White)
        };

        // Twelve triangles, two per face. Front faces wind clockwise when seen from outside.
        _cubeIndices = new short[]
        {
            4, 6, 5, 4, 7, 6, // front  (+Z)
            1, 3, 0, 1, 2, 3, // back   (-Z)
            0, 7, 4, 0, 3, 7, // left   (-X)
            5, 2, 1, 5, 6, 2, // right  (+X)
            3, 6, 7, 3, 2, 6, // top    (+Y)
            0, 5, 1, 0, 4, 5  // bottom (-Y)
        };

        // A 10 x 10 chequerboard made of triangle lists.
        const int tiles = 10;
        _floor = new VertexPositionColor[tiles * tiles * 6];
        int v = 0;
        for (int z = 0; z < tiles; z++)
        {
            for (int x = 0; x < tiles; x++)
            {
                Color color = (x + z) % 2 == 0 ? new Color(90, 60, 120) : new Color(40, 30, 60);
                var a = new Vector3(x - tiles / 2, -2f, z - tiles / 2);
                var b = a + new Vector3(1, 0, 0);
                var c = a + new Vector3(1, 0, 1);
                var d = a + new Vector3(0, 0, 1);
                _floor[v++] = new VertexPositionColor(d, color);
                _floor[v++] = new VertexPositionColor(a, color);
                _floor[v++] = new VertexPositionColor(b, color);
                _floor[v++] = new VertexPositionColor(d, color);
                _floor[v++] = new VertexPositionColor(b, color);
                _floor[v++] = new VertexPositionColor(c, color);
            }
        }
    }

    public void Draw(Camera camera, float seconds)
    {
        _device.BlendState = BlendState.Opaque;
        _device.DepthStencilState = DepthStencilState.Default;
        _device.RasterizerState = RasterizerState.CullCounterClockwise;

        _effect.View = camera.View;
        _effect.Projection = camera.Projection;

        _effect.World = Matrix.Identity;
        _effect.CurrentTechnique.Passes[0].Apply();
        _device.DrawUserPrimitives(PrimitiveType.TriangleList, _floor, 0, _floor.Length / 3);

        // Spin the cube around two axes at different speeds.
        _effect.World = Matrix.CreateRotationY(seconds) * Matrix.CreateRotationX(seconds * 0.6f);
        _effect.CurrentTechnique.Passes[0].Apply();
        _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _cube, 0, _cube.Length, _cubeIndices, 0, 12);
    }
}
