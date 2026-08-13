#ifndef TOTEM_FIRST_PLAYABLE_VFX_COMMON_INCLUDED
#define TOTEM_FIRST_PLAYABLE_VFX_COMMON_INCLUDED

float FP_Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float FP_ValueNoise(float2 p)
{
    float2 cell = floor(p);
    float2 local = frac(p);
    local = local * local * (3.0 - 2.0 * local);
    float a = FP_Hash21(cell);
    float b = FP_Hash21(cell + float2(1.0, 0.0));
    float c = FP_Hash21(cell + float2(0.0, 1.0));
    float d = FP_Hash21(cell + 1.0);
    return lerp(lerp(a, b, local.x), lerp(c, d, local.x), local.y);
}

float FP_SdBox(float2 p, float2 halfSize)
{
    float2 d = abs(p) - halfSize;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

float FP_SdDiamond(float2 p, float radius)
{
    return (abs(p.x) + abs(p.y) - radius) * 0.70710678;
}

float FP_SdSegment(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / max(dot(ba, ba), 0.00001));
    return length(pa - ba * h);
}

float FP_FillMask(float distanceValue, float softness)
{
    float aa = max(fwidth(distanceValue), max(softness, 0.0001));
    return 1.0 - smoothstep(-aa, aa, distanceValue);
}

float FP_FrameMask(float distanceValue, float width, float softness)
{
    float frameDistance = abs(distanceValue) - width;
    float aa = max(fwidth(frameDistance), max(softness, 0.0001));
    return 1.0 - smoothstep(-aa, aa, frameDistance);
}

float FP_BoxFrame(float2 p, float2 halfSize, float width, float softness)
{
    return FP_FrameMask(FP_SdBox(p, halfSize), width, softness);
}

float FP_DiamondFrame(float2 p, float radius, float width, float softness)
{
    return FP_FrameMask(FP_SdDiamond(p, radius), width, softness);
}

float FP_Wedge(float2 p, float softness)
{
    float body = FP_FillMask(FP_SdBox(p - float2(0.0, -0.28), float2(0.12, 0.44)), softness);
    float diagonalA = FP_SdSegment(p, float2(-0.62, 0.08), float2(0.0, 0.68));
    float diagonalB = FP_SdSegment(p, float2(0.62, 0.08), float2(0.0, 0.68));
    float head = max(FP_FillMask(diagonalA - 0.13, softness), FP_FillMask(diagonalB - 0.13, softness));
    return saturate(max(body, head) * step(-0.05, p.y + 0.7));
}

float FP_Branch(float2 p, float width, float softness)
{
    float d0 = FP_SdSegment(p, float2(-0.72, -0.58), float2(-0.18, -0.08));
    float d1 = FP_SdSegment(p, float2(-0.18, -0.08), float2(0.18, 0.18));
    float d2 = FP_SdSegment(p, float2(0.18, 0.18), float2(0.64, 0.68));
    float d3 = FP_SdSegment(p, float2(0.08, 0.08), float2(0.58, -0.34));
    float distanceValue = min(min(d0, d1), min(d2, d3)) - width;
    return FP_FillMask(distanceValue, softness);
}

float FP_SelectShape(float2 p, float shape, float edgeWidth, float softness)
{
    float box = FP_BoxFrame(p, float2(0.72, 0.72), edgeWidth, softness);
    float diamond = FP_DiamondFrame(p, 0.92, edgeWidth, softness);
    float wedge = FP_Wedge(p, softness);
    float branch = FP_Branch(p, edgeWidth * 1.4, softness);

    float boxWeight = 1.0 - step(0.5, abs(shape - 0.0));
    float diamondWeight = 1.0 - step(0.5, abs(shape - 1.0));
    float wedgeWeight = 1.0 - step(0.5, abs(shape - 2.0));
    float branchWeight = 1.0 - step(0.5, abs(shape - 3.0));
    return saturate(box * boxWeight + diamond * diamondWeight + wedge * wedgeWeight + branch * branchWeight);
}

#endif
