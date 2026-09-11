// Monster Maze bloom: makes bright things (the exit, crystals, sparks) glow.
// "Extract" keeps only the bright parts of the scene; "Blur" softens them in one direction.
//
// Compiled ahead of time for OpenGL ES (Android and iOS). See Tools/ShaderCompiler.

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_3
    #define PS_SHADERMODEL ps_4_0_level_9_3
#endif

Texture2D SpriteTexture;
sampler2D SourceSampler : register(s0) = sampler_state { Texture = <SpriteTexture>; };

float Threshold;     // brightness (0-1) below which nothing glows
float2 TexelStep;    // one texel in the blur direction, e.g. (1/width, 0)

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 ExtractPS(VertexShaderOutput input) : COLOR0
{
    float3 colour = tex2D(SourceSampler, input.TexCoord).rgb;
    float brightest = max(colour.r, max(colour.g, colour.b));

    // A soft threshold, so glow fades in rather than switching on.
    float amount = saturate((brightest - Threshold) / max(0.0001, 1.0 - Threshold));
    return float4(colour * amount, 1.0);
}

float4 BlurPS(VertexShaderOutput input) : COLOR0
{
    // A 9-tap Gaussian blur done with 5 samples, using the GPU's bilinear filtering to blend
    // pairs of texels for free.
    float2 uv = input.TexCoord;
    float3 sum = tex2D(SourceSampler, uv).rgb * 0.2270270270;
    sum += tex2D(SourceSampler, uv + TexelStep * 1.3846153846).rgb * 0.3162162162;
    sum += tex2D(SourceSampler, uv - TexelStep * 1.3846153846).rgb * 0.3162162162;
    sum += tex2D(SourceSampler, uv + TexelStep * 3.2307692308).rgb * 0.0702702703;
    sum += tex2D(SourceSampler, uv - TexelStep * 3.2307692308).rgb * 0.0702702703;
    return float4(sum, 1.0);
}

technique Extract
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL ExtractPS();
    }
};

technique Blur
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL BlurPS();
    }
};
