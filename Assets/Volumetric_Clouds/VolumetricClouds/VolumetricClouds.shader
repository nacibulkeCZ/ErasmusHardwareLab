Shader "Custom/VolumetricClouds_Endless"
{
    Properties
    {
        [Header(Main Appearance)]
        [MainColor] _BaseColor("Cloud Tint", Color) = (1, 1, 1, 1) // Color of the cloud matter
        _DensityMultiplier("Overall Density", Float) = 5.0
        
        [Header(Lighting)]
        _SunIntensity("Sun Brightness", Range(0, 10)) = 1.5
        [Color] _LightTint("Sun Tint", Color) = (1.0, 1.0, 1.0, 1)
        
        [Space(10)]
        _ShadowBrightness("Shadow Brightness", Range(0, 2)) = 0.5
        [Color] _ShadowTint("Shadow Tint", Color) = (0.6, 0.7, 0.8, 1) // Blueish tint for sky shadows
        
        [Space(10)]
        _HgPhase("Silver Lining (Phase)", Range(-1, 1)) = 0.5
        _Absorption("Light Absorption", Range(0, 10)) = 1.0

        [Header(Shape)]
        _NoiseVolume("Noise Volume (3D)", 3D) = "white" {}
        _CloudScale("Cloud Scale (World Space)", Float) = 0.01
        
        [Header(Layers)]
        _DensityThreshold("Shape Cutoff", Range(0, 1)) = 0.2
        _LayerSpacing("Layer Spacing", Float) = 0.5
        _LayerThickness("Layer Thickness", Range(0.005, 5.0)) = 0.5
        _LayerOffset("Layer Offset", Float) = 0.0

        [Header(Raymarching)]
        [IntRange] _Steps("Quality Steps", Range(16, 256)) = 64
        _MaxDistance("Render Distance", Float) = 500
        _DepthBlend("Depth Blend (Softness)", Range(0.1, 50.0)) = 10.0
        _StepSize("Step Size", Float) = 2.0
        _Jitter("Noise Grain", Range(0, 1)) = 1
        
        [Header(Animation)]
        _WindSpeed("Wind Speed", Vector) = (2, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        // CHANGED: Use Premultiplied Alpha blending to prevent dark outlines on edges
        Blend One OneMinusSrcAlpha
        ZWrite Off 
        // CHANGED: ZTest Always allows the shader to calculate depth manually. 
        ZTest Always 
        Cull Front 

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0; 
                float4 screenPos : TEXCOORD1;
            };

            TEXTURE3D(_NoiseVolume);
            SAMPLER(sampler_NoiseVolume);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _DensityMultiplier;
                float _SunIntensity;
                half4 _LightTint;
                float _ShadowBrightness;
                half4 _ShadowTint;
                float _HgPhase;
                float _Absorption;
                float4 _NoiseVolume_ST;
                float _CloudScale;
                float _DensityThreshold;
                float _LayerSpacing;
                float _LayerThickness;
                float _LayerOffset;
                int _Steps;        
                float _MaxDistance;
                float _DepthBlend;
                float _StepSize;
                float _Jitter;   
                float3 _WindSpeed;
            CBUFFER_END

            float InterleavedGradientNoise(float2 pixelCoords)
            {
                float3 magic = float3(0.06711056, 0.00583715, 52.9829189);
                return frac(magic.z * frac(dot(pixelCoords, magic.xy)));
            }

            float HG(float costheta, float g) {
                float g2 = g * g;
                return (1.0 - g2) / (4.0 * 3.1415 * pow(1.0 + g2 - 2.0 * g * costheta, 1.5));
            }

            float GetDensity(float3 p)
            {
                float3 animate = _Time.y * _WindSpeed;
                
                float noiseVal = SAMPLE_TEXTURE3D(_NoiseVolume, sampler_NoiseVolume, (p + animate) * _CloudScale).r;
                
                // --- NEW SINGLE LAYER LOGIC ---
                // 1. Get the position of the Object (The Cube) in World Space
                float3 objectPos = GetObjectToWorldMatrix()._m03_m13_m23;

                // 2. Calculate vertical distance from the center of the object (plus offset)
                float centerHeight = objectPos.y + _LayerOffset;
                float distFromCenter = abs(p.y - centerHeight);

                // 3. Define Thickness: We scale the 0-1 slider to roughly 100 world units 
                //    so the slider range is useful for large cloud layers.
                float layerHeight = _LayerThickness * 100.0; 
                
                // 4. Create Mask: 1.0 at center, fades to 0.0 at the edges defined by thickness
                float layerMask = 1.0 - smoothstep(0.0, layerHeight, distFromCenter);

                float finalNoise = noiseVal * layerMask;
                float density = max(0, finalNoise - _DensityThreshold);
                
                return density * _DensityMultiplier;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = vertexInput.positionCS;
                OUT.worldPos = vertexInput.positionWS;
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float2 pixelCoords = screenUV * _ScreenParams.xy;
                
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = normalize(IN.worldPos - rayOrigin);
                
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneDepthZ = LinearEyeDepth(rawDepth, _ZBufferParams);
                float viewDot = dot(rayDir, -UNITY_MATRIX_V[2].xyz);
                float realSceneDist = sceneDepthZ / viewDot;
                
                float distToBackFace = length(IN.worldPos - rayOrigin);
                float limit = min(realSceneDist, distToBackFace);
                limit = min(limit, _MaxDistance);

                float jitter = InterleavedGradientNoise(pixelCoords) * _Jitter;
                float3 currentPos = rayOrigin + (rayDir * _StepSize * jitter);
                float traveled = 0;

                // --- LIGHTING CALCS ---
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                
                // 1. Lit Color: BaseColor * SunColor * Intensity * Tint
                float3 litColor = _BaseColor.rgb * mainLight.color * _SunIntensity * _LightTint.rgb;
                
                // 2. Shadow Color: BaseColor * ShadowTint * Brightness
                float3 shadowColor = _BaseColor.rgb * _ShadowTint.rgb * _ShadowBrightness;

                float cosAngle = dot(rayDir, lightDir);
                float phaseVal = HG(cosAngle, _HgPhase);

                float totalDensity = 0;
                float transmittance = 1.0;
                float3 accumulatedColor = 0;

                [loop]
                for (int j = 0; j < _Steps; j++)
                {
                    if (traveled > limit) break;
                    
                    float density = GetDensity(currentPos);

                    if (density > 0)
                    {
                        // --- SOFT DEPTH BLEND (With Noise) ---
                        float distToGeo = realSceneDist - traveled;
                        float noisyBlend = _DepthBlend * (1.0 + (jitter - 0.5) * 0.5);
                        float depthFade = saturate(distToGeo / max(0.01, noisyBlend));
                        
                        density *= depthFade;

                        float stepAbsorb = density * _StepSize * _Absorption;
                        float prevTransmittance = transmittance;
                        transmittance *= exp(-stepAbsorb);
                        
                        float absorbedLight = prevTransmittance - transmittance;
                        
                        float lightTransmission = exp(-density);
                        float3 stepColor = lerp(shadowColor, litColor * phaseVal, lightTransmission);
                        
                        accumulatedColor += absorbedLight * stepColor;
                        
                        if (transmittance < 0.01) break;
                    }

                    currentPos += rayDir * _StepSize;
                    traveled += _StepSize;
                }
                
                float alpha = 1.0 - transmittance;
                return float4(accumulatedColor, alpha);
            }
            ENDHLSL
        }
    }
}