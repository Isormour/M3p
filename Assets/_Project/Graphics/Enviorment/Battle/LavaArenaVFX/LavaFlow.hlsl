#ifndef M3P_LAVA_FLOW_INCLUDED
#define M3P_LAVA_FLOW_INCLUDED

// RG stores visible motion in the sprite's UV space, mapped from [-1,1] to [0,1].
// Sample the field at stationary UVs: it belongs to the waterfall, not the noise.
void LavaFlowNoise_float(UnityTexture2D NoiseTex, UnityTexture2D FlowMap,
    float2 UV, float4 TNO, float Speed, float CycleSeconds, float Seconds,
    out float Noise)
{
    float2 direction = SAMPLE_TEXTURE2D(FlowMap.tex, FlowMap.samplerstate, UV).rg * 2.0 - 1.0;
    // Both nearest 8-bit representations of 0.5 should mean no motion.
    direction = sign(direction) * max(abs(direction) - 1.0 / 255.0, 0.0) * (255.0 / 254.0);

    float cycle = max(CycleSeconds, 0.05);
    float phaseA = frac(Seconds / cycle);
    float phaseB = frac(phaseA + 0.5);
    float weightA = 1.0 - abs(2.0 * phaseA - 1.0);
    float2 travel = direction * Speed * cycle;
    // Backtrace texture samples so the visible features move WITH the field.
    // Bounded phases prevent the usual accumulating UV distortion at bends.
    float2 uvA = (UV - travel * phaseA) * TNO.xy;
    float2 uvB = (UV - travel * phaseB) * TNO.xy;
    float a = SAMPLE_TEXTURE2D(NoiseTex.tex, NoiseTex.samplerstate, uvA).r;
    float b = SAMPLE_TEXTURE2D(NoiseTex.tex, NoiseTex.samplerstate, uvB).r;
    Noise = lerp(b, a, weightA);
}

void LavaFlowNoise_half(UnityTexture2D NoiseTex, UnityTexture2D FlowMap,
    half2 UV, half4 TNO, half Speed, half CycleSeconds, half Seconds,
    out half Noise)
{
    float value;
    LavaFlowNoise_float(NoiseTex, FlowMap, UV, TNO, Speed, CycleSeconds, Seconds, value);
    Noise = (half)value;
}

#endif
