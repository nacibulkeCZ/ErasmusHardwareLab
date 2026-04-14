Shader "UI/VRPanelHighlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _HighlightAmount ("Highlight Amount", Range(0, 1)) = 0
        
        [Header(Shape Settings)]
        _AspectRatio ("Aspect Ratio (Width/Height)", Float) = 1.0
        _CornerRadius ("Corner Radius", Range(0, 2)) = 0.2
        _BorderThickness ("Border Thickness", Range(0.001, 0.5)) = 0.04
        
        [Header(Idle State)]
        _IdleBorderColor ("Idle Border Color", Color) = (0.5, 0.5, 0.5, 1)        [HDR] _IdleGradientTop ("Idle Gradient Top", Color) = (0.2, 0.2, 0.2, 0.8)
        [HDR] _IdleGradientBottom ("Idle Gradient Bottom", Color) = (0.05, 0.05, 0.05, 0.9)
        [Header(Highlight State)]
        [HDR] _ActiveBorderColor ("Active Border Glow Color", Color) = (0.2, 0.9, 1.0, 1)
        [HDR] _VignetteColor ("Vignette Glow Color", Color) = (0.1, 0.6, 1.0, 1)
        _VignetteIntensity ("Vignette Intensity", Float) = 1.5
        _AnimSpeed ("Animation Speed", Float) = 3.0

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
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
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

            float4 _IdleBorderColor;
            float4 _IdleGradientTop;
            float4 _IdleGradientBottom;
            
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

                float fw = length(float2(ddx(d), ddy(d))); 
                fw = max(fw, 0.003);

                float alphaMask = 1.0 - smoothstep(-fw, fw, d);

                float halfThickness = _BorderThickness * 0.5;
                float dBorder = abs(d + halfThickness) - halfThickness;
                float borderMask = 1.0 - smoothstep(-fw, fw, dBorder);

                float angle = atan2(uv.y, uv.x);
                float t = _Time.y * _AnimSpeed;
                
                float wave = sin(angle * 4.0 + t) 
                           + sin(angle * 7.0 - t * 1.5) * 0.5 
                           + cos(angle * 2.0 + t * 2.0) * 0.5;
                wave = smoothstep(0.2, 0.8, (wave + 2.0) / 4.0) + 0.25; 
                float pulse = 0.85 + 0.15 * sin(t * 4.0);
                
                float4 activeBorderC = _ActiveBorderColor * wave * pulse;
                float4 currentBorder = lerp(_IdleBorderColor, activeBorderC, _HighlightAmount);

                // Idle state subtle vertical gradient
                float verticalFactor = saturate((uv.y / extents.y) * 0.5 + 0.5); // Map to 0-1 range
                float4 idleGradient = lerp(_IdleGradientBottom, _IdleGradientTop, verticalFactor);

                // Highlight glowing Vignette mapping on background based on UV depth from center
                float vignetteMask = saturate(1.0 - length(IN.texcoord - float2(0.5, 0.5)) * 1.5);
                vignetteMask = pow(vignetteMask, 1.5); 
                float4 vignetteGlow = _VignetteColor * vignetteMask * _VignetteIntensity;

                // Blend between Idle Gradient and Active Vignette Glow based on highlight
                float4 backgroundEffect = lerp(idleGradient, vignetteGlow, _HighlightAmount);

                half4 finalColor = texColor;
                finalColor.a *= alphaMask;

                // Modulate main texture color with the background effect based on its opacity
                finalColor.rgb = lerp(finalColor.rgb, finalColor.rgb + backgroundEffect.rgb, backgroundEffect.a * alphaMask);
                finalColor.a = max(finalColor.a, backgroundEffect.a * alphaMask);

                finalColor.rgb = lerp(finalColor.rgb, currentBorder.rgb, borderMask * currentBorder.a);
                finalColor.a = max(finalColor.a, borderMask * currentBorder.a * alphaMask);

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (finalColor.a - 0.001);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}
