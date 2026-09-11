// @since 29
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
#if CH30
    private RenderTarget2D _blurHalf;
    private RenderTarget2D _blurSmall;
#endif
#if CH31
    private readonly Bloom _bloom;
#endif
    private QualityProfile _quality;

    public PostProcessor(GraphicsDevice device, SpriteBatch spriteBatch, GameAssets assets, QualityProfile quality)
    {
        _device = device;
        _spriteBatch = spriteBatch;
        _effect = assets.PostProcessEffect;
        _quality = quality;
        _black = new Texture2D(device, 1, 1);
        _black.SetData(new[] { Color.Black });
#if CH31
        _bloom = new Bloom(device, spriteBatch, assets.BloomEffect);
#endif
    }

    public float VignetteIntensity { get; set; } = 0.45f;
    public Color VignetteColor { get; set; } = Color.Black;
    public float Saturation { get; set; } = 1f;
    public float Contrast { get; set; } = 1f;
    public float Brightness { get; set; }
    public Vector3 Tint { get; set; } = Vector3.One;
#if CH30
    public float ChromaticAberration { get; set; }
    public float BlurAmount { get; set; }
#endif
#if CH31
    public float BloomIntensity { get; set; } = 0.9f;
    public float BloomThreshold { get; set; } = 0.72f;
#endif

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
#if CH30

        // A blurred copy is just the scene shrunk twice; stretching it back up blurs it.
        if (BlurAmount > 0.01f)
        {
            Copy(_scene, _blurHalf);
            Copy(_blurHalf, _blurSmall);
            blur = _blurSmall;
        }
#endif
#if CH31

        if (_quality.Bloom && BloomIntensity > 0.01f)
            bloom = _bloom.Process(_scene, BloomThreshold);
#endif
        _device.SetRenderTarget(null);

        _effect.Parameters["BloomTexture"].SetValue(bloom);
        _effect.Parameters["BlurTexture"].SetValue(blur);
#if CH31
        _effect.Parameters["BloomIntensity"].SetValue(bloom == _black ? 0f : BloomIntensity);
#else
        _effect.Parameters["BloomIntensity"].SetValue(0f);
#endif
#if CH30
        _effect.Parameters["BlurAmount"].SetValue(blur == _black ? 0f : BlurAmount);
        _effect.Parameters["ChromaticAberration"].SetValue(ChromaticAberration);
#else
        _effect.Parameters["BlurAmount"].SetValue(0f);
        _effect.Parameters["ChromaticAberration"].SetValue(0f);
#endif
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
#if CH31
        _bloom.Dispose();
#endif
    }

    private void EnsureTargets()
    {
        PresentationParameters screen = _device.PresentationParameters;
#if CH40
        float scale = _quality.RenderScale;
#else
        const float scale = 1f;
#endif
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
#if CH30
        _blurHalf = new RenderTarget2D(_device, Math.Max(1, width / 4), Math.Max(1, height / 4));
        _blurSmall = new RenderTarget2D(_device, Math.Max(1, width / 12), Math.Max(1, height / 12));
#endif
    }

    private void DisposeTargets()
    {
        _scene?.Dispose();
        _scene = null;
#if CH30
        _blurHalf?.Dispose();
        _blurHalf = null;
        _blurSmall?.Dispose();
        _blurSmall = null;
#endif
    }
#if CH30

    private void Copy(Texture2D source, RenderTarget2D destination)
    {
        _device.SetRenderTarget(destination);
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone);
        _spriteBatch.Draw(source, new Rectangle(0, 0, destination.Width, destination.Height), Color.White);
        _spriteBatch.End();
    }
#endif
}
