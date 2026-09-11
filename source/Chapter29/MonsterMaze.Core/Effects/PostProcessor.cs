using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.Rendering;

namespace MonsterMaze.Effects;

/// <summary>
/// Draws the 3D scene into an off-screen render target instead of straight to the screen, then
/// copies it to the screen through our PostProcess shader. That final pass is where every
/// full-screen effect happens: vignette, colour grading, bloom, blur and chromatic aberration.
/// </summary>
public sealed class PostProcessor : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly SpriteBatch _spriteBatch;
    private readonly Effect _effect;
    private readonly Texture2D _black;
    private RenderTarget2D _scene;
    private QualityProfile _quality;

    public PostProcessor(GraphicsDevice device, SpriteBatch spriteBatch, GameAssets assets, QualityProfile quality)
    {
        _device = device;
        _spriteBatch = spriteBatch;
        _effect = assets.PostProcessEffect;
        _quality = quality;
        _black = new Texture2D(device, 1, 1);
        _black.SetData(new[] { Color.Black });
    }

    public float VignetteIntensity { get; set; } = 0.45f;
    public Color VignetteColor { get; set; } = Color.Black;
    public float Saturation { get; set; } = 1f;
    public float Contrast { get; set; } = 1f;
    public float Brightness { get; set; }
    public Vector3 Tint { get; set; } = Vector3.One;

    public QualityProfile Quality
    {
        get => _quality;
        set
        {
            if (_quality == value)
                return;
            _quality = value;
            DisposeTargets();
        }
    }

    /// <summary>Redirects all drawing into the off-screen scene target.</summary>
    public void BeginScene()
    {
        EnsureTargets();
        _device.SetRenderTarget(_scene);
    }

    /// <summary>Draws the finished scene to the screen through the post-processing shader.</summary>
    public void EndScene()
    {
        _device.SetRenderTarget(null);
        Texture2D blur = _black;
        Texture2D bloom = _black;
        _device.SetRenderTarget(null);

        _effect.Parameters["BloomTexture"].SetValue(bloom);
        _effect.Parameters["BlurTexture"].SetValue(blur);
        _effect.Parameters["BloomIntensity"].SetValue(0f);
        _effect.Parameters["BlurAmount"].SetValue(0f);
        _effect.Parameters["ChromaticAberration"].SetValue(0f);
        _effect.Parameters["Saturation"].SetValue(Saturation);
        _effect.Parameters["Contrast"].SetValue(Contrast);
        _effect.Parameters["Brightness"].SetValue(Brightness);
        _effect.Parameters["Tint"].SetValue(Tint);
        _effect.Parameters["VignetteIntensity"].SetValue(VignetteIntensity);
        _effect.Parameters["VignetteColor"].SetValue(VignetteColor.ToVector3());

        PresentationParameters screen = _device.PresentationParameters;
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, _effect);
        _device.SamplerStates[1] = SamplerState.LinearClamp;
        _device.SamplerStates[2] = SamplerState.LinearClamp;
        _spriteBatch.Draw(_scene, new Rectangle(0, 0, screen.BackBufferWidth, screen.BackBufferHeight), Color.White);
        _spriteBatch.End();
    }

    public void Dispose()
    {
        DisposeTargets();
        _black.Dispose();
    }

    private void EnsureTargets()
    {
        PresentationParameters screen = _device.PresentationParameters;
        const float scale = 1f;
        int width = Math.Max(1, (int)(screen.BackBufferWidth * scale));
        int height = Math.Max(1, (int)(screen.BackBufferHeight * scale));

        if (_scene != null && _scene.Width == width && _scene.Height == height)
            return;

        DisposeTargets();

        // The scene target needs a depth buffer for 3D. On a desktop we could also multisample it
        // for smooth edges, but MonoGame's OpenGL ES back end (Android and iOS) can't resolve
        // multisampled render targets, so we don't. At a phone's pixel density it's hard to tell.
        _scene = new RenderTarget2D(_device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24,
            0, RenderTargetUsage.DiscardContents);
    }

    private void DisposeTargets()
    {
        _scene?.Dispose();
        _scene = null;
    }
}
