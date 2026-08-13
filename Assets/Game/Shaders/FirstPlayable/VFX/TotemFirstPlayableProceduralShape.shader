Shader "Totem/FirstPlayable/VFX/ProceduralShape"
{
    Properties
    {
        [HDR] _Color ("主颜色", Color) = (0.22,0.69,0.77,1)
        [HDR] _SecondaryColor ("次颜色", Color) = (1,1,1,1)
        [Enum(BoxFrame,0,DiamondFrame,1,Wedge,2,Branch,3)] _Shape ("形状", Float) = 0
        _EdgeWidth ("结构宽度", Range(0.01,0.25)) = 0.06
        _Softness ("边缘软化", Range(0.0001,0.05)) = 0.005
        _Progress ("播放进度", Range(0,1)) = 1
        _RevealWidth ("显现宽度", Range(0.02,1)) = 0.2
        _PulseSpeed ("脉冲速度", Range(0,20)) = 4
        _PulseAmount ("脉冲强度", Range(0,1)) = 0.15
        _NoiseScale ("噪声尺度", Range(1,64)) = 12
        _NoiseSpeed ("噪声速度", Range(-10,10)) = 1
        _NoiseAmount ("噪声强度", Range(0,1)) = 0.08
        _Intensity ("HDR 强度", Range(0,8)) = 1
        _Opacity ("透明度", Range(0,1)) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("深度测试", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest [_ZTest]
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "TotemFirstPlayableVFXCommon.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _SecondaryColor;
                float _Shape;
                float _EdgeWidth;
                float _Softness;
                float _Progress;
                float _RevealWidth;
                float _PulseSpeed;
                float _PulseAmount;
                float _NoiseScale;
                float _NoiseSpeed;
                float _NoiseAmount;
                float _Intensity;
                float _Opacity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float2 p = input.uv * 2.0 - 1.0;
                float shapeMask = FP_SelectShape(p, round(_Shape), _EdgeWidth, _Softness);

                float progress = saturate(_Progress);
                float revealedArea = 1.0 - smoothstep(progress, progress + 0.02, input.uv.y);
                float revealFront = 1.0 - smoothstep(
                    _RevealWidth,
                    _RevealWidth + 0.08,
                    abs(input.uv.y - progress));
                float reveal = saturate(revealedArea + revealFront * 0.35);

                float noise = FP_ValueNoise(input.uv * _NoiseScale + _Time.y * _NoiseSpeed);
                float noiseFactor = lerp(1.0, noise, _NoiseAmount);
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed * 6.2831853) * _PulseAmount;
                float edgeBand = saturate(shapeMask * reveal * noiseFactor);
                half3 color = lerp(_SecondaryColor.rgb, _Color.rgb, saturate(input.uv.y + noise * 0.2));
                color *= _Intensity * pulse * input.color.rgb;
                half alpha = edgeBand * _Opacity * _Color.a * input.color.a;
                clip(alpha - 0.001);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
