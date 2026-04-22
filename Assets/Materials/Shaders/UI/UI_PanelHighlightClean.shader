Shader "UI/VRPanelHighlightClean"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _HighlightAmount ("Fill Amount", Range(0, 1)) = 0

        [Header(Shape Settings)]
        _AspectRatio ("Aspect Ratio (Width/Height)", Float) = 1.0
        _CornerRadius ("Corner Radius", Range(0, 2)) = 0.18
        _BorderThickness ("Border Thickness", Range(0.001, 0.5)) = 0.035

        [Header(Blue Futuristic Look)]
        [HDR] _IdleGradientBottom ("Base Shadow", Color) = (0.015, 0.045, 0.11, 1)
        [HDR] _IdleGradientTop ("Base Light", Color) = (0.055, 0.18, 0.38, 1)
        [HDR] _IdleBorderColor ("Outer Frame", Color) = (0.12, 0.35, 0.68, 1)
        [HDR] _ActiveBorderColor ("Neon Fill", Color) = (0.10, 0.78, 1.0, 1)
        [HDR] _VignetteColor ("Energy Glow", Color) = (0.62, 0.94, 1.0, 1)
        _VignetteIntensity ("Glow Intensity", Float) = 1.35
        _AnimSpeed ("Pulse Speed", Float) = 2.6

        [Header(UI Settings)]
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
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _HighlightAmount;

            float _AspectRatio;
            float _CornerRadius;
            float _BorderThickness;

            float4 _IdleGradientBottom;
            float4 _IdleGradientTop;
            float4 _IdleBorderColor;
            float4 _ActiveBorderColor;
            float4 _VignetteColor;
            float _VignetteIntensity;
            float _AnimSpeed;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            float sdRoundBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 texColor = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                float2 uv = IN.texcoord * 2.0 - 1.0;
                uv.x *= _AspectRatio;

                float2 extents = float2(_AspectRatio, 1.0) - 0.01;
                float d = sdRoundBox(uv, extents, _CornerRadius);

                float fw = max(length(float2(ddx(d), ddy(d))), 0.003);
                float alphaMask = 1.0 - smoothstep(-fw, fw, d);

                float halfThickness = _BorderThickness * 0.5;
                float dBorder = abs(d + halfThickness) - halfThickness;
                float borderMask = 1.0 - smoothstep(-fw, fw, dBorder);

                // Fill progress
                float fillAmount = saturate(_HighlightAmount);
                float fillFeather = max(fw * 2.5, 0.01);
                float fillMask = 1.0 - smoothstep(fillAmount, fillAmount + fillFeather, IN.texcoord.x);
                float fillHead = 1.0 - smoothstep(fillAmount - fillFeather * 3.0, fillAmount + fillFeather * 3.0, abs(IN.texcoord.x - fillAmount));

                // Animated effects — completely off when fillAmount is near zero
                float effectFade = smoothstep(0.0, 0.15, fillAmount);

                float pulseTime = _Time.y * _AnimSpeed;

                float scanWave = sin((IN.texcoord.y * 38.0 + IN.texcoord.x * 12.0) * 6.28318 + pulseTime * 2.8);
                scanWave = 0.5 + 0.5 * scanWave;
                scanWave = smoothstep(0.35, 0.95, scanWave);
                scanWave *= effectFade;

                float sweep = sin((IN.texcoord.x * 22.0 - pulseTime * 3.6) * 6.28318);
                sweep = 0.5 + 0.5 * sweep;
                sweep = smoothstep(0.55, 0.95, sweep);
                sweep *= fillMask * effectFade;

                // Clean idle gradient
                float verticalFactor = saturate(IN.texcoord.y);
                float4 baseGradient = lerp(_IdleGradientBottom, _IdleGradientTop, verticalFactor);

                // Hover fill glow
                float centerGlow = pow(saturate(1.0 - abs(IN.texcoord.y - 0.5) * 2.0), 2.4);
                float4 fillGlow = _ActiveBorderColor * (0.55 + 0.45 * scanWave);
                fillGlow.rgb += _VignetteColor.rgb * (0.22 + 0.20 * centerGlow);
                fillGlow.rgb += _VignetteColor.rgb * sweep * 0.35;
                fillGlow.a = 1.0;

                // Border glow
                float4 frameGlow = _IdleBorderColor * 0.55 + _ActiveBorderColor * borderMask * 0.85;
                frameGlow.rgb += _VignetteColor.rgb * borderMask * 0.25 * _VignetteIntensity;

                // Compose body: clean gradient base, fill effects only where highlighted
                float4 bodyColor = baseGradient;
                bodyColor.rgb = lerp(bodyColor.rgb, fillGlow.rgb, fillMask * effectFade);
                bodyColor.rgb += fillHead * _VignetteColor.rgb * 0.65 * fillMask * effectFade;
                bodyColor.rgb += frameGlow.rgb * borderMask;
                bodyColor.rgb += _VignetteColor.rgb * _VignetteIntensity * fillMask * effectFade * 0.10;

                // Final composite
                half4 finalColor = texColor;
                finalColor.rgb *= bodyColor.rgb;
                finalColor.rgb += (fillHead * _VignetteColor.rgb + sweep * _ActiveBorderColor.rgb) * fillMask * effectFade * 0.18;
                finalColor.rgb = lerp(finalColor.rgb, finalColor.rgb + frameGlow.rgb, borderMask * 0.75);
                finalColor.a *= alphaMask;
                finalColor.a = max(finalColor.a, borderMask * alphaMask * 0.9);

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(finalColor.a - 0.001);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}
