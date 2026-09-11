// Monster Maze post-processing: the final pass that turns the rendered maze into the image on
// screen. It adds chromatic aberration, blur, bloom, colour grading and a vignette in one go.
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

// SpriteBatch puts the scene texture in the first slot.
Texture2D SpriteTexture;
sampler2D SceneSampler : register(s0) = sampler_state { Texture = <SpriteTexture>; };

// The glow from bright lights (Chapter 31) and a blurred copy of the scene (Chapter 30).
Texture2D BloomTexture;
sampler2D BloomSampler : register(s1) = sampler_state { Texture = <BloomTexture>; };
Texture2D BlurTexture;
sampler2D BlurSampler : register(s2) = sampler_state { Texture = <BlurTexture>; };

float BloomIntensity;        // 0 = no glow
float BlurAmount;            // 0 = sharp, 1 = fully blurred
float ChromaticAberration;   // how far the red and blue channels drift apart at the edges
float Saturation;            // 1 = unchanged, 0 = black and white
float Contrast;              // 1 = unchanged
float Brightness;            // added to every channel
float3 Tint;                 // multiplies the final colour
float VignetteIntensity;     // how dark the corners get
float3 VignetteColor;        // black normally, red when Rex is close

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 CompositePS(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TexCoord;
    float2 fromCentre = uv - 0.5;

    // Chromatic aberration: sample red and blue slightly outwards and inwards.
    float2 shift = fromCentre * ChromaticAberration;
    float3 colour;
    colour.r = tex2D(SceneSampler, uv + shift).r;
    colour.g = tex2D(SceneSampler, uv).g;
    colour.b = tex2D(SceneSampler, uv - shift).b;

    // Panic blur: mix towards a blurred copy of the scene.
    colour = lerp(colour, tex2D(BlurSampler, uv).rgb, BlurAmount);

    // Bloom: add the glow on top.
    colour += tex2D(BloomSampler, uv).rgb * BloomIntensity;

    // Colour grading.
    float grey = dot(colour, float3(0.299, 0.587, 0.114));
    colour = lerp(float3(grey, grey, grey), colour, Saturation);
    colour = (colour - 0.5) * Contrast + 0.5 + Brightness;
    colour *= Tint;

    // Vignette: fade the edges towards the vignette colour.
    float edge = smoothstep(0.3, 0.85, length(fromCentre) * 1.35);
    colour = lerp(colour, VignetteColor, edge * VignetteIntensity);

    return float4(saturate(colour), 1.0) * input.Color;
}

technique Composite
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL CompositePS();
    }
};
