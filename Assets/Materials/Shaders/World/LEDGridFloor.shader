Shader "Custom/LEDGridFloor"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (0.005, 0.025, 0.07, 1)
        [HDR] _LineColor ("Line Color", Color) = (0.04, 0.7, 1.4, 1)
        [HDR] _MajorLineColor ("Major Line Color", Color) = (0.12, 1.0, 2.0, 1)

        _GridScale ("Grid Cell Size", Float) = 1.0
        _LineWidth ("Line Width", Range(0.001, 0.08)) = 0.018
        _MajorLineEvery ("Major Line Every N Cells", Float) = 4.0
        _MajorLineWidth ("Major Line Width", Range(0.001, 0.12)) = 0.028

        _GlowStrength ("Glow Strength", Range(0, 8)) = 2.0
        _PulseSpeed ("Pulse Speed", Range(0, 8)) = 0.8
        _FadeDistance ("Distance Fade", Range(0, 80)) = 35.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _LineColor;
                float4 _MajorLineColor;
                float _GridScale;
                float _LineWidth;
                float _MajorLineEvery;
                float _MajorLineWidth;
                float _GlowStrength;
                float _PulseSpeed;
                float _FadeDistance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            float GridMask(float2 gridCoord, float lineWidth)
            {
                float2 cellPosition = frac(gridCoord);
                float2 cellDistance = min(cellPosition, 1.0 - cellPosition);
                float2 derivatives = max(fwidth(gridCoord), 0.0001);
                float2 lines = 1.0 - smoothstep(lineWidth, lineWidth + derivatives, cellDistance);
                return saturate(max(lines.x, lines.y));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float gridScale = max(_GridScale, 0.001);
                float2 gridCoord = IN.positionWS.xz / gridScale;

                float minorLines = GridMask(gridCoord, _LineWidth);

                float majorEvery = max(_MajorLineEvery, 1.0);
                float majorLines = GridMask(gridCoord / majorEvery, _MajorLineWidth);

                float distanceFade = 1.0;
                if (_FadeDistance > 0.001)
                {
                    float cameraDistance = distance(GetCameraPositionWS(), IN.positionWS);
                    distanceFade = saturate(1.0 - cameraDistance / _FadeDistance);
                }

                float pulse = 0.85 + 0.15 * sin(_Time.y * _PulseSpeed);
                float lineMask = saturate(minorLines + majorLines);
                float majorMask = saturate(majorLines);

                float3 color = _BaseColor.rgb;
                color += _LineColor.rgb * minorLines * _GlowStrength * pulse;
                color += _MajorLineColor.rgb * majorMask * _GlowStrength * 1.35 * pulse;

                color = lerp(_BaseColor.rgb, color, distanceFade);

                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
