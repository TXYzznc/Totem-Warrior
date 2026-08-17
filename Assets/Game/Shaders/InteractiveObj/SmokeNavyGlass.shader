Shader "Game/InteractiveObj/Smoke Navy Glass"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Tint", Color) = (0.035, 0.115, 0.175, 1)
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0, 2)) = 0.72
        _MetallicGlossMap("Metallic Smoothness", 2D) = "white" {}
        _Opacity("Core Opacity", Range(0, 1)) = 0.58
        _EdgeOpacity("Edge Opacity", Range(0, 1)) = 0.84
        _FresnelPower("Edge Fresnel Power", Range(0.5, 8)) = 3.2
        [HDR] _EdgeColor("Edge Reflection Color", Color) = (0.18, 0.62, 0.82, 1)
        _EdgeIntensity("Edge Reflection Intensity", Range(0, 3)) = 0.72
        _Smoothness("Smoothness", Range(0, 1)) = 0.86
        _SmokeStrength("Smoked Thickness", Range(0, 1)) = 0.34
        _NoiseScale("Micro Variation Scale", Range(1, 80)) = 32
        [HDR] _FlowColor("Internal Flow Color", Color) = (0.03, 0.72, 1.1, 1)
        _FlowIntensity("Internal Flow Intensity", Range(0, 3)) = 0.55
        _FlowSpeed("Internal Flow Speed", Range(0, 0.5)) = 0.11
        _FlowTiling("Internal Flow Tiling", Range(0.5, 8)) = 2.2
        _FlowWidth("Internal Flow Width", Range(0.01, 0.45)) = 0.16
        _FlowDistortion("Internal Flow Distortion", Range(0, 0.5)) = 0.12
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MetallicGlossMap);
            SAMPLER(sampler_MetallicGlossMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BumpScale;
                half _Opacity;
                half _EdgeOpacity;
                half _FresnelPower;
                half4 _EdgeColor;
                half _EdgeIntensity;
                half _Smoothness;
                half _SmokeStrength;
                half _NoiseScale;
                half4 _FlowColor;
                half _FlowIntensity;
                half _FlowSpeed;
                half _FlowTiling;
                half _FlowWidth;
                half _FlowDistortion;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half3 tangentWS : TEXCOORD3;
                half3 bitangentWS : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
            };

            half Hash21(float2 value)
            {
                value = frac(value * half2(123.34h, 345.45h));
                value += dot(value, value + 34.345h);
                return frac(value.x * value.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = normalInputs.tangentWS;
                output.bitangentWS = normalInputs.bitangentWS;
                output.shadowCoord = GetShadowCoord(positionInputs);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                half3 normalWS = normalize(input.tangentWS * normalTS.x + input.bitangentWS * normalTS.y + input.normalWS * normalTS.z);
                half3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half fresnel = pow(saturate(1.0h - dot(normalWS, viewDirWS)), _FresnelPower);
                half4 source = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 mask = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, input.uv);
                half grain = Hash21(floor(input.uv * _NoiseScale));
                half smoke = lerp(1.0h - _SmokeStrength, 1.0h, grain);
                half3 baseColor = source.rgb * _BaseColor.rgb * smoke;
                half smoothness = saturate(_Smoothness * mask.a);

                Light mainLight = GetMainLight(input.shadowCoord);
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 indirect = SampleSH(normalWS) * baseColor;
                half3 diffuse = baseColor * (mainLight.color * (ndotl * mainLight.shadowAttenuation) + indirect);
                half3 halfDir = SafeNormalize(mainLight.direction + viewDirWS);
                half specularPower = exp2(4.0h + smoothness * 8.0h);
                half specular = pow(saturate(dot(normalWS, halfDir)), specularPower) * (0.18h + smoothness * 0.82h);
                half3 glassReflection = _EdgeColor.rgb * (fresnel * _EdgeIntensity + specular * mainLight.shadowAttenuation);
                // A single procedural stripe is kept inside the transparent forward pass.  It avoids
                // an extra texture or pass while making the energy feel embedded in the crystal volume.
                half flowDistortion = sin((input.uv.x + input.uv.y * 0.25h) * 6.283185h) * _FlowDistortion;
                half flowPhase = frac(input.uv.y * _FlowTiling - _TimeParameters.x * _FlowSpeed + flowDistortion);
                half distanceToFlowCenter = abs(flowPhase - 0.5h) * 2.0h;
                half flowLine = 1.0h - smoothstep(_FlowWidth, _FlowWidth + 0.10h, distanceToFlowCenter);
                half crystalMask = saturate(source.g * 1.25h);
                half internalFlow = flowLine * crystalMask * (0.45h + 0.55h * (1.0h - fresnel));
                half3 flowGlow = _FlowColor.rgb * (internalFlow * _FlowIntensity);
                half3 color = diffuse * (0.34h + 0.28h * (1.0h - fresnel)) + glassReflection + flowGlow;
                half alpha = lerp(_Opacity, _EdgeOpacity, fresnel);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
