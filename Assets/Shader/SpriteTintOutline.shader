// change #22 子项 A：Bot 染色 + 描边一体 Sprite shader（URP 兼容）
//
// 单 Pass：
//   1. 采样 _MainTex，rgb × _Tint 得到本体色（保留纹理明暗）
//   2. 采样上下左右 4 个 UV 偏移，若当前像素透明但邻居有实体 → 输出 _OutlineColor
//
// SRP Batcher：所有材质属性放到 UnityPerMaterial CBUFFER，_MainTex 走 PerRendererData
// 由 SpriteRenderer 通过 MaterialPropertyBlock 注入。同一 shader / 同一 sharedMaterial 下
// 49 Bot 各自 tint 不同色不会破 Batcher 合并。
//
// 用法（C#）：
//   var mpb = new MaterialPropertyBlock();
//   sr.GetPropertyBlock(mpb);
//   mpb.SetColor("_Tint", color);
//   sr.SetPropertyBlock(mpb);

Shader "Tattoo/SpriteTintOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Tint            ("Tint",            Color) = (1,1,1,1)
        _OutlineColor    ("Outline Color",   Color) = (0,0,0,1)
        _OutlineWidth    ("Outline Width (texel)", Range(0, 8)) = 2
        _AlphaThreshold  ("Alpha Threshold", Range(0, 1))       = 0.1

        // ── 与 Sprites/Default 一致的 stencil / clip 属性 ──
        _StencilComp     ("Stencil Comparison", Float) = 8
        _Stencil         ("Stencil ID",         Float) = 0
        _StencilOp       ("Stencil Operation",  Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask",  Float) = 255
        _ColorMask       ("Color Mask",         Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline"    = "UniversalPipeline"
        }

        Cull   Off
        Lighting Off
        ZWrite Off
        Blend  One OneMinusSrcAlpha // premultiplied，配合 fragment 的 rgb *= a

        Pass
        {
            Name "SpriteTintOutline"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;   // (1/w, 1/h, w, h)
                half4  _Tint;
                half4  _OutlineColor;
                float  _OutlineWidth;
                float  _AlphaThreshold;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 src   = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half  alpha = src.a * _Tint.a * IN.color.a;

                // ── 描边：采样 4 邻域 alpha ──
                float2 texel = _MainTex_TexelSize.xy * _OutlineWidth;
                half aU = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(0,       texel.y)).a;
                half aD = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv - float2(0,       texel.y)).a;
                half aL = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv - float2(texel.x, 0)).a;
                half aR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(texel.x, 0)).a;
                half neighborMax = max(max(aU, aD), max(aL, aR));
                half neighborMin = min(min(aU, aD), min(aL, aR));

                // 外描边：当前透明 + 邻居实心（sprite 边界外围一圈）
                float outerOutline = step(alpha, _AlphaThreshold) * step(_AlphaThreshold, neighborMax);
                // 内描边：当前实心 + 邻居透明（硬边 sprite 内圈边缘一圈）
                float innerOutline = step(_AlphaThreshold, alpha) * step(neighborMin, _AlphaThreshold);
                float isOutline    = saturate(outerOutline + innerOutline);

                // 本体色：纹理 * Tint * VertexColor
                half3 bodyRgb = src.rgb * _Tint.rgb * IN.color.rgb;

                // 描边像素直接替换为描边色（alpha 也切到描边 alpha）
                half3 finalRgb = lerp(bodyRgb, _OutlineColor.rgb, isOutline);
                half  finalA   = lerp(alpha,   _OutlineColor.a,   isOutline);

                // Premultiplied alpha（配合 Blend One OneMinusSrcAlpha）
                return half4(finalRgb * finalA, finalA);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
