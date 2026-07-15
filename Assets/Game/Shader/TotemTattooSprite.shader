Shader "Totem/Actor Tattoo Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TattooMap ("Tattoo Map", 2D) = "black" {}
        _TattooPatternAtlas ("Tattoo Pattern Atlas", 2D) = "white" {}
        _TattooPart1 ("Tattoo Part 1", Vector) = (0,0,0,0)
        _TattooPart2 ("Tattoo Part 2", Vector) = (0,0,0,0)
        _TattooPart3 ("Tattoo Part 3", Vector) = (0,0,0,0)
        _TattooPart4 ("Tattoo Part 4", Vector) = (0,0,0,0)
        _TattooPart5 ("Tattoo Part 5", Vector) = (0,0,0,0)
        _TattooPart6 ("Tattoo Part 6", Vector) = (0,0,0,0)
        _TattooTransform1 ("Tattoo Transform 1", Vector) = (0.5,0.5,1,0)
        _TattooTransform2 ("Tattoo Transform 2", Vector) = (0.5,0.5,1,0)
        _TattooTransform3 ("Tattoo Transform 3", Vector) = (0.5,0.5,1,0)
        _TattooTransform4 ("Tattoo Transform 4", Vector) = (0.5,0.5,1,0)
        _TattooTransform5 ("Tattoo Transform 5", Vector) = (0.5,0.5,1,0)
        _TattooTransform6 ("Tattoo Transform 6", Vector) = (0.5,0.5,1,0)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "ActorTattooSprite"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionHCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_TattooMap); SAMPLER(sampler_TattooMap);
            TEXTURE2D(_TattooPatternAtlas); SAMPLER(sampler_TattooPatternAtlas);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float4 _TattooPart1; float4 _TattooPart2; float4 _TattooPart3;
                float4 _TattooPart4; float4 _TattooPart5; float4 _TattooPart6;
                float4 _TattooTransform1; float4 _TattooTransform2; float4 _TattooTransform3;
                float4 _TattooTransform4; float4 _TattooTransform5; float4 _TattooTransform6;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            void ResolvePart(int partId, out float4 descriptor, out float4 transform)
            {
                descriptor = _TattooPart1; transform = _TattooTransform1;
                if (partId == 2) { descriptor = _TattooPart2; transform = _TattooTransform2; }
                else if (partId == 3) { descriptor = _TattooPart3; transform = _TattooTransform3; }
                else if (partId == 4) { descriptor = _TattooPart4; transform = _TattooTransform4; }
                else if (partId == 5) { descriptor = _TattooPart5; transform = _TattooTransform5; }
                else if (partId == 6) { descriptor = _TattooPart6; transform = _TattooTransform6; }
            }

            half SamplePattern(float2 localUv, float patternId)
            {
                float patternIndex = clamp(round(patternId) - 1.0, 0.0, 7.0);
                float2 atlasCell = float2(fmod(patternIndex, 4.0), floor(patternIndex / 4.0));
                float2 atlasUv = (atlasCell + saturate(localUv)) / float2(4.0, 2.0);
                return SAMPLE_TEXTURE2D(_TattooPatternAtlas, sampler_TattooPatternAtlas, atlasUv).a;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 body = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color * input.color;
                half4 map = SAMPLE_TEXTURE2D(_TattooMap, sampler_TattooMap, input.uv);
                int partId = (int)round(map.b * 255.0);
                if (body.a > 0.001 && map.a > 0.001 && partId >= 1 && partId <= 6)
                {
                    float4 descriptor; float4 transform;
                    ResolvePart(partId, descriptor, transform);
                    if (descriptor.w > 0.0)
                    {
                        float safeScale = max(transform.z, 0.01);
                        float2 patternUv = (map.rg - transform.xy) / safeScale + 0.5;
                        float insidePattern = step(0.0, patternUv.x) * step(patternUv.x, 1.0) * step(0.0, patternUv.y) * step(patternUv.y, 1.0);
                        // The review prototype atlas is solid white, so the test scene renders
                        // every approved skin region as one continuous 80% opaque colour block.
                        // Real tattoo atlases may still supply a patterned alpha later.
                        half inkAlpha = SamplePattern(patternUv, descriptor.w) * map.a * insidePattern * body.a * 0.80;
                        body.rgb = lerp(body.rgb, descriptor.rgb, inkAlpha);
                    }
                }

                return half4(body.rgb * body.a, body.a);
            }
            ENDHLSL
        }
    }
    Fallback "Sprites/Default"
}
