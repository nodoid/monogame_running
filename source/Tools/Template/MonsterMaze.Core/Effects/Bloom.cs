// @since 31
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonsterMaze.Effects;

/// <summary>
/// Makes bright things glow. We keep only the brightest parts of the scene, shrink them (which is
/// cheap and already blurs a little), blur them properly in two passes, and the post-processing
/// shader adds the result back on top of the scene.
/// </summary>
public sealed class Bloom : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly SpriteBatch _spriteBatch;
    private readonly Effect _effect;
    private RenderTarget2D _half;
    private RenderTarget2D _quarterA;
    private RenderTarget2D _quarterB;

    public Bloom(GraphicsDevice device, SpriteBatch spriteBatch, Effect effect)
    {
        _device = device;
        _spriteBatch = spriteBatch;
        _effect = effect;
    }

    public Texture2D Process(Texture2D scene, float threshold)
    {
        EnsureTargets(scene.Width, scene.Height);

        // 1. Keep only the bright parts, at half size.
        _effect.CurrentTechnique = _effect.Techniques["Extract"];
        _effect.Parameters["Threshold"].SetValue(threshold);
        Draw(scene, _half, _effect);

        // 2. Shrink to a quarter; bilinear filtering averages the pixels as it goes.
        Draw(_half, _quarterA, null);

        // 3. Blur across, then down. Two 1D blurs are much cheaper than one 2D blur.
        _effect.CurrentTechnique = _effect.Techniques["Blur"];
        _effect.Parameters["TexelStep"].SetValue(new Vector2(1f / _quarterA.Width, 0f));
        Draw(_quarterA, _quarterB, _effect);
        _effect.Parameters["TexelStep"].SetValue(new Vector2(0f, 1f / _quarterA.Height));
        Draw(_quarterB, _quarterA, _effect);

        return _quarterA;
    }

    public void Dispose()
    {
        _half?.Dispose();
        _quarterA?.Dispose();
        _quarterB?.Dispose();
    }

    private void Draw(Texture2D source, RenderTarget2D destination, Effect effect)
    {
        _device.SetRenderTarget(destination);
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, effect);
        _spriteBatch.Draw(source, new Rectangle(0, 0, destination.Width, destination.Height), Color.White);
        _spriteBatch.End();
    }

    private void EnsureTargets(int width, int height)
    {
        int halfWidth = Math.Max(1, width / 2);
        int halfHeight = Math.Max(1, height / 2);
        if (_half != null && _half.Width == halfWidth && _half.Height == halfHeight)
            return;

        Dispose();
        _half = new RenderTarget2D(_device, halfWidth, halfHeight);
        _quarterA = new RenderTarget2D(_device, Math.Max(1, width / 4), Math.Max(1, height / 4));
        _quarterB = new RenderTarget2D(_device, Math.Max(1, width / 4), Math.Max(1, height / 4));
    }
}
