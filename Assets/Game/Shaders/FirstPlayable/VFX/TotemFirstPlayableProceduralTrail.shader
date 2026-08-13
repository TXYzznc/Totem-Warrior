Shader "Totem/FirstPlayable/VFX/ProceduralTrail"
{
    Properties
    {
        _NoiseTex ("共用噪声", 2D) = "gray" {}
        [HDR] _HeadColor ("头部颜色", Color) = (1,1,1,1)
        [HDR] _TailColor ("尾部颜色", Color) = (0.22,0.69,0.77,0)
        _SegmentCount ("分段数量", Range(1,32)) = 8
        _SegmentFill ("分段占比", Range(0.05,1)) = 0.65
        _FlowSpeed ("流动速度", Range(-20,20)) = 4
        _CoreWidth ("核心宽度", Range(0.01,1)) = 0.3
        _EdgeSoftness ("边缘软化", Range(0.001,0.4)) = 0.08
        _NoiseScale ("噪声尺度", Range(0.1,16)) = 3
        _NoiseAmount ("噪声强度", Range(0,1)) = 0.12
        _Taper ("首尾收束", Range(0.001,0.5)) = 0.12
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

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _NoiseTex_ST;
                half4 _HeadColor;
                half4 _TailColor;
                float _SegmentCount;
                float _SegmentFill;
                float _FlowSpeed;
                float _CoreWidth;
                float _EdgeSoftness;
                float _NoiseScale;
                float _NoiseAmount;
                float _Taper;
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
                float flow = input.uv.x * _SegmentCount - _Time.y * _FlowSpeed;
                float segmentPhase = frac(flow);
                float segmentMask = 1.0 - step(_SegmentFill, segmentPhase);

                float centeredY = abs(input.uv.y * 2.0 - 1.0);
                float widthMask = 1.0 - smoothstep(_CoreWidth, _CoreWidth + _EdgeSoftness, centeredY);
                float taperIn = saturate(input.uv.x / max(_Taper, 0.001));
                float taperOut = saturate((1.0 - input.uv.x) / max(_Taper, 0.001));
                float taper = taperIn * taperOut;

                float2 noiseUv = input.uv * float2(_NoiseScale, 1.0) + float2(_Time.y * _FlowSpeed * 0.05, 0.0);
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUv).r;
                float noiseFactor = lerp(1.0, noise, _NoiseAmount);

                float mask = segmentMask * widthMask * taper * noiseFactor;
                half4 color = lerp(_TailColor, _HeadColor, input.uv.x) * input.color;
                color.rgb *= _Intensity;
                color.a *= mask * _Opacity;
                clip(color.a - 0.001);
                return color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
