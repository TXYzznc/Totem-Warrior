Shader "Totem/Visual Destruction"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _DestructionProgress("Destruction Progress", Range(0,1)) = 0
        _DestructionStrength("Destruction Strength", Range(0,1)) = 1
        _DestructionSeed("Destruction Seed", Range(0,1)) = 0
        _HitPointOS("Hit Point Object Space", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _DestructionProgress;
                float _DestructionStrength;
                float _DestructionSeed;
                float4 _HitPointOS;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float breakup : TEXCOORD3;
            };

            float Hash31(float3 value)
            {
                value = frac(value * 0.1031);
                value += dot(value, value.yzx + 33.33);
                return frac((value.x + value.y) * value.z);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz;
                float distanceFromHit = distance(positionOS, _HitPointOS.xyz);
                float noise = Hash31(floor(positionOS * 5.0) + _DestructionSeed);
                float breakup = saturate(distanceFromHit * 0.65 + noise * 0.55);
                float displacement = saturate(_DestructionProgress * 1.25 - breakup + 0.18) * _DestructionStrength * 0.26;
                float3 direction = normalize(positionOS - _HitPointOS.xyz + input.normalOS * 0.05);
                positionOS += direction * displacement;

                output.positionHCS = TransformObjectToHClip(positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionOS = input.positionOS.xyz;
                output.breakup = breakup;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                clip(input.breakup - _DestructionProgress);
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                Light light = GetMainLight();
                half lighting = saturate(dot(normalize(input.normalWS), light.direction)) * 0.75h + 0.25h;
                half edge = saturate(1.0h - abs(input.breakup - _DestructionProgress) * 14.0h);
                half3 color = albedo.rgb * (light.color * lighting + SampleSH(normalize(input.normalWS)));
                color += edge * half3(0.08h, 0.07h, 0.04h);
                return half4(color, albedo.a);
            }
            ENDHLSL
        }
    }
}
