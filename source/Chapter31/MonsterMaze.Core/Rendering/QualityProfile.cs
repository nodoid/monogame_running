using Microsoft.Xna.Framework.Graphics;

namespace MonsterMaze.Rendering;

public enum GraphicsQuality
{
    Low,
    Medium,
    High
}

/// <summary>
/// The graphics settings behind each quality level. Low suits older phones, High suits
/// recent phones and tablets.
/// </summary>
public sealed class QualityProfile
{
    public static readonly QualityProfile Low = new()
    {
        Level = GraphicsQuality.Low,
        MultiSampleCount = 0,
        AnisotropicFiltering = false,
        Bloom = false,
    };

    public static readonly QualityProfile Medium = new()
    {
        Level = GraphicsQuality.Medium,
        MultiSampleCount = 2,
        AnisotropicFiltering = true,
        Bloom = true,
    };

    public static readonly QualityProfile High = new()
    {
        Level = GraphicsQuality.High,
        MultiSampleCount = 4,
        AnisotropicFiltering = true,
        Bloom = true,
    };

    public GraphicsQuality Level { get; private init; }

    /// <summary>Samples per pixel for anti-aliasing (0 turns it off).</summary>
    public int MultiSampleCount { get; private init; }

    /// <summary>Keeps floor and wall textures sharp when seen at a steep angle.</summary>
    public bool AnisotropicFiltering { get; private init; }

    /// <summary>The glow around bright lights.</summary>
    public bool Bloom { get; private init; }

    /// <summary>Wrap so textures repeat; mipmapped by the content pipeline to avoid shimmering.</summary>
    public SamplerState WallSampler => AnisotropicFiltering ? SamplerState.AnisotropicWrap : SamplerState.LinearWrap;

    public static QualityProfile For(GraphicsQuality quality) => quality switch
    {
        GraphicsQuality.Low => Low,
        GraphicsQuality.Medium => Medium,
        _ => High
    };

    /// <summary>
    /// A sensible starting point before the player chooses: screens with an enormous number of
    /// pixels (large tablets) start on Medium to keep the frame rate smooth.
    /// </summary>
    public static GraphicsQuality Recommended(int pixelWidth, int pixelHeight) =>
        (long)pixelWidth * pixelHeight > 4_000_000 ? GraphicsQuality.Medium : GraphicsQuality.High;
}
