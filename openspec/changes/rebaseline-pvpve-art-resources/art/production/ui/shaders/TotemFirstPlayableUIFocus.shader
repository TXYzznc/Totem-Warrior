Shader "Totem/FirstPlayable/UI/FocusSweep"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _FocusColor ("焦点颜色", Color) = (0.22,0.69,0.77,1)
        _FocusAmount ("焦点强度", Range(0,1)) = 0
        _BorderWidth ("边框宽度", Range(0.002,0.2)) = 0.035
        _Chamfer ("切角", Range(0,0.25)) = 0.08
        _SweepPosition ("扫描位置", Range(-0.5,1.5)) = -0.5
        _SweepWidth ("扫描宽度", Range(0.01,0.5)) = 0.12
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UIFocusSweep"
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _FocusColor;
                float4 _ClipRect;
                float _FocusAmount;
                float _BorderWidth;
                float _Chamfer;
                float _SweepPosition;
                float _SweepWidth;
            CBUFFER_END

            // UnityUI.cginc is a legacy CG include. This URP HLSL shader keeps the
            // required RectMask2D clip test locally so it does not pull in fixed4.
            float GetUIRectClipMask(float2 position, float4 clipRect)
            {
                float2 inside = step(clipRect.xy, position) * step(position, clipRect.zw);
                return inside.x * inside.y;
            }

            float SdChamferedBox(float2 p, float2 halfSize, float chamfer)
            {
                p = abs(p);
                float2 d = p - halfSize;
                float box = max(d.x, d.y);
                float cut = (p.x + p.y - halfSize.x - halfSize.y + chamfer) * 0.70710678;
                return max(box, cut);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.worldPosition = input.positionOS;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                float2 p = input.uv * 2.0 - 1.0;
                float distanceValue = SdChamferedBox(p, float2(0.96, 0.88), _Chamfer);
                float aa = max(fwidth(distanceValue), 0.001);
                float border = 1.0 - smoothstep(_BorderWidth - aa, _BorderWidth + aa, abs(distanceValue));
                float sweep = 1.0 - smoothstep(_SweepWidth, _SweepWidth + 0.03, abs(input.uv.x - _SweepPosition));
                float focusMask = saturate(max(border, sweep * border * 1.5) * _FocusAmount);
                sprite.rgb = lerp(sprite.rgb, _FocusColor.rgb * _FocusColor.a, focusMask);
                sprite.a = max(sprite.a, focusMask * _FocusColor.a);

                #ifdef UNITY_UI_CLIP_RECT
                    sprite.a *= GetUIRectClipMask(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(sprite.a - 0.001);
                #endif
                return sprite;
            }
            ENDHLSL
        }
    }
    Fallback "UI/Default"
}
