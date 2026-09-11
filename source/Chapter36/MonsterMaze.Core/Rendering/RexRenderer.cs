using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.Monster;

namespace MonsterMaze.Rendering;

/// <summary>
/// Draws Rex as a "billboard": a flat, high-resolution picture that always turns to face the
/// camera. Because it is drawn in the 3D world, walls hide him properly and the fog swallows
/// him in the distance, so he looms out of the darkness as he approaches.
/// </summary>
public sealed class RexRenderer : IDisposable
{
    private const float Height = 2.15f;
    private const float Width = Height * 512f / 640f;

    private static readonly short[] Indices = { 0, 1, 2, 0, 2, 3 };

    private readonly GraphicsDevice _device;
    private readonly BasicEffect _effect;
    private readonly VertexPositionTexture[] _quad = new VertexPositionTexture[4];

    public RexRenderer(GraphicsDevice device, GameAssets assets)
    {
        _device = device;
        _effect = new BasicEffect(device)
        {
            TextureEnabled = true,
            Texture = assets.RexSheet,
            LightingEnabled = false,
            VertexColorEnabled = false
        };
    }

    public void Draw(Rex rex, Camera camera, MazeRenderer maze)
    {
        // Turn to face the camera around the vertical axis only, so Rex stays upright.
        Vector3 toCamera = camera.Position - rex.Position;
        toCamera.Y = 0f;
        if (toCamera.LengthSquared() < 0.0001f)
            toCamera = -camera.Forward;
        toCamera.Normalize();
        Vector3 right = Vector3.Cross(Vector3.Up, toCamera);

        Vector3 feet = rex.Position;
        if (rex.Animation == RexAnimation.Lunge)
            feet += toCamera * 0.35f;

        float halfWidth = Width * rex.Scale / 2f;
        Vector3 up = Vector3.Up * Height * rex.Scale;

        // Pick the frame's rectangle out of the 4 x 2 sprite sheet.
        float u0 = rex.Frame % 4 / 4f;
        float v0 = rex.Frame / 4 / 2f;
        float u1 = u0 + 0.25f;
        float v1 = v0 + 0.5f;

        _quad[0] = new VertexPositionTexture(feet - right * halfWidth, new Vector2(u0, v1));
        _quad[1] = new VertexPositionTexture(feet - right * halfWidth + up, new Vector2(u0, v0));
        _quad[2] = new VertexPositionTexture(feet + right * halfWidth + up, new Vector2(u1, v0));
        _quad[3] = new VertexPositionTexture(feet + right * halfWidth, new Vector2(u1, v1));

        _device.BlendState = BlendState.AlphaBlend;
        _device.DepthStencilState = DepthStencilState.DepthRead;
        _device.RasterizerState = RasterizerState.CullNone;
        _device.SamplerStates[0] = SamplerState.LinearClamp;

        _effect.World = Matrix.Identity;
        _effect.View = camera.View;
        _effect.Projection = camera.Projection;

        // Light and fog Rex exactly like the walls around him.
        _effect.FogEnabled = true;
        _effect.FogStart = maze.FogStart;
        _effect.FogEnd = maze.FogEnd;
        _effect.FogColor = maze.FogColor.ToVector3();
        _effect.DiffuseColor = maze.TorchColor * maze.Brightness;
        _effect.CurrentTechnique.Passes[0].Apply();
        _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _quad, 0, 4, Indices, 0, 2);
    }

    public void Dispose() => _effect.Dispose();
}
