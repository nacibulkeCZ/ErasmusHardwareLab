Shader "UI/CircleProgress"
{
    Properties
    {
        _FillAmount ("Fill Amount", Range(0, 1)) = 0
        _FillColor ("Fill Color", Color) = (0, 1, 0, 1)
        _BackgroundColor ("Background Color", Color) = (0.2, 0.2, 0.2, 0.5)
        _Radius ("Radius", Range(0, 0.5)) = 0.45
        _Thickness ("Thickness", Range(0, 0.5)) = 0.08
        _AntiAlias ("Anti-Alias", Range(0, 0.05)) = 0.01
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _FillAmount;
            float4 _FillColor;
            float4 _BackgroundColor;
            float _Radius;
            float _Thickness;
            float _AntiAlias;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 center = i.uv - 0.5;
                float dist = length(center);

                float innerEdge = _Radius - _Thickness;
                float ring = smoothstep(innerEdge - _AntiAlias, innerEdge + _AntiAlias, dist)
                           * (1.0 - smoothstep(_Radius - _AntiAlias, _Radius + _AntiAlias, dist));

                if (ring < 0.001)
                    discard;

                // Angle from top (12 o'clock), clockwise, 0..1
                float angle = atan2(center.x, center.y);
                float normalized = 1.0 - (angle / (2.0 * 3.14159265) + 0.5);
                normalized = frac(normalized + 0.5);

                float isFilled = step(normalized, _FillAmount);

                fixed4 col = lerp(_BackgroundColor, _FillColor, isFilled);
                col.a *= ring;

                return col;
            }
            ENDCG
        }
    }
}
