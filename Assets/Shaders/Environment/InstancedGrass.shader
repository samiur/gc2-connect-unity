// ABOUTME: GPU instanced grass blade shader for rendering thousands of grass blades.
// ABOUTME: Features SSS, ambient occlusion, color variation, and quality tier toggles.

Shader "OpenRange/InstancedGrass"
{
    Properties
    {
        [Header(Colors)]
        _BaseColor ("Base Color", Color) = (0.15, 0.45, 0.1, 1)
        _TipColor ("Tip Color", Color) = (0.3, 0.65, 0.2, 1)
        _TipBlendStart ("Tip Blend Start", Range(0, 1)) = 0.3
        _TipBlendEnd ("Tip Blend End", Range(0, 1)) = 1.0

        [Header(Wind)]
        _WindStrength ("Wind Strength", Range(0, 2)) = 1.0
        _WindSpeed ("Wind Speed", Range(0, 10)) = 2.0
        _WindTurbulence ("Wind Turbulence", Range(0, 2)) = 0.5
        _SecondaryWindScale ("Secondary Wind Scale", Range(0, 1)) = 0.4

        [Header(Subsurface Scattering)]
        _SubsurfaceColor ("Subsurface Color", Color) = (0.5, 0.8, 0.3, 1)
        _SubsurfacePower ("Subsurface Power", Range(0, 2)) = 0.8
        _SubsurfaceDistortion ("Subsurface Distortion", Range(0, 1)) = 0.5

        [Header(Ambient Occlusion)]
        _AOStrength ("AO Strength", Range(0, 1)) = 0.4
        _AOHeight ("AO Height", Range(0, 1)) = 0.3

        [Header(Color Variation)]
        _ColorVariation ("Color Variation", Range(0, 0.5)) = 0.15
        _ColorVariationScale ("Variation Scale", Range(0.01, 0.5)) = 0.1
        _VariationColor ("Variation Tint", Color) = (0.3, 0.4, 0.15, 1)

        [Header(Appearance)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.1
        _SpecularStrength ("Specular Strength", Range(0, 1)) = 0.1
        _WrapLighting ("Wrap Lighting", Range(0, 1)) = 0.5
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Quality)]
        [Toggle] _EnableSSS ("Enable Subsurface Scattering", Float) = 1
        [Toggle] _EnableAO ("Enable Ambient Occlusion", Float) = 1
        [Toggle] _EnableColorVariation ("Enable Color Variation", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 100
        Cull Off // Grass blades visible from both sides

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #pragma multi_compile_local _ _ENABLESSS_ON
            #pragma multi_compile_local _ _ENABLEAO_ON
            #pragma multi_compile_local _ _ENABLECOLORVARIATION_ON
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Per-instance data from compute buffer
            // Data is packed as 2 float4s per instance:
            //   _PositionBuffer[instanceID * 2 + 0] = position.xyz, rotation (radians)
            //   _PositionBuffer[instanceID * 2 + 1] = height, width, tilt, colorVariation
            StructuredBuffer<float4> _PositionBuffer;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                float heightFactor : TEXCOORD4;
                float colorVariation : TEXCOORD5;
                float4 shadowCoord : TEXCOORD6;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TipColor;
                float _TipBlendStart;
                float _TipBlendEnd;
                float _WindStrength;
                float _WindSpeed;
                float _WindTurbulence;
                float _SecondaryWindScale;
                float4 _SubsurfaceColor;
                float _SubsurfacePower;
                float _SubsurfaceDistortion;
                float _AOStrength;
                float _AOHeight;
                float _ColorVariation;
                float _ColorVariationScale;
                float4 _VariationColor;
                float _Smoothness;
                float _SpecularStrength;
                float _WrapLighting;
                float _Cutoff;
                float _EnableSSS;
                float _EnableAO;
                float _EnableColorVariation;
            CBUFFER_END

            // Global wind parameters (set by WindController)
            float4 _GlobalWindDirection;
            float _GlobalWindStrength;
            float _GlobalWindSpeed;
            float _GlobalWindTime;

            // Gradient noise functions for smoother wind
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float gradientNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                // Quintic interpolation for smoother results
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                float2 ga = hash2(i + float2(0.0, 0.0));
                float2 gb = hash2(i + float2(1.0, 0.0));
                float2 gc = hash2(i + float2(0.0, 1.0));
                float2 gd = hash2(i + float2(1.0, 1.0));

                float va = dot(ga, f - float2(0.0, 0.0));
                float vb = dot(gb, f - float2(1.0, 0.0));
                float vc = dot(gc, f - float2(0.0, 1.0));
                float vd = dot(gd, f - float2(1.0, 1.0));

                return lerp(lerp(va, vb, u.x), lerp(vc, vd, u.x), u.y);
            }

            // Simple hash for color variation
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            void setup()
            {
                // This function is called per instance for procedural instancing
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Get instance data (2 float4s per instance)
                uint baseIndex = input.instanceID * 2;
                float4 positionData = _PositionBuffer[baseIndex];
                float4 scaleData = _PositionBuffer[baseIndex + 1];

                float3 instancePos = positionData.xyz;
                float instanceRotation = positionData.w;
                float instanceHeight = scaleData.x;  // 0.08-0.2 meters
                float instanceWidth = scaleData.y;   // 0.015-0.03 meters
                float instanceTilt = scaleData.z;    // Forward tilt in radians
                float instanceColorVar = scaleData.w; // Per-instance color variation

                // Get vertex height factor (0 at base, 1 at tip)
                float heightFactor = saturate(input.positionOS.y);
                output.heightFactor = heightFactor;
                output.colorVariation = instanceColorVar;

                // Scale vertex by per-instance dimensions
                // The mesh is 1x1 unit, so scale it to actual grass blade size
                float3 scaledPos = float3(
                    input.positionOS.x * instanceWidth,
                    input.positionOS.y * instanceHeight,
                    input.positionOS.z * instanceWidth  // Z uses width for forward curve
                );

                // Apply forward tilt (only affects upper portion)
                float tiltAmount = heightFactor * sin(instanceTilt);
                scaledPos.z += tiltAmount * instanceHeight * 0.2;

                // Rotate vertex around Y axis
                float sinR = sin(instanceRotation);
                float cosR = cos(instanceRotation);
                float3 rotatedPos = float3(
                    scaledPos.x * cosR - scaledPos.z * sinR,
                    scaledPos.y,
                    scaledPos.x * sinR + scaledPos.z * cosR
                );

                // Wind animation with primary and secondary waves
                float windTime = _GlobalWindTime * _GlobalWindSpeed * _WindSpeed;
                float2 windUV = instancePos.xz * 0.1 + windTime * 0.5;

                // Primary wind wave
                float primaryNoise = gradientNoise(windUV);

                // Secondary wind wave (higher frequency detail)
                float secondaryNoise = gradientNoise(windUV * 2.3 + 100.0) * _SecondaryWindScale;

                float windNoise = primaryNoise * 0.6 + secondaryNoise * 0.4;

                // Additional turbulence
                float turbulence = gradientNoise(windUV * 2.0 + 100.0) * _WindTurbulence;

                float3 windDir = _GlobalWindDirection.xyz;
                if (length(windDir) < 0.01) windDir = float3(1, 0, 0); // Default wind direction

                // Wind effect increases with height (quadratic)
                float windEffect = heightFactor * heightFactor * _GlobalWindStrength * _WindStrength;
                float3 windDisplacement = windDir * (windNoise + turbulence) * windEffect * 0.3;

                // Add slight sway perpendicular to wind
                float swayNoise = gradientNoise(windUV * 1.5 + 50.0);
                float3 perpDir = cross(windDir, float3(0, 1, 0));
                windDisplacement += perpDir * swayNoise * windEffect * 0.1;

                // Apply displacement
                rotatedPos += windDisplacement;

                // Transform to world space
                float3 worldPos = rotatedPos + instancePos;

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                output.shadowCoord = TransformWorldToShadowCoord(worldPos);

                // Rotate normal
                float3 rotatedNormal = float3(
                    input.normalOS.x * cosR - input.normalOS.z * sinR,
                    input.normalOS.y,
                    input.normalOS.x * sinR + input.normalOS.z * cosR
                );
                output.normalWS = normalize(rotatedNormal);

                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float heightFactor = input.heightFactor;

                // Start with base color
                float3 baseColor = _BaseColor.rgb;

                // Color variation - combine world-space noise with per-instance variation
                #if defined(_ENABLECOLORVARIATION_ON)
                {
                    float worldVariation = valueNoise(input.positionWS.xz * _ColorVariationScale * 10.0);
                    float totalVariation = (worldVariation * 0.5 + input.colorVariation * 0.5) * _ColorVariation;
                    baseColor = lerp(baseColor, _VariationColor.rgb, totalVariation);
                }
                #endif

                // Blend between base and tip color based on height
                float blendFactor = smoothstep(_TipBlendStart, _TipBlendEnd, heightFactor);
                float3 albedo = lerp(baseColor, _TipColor.rgb, blendFactor);

                // Ambient occlusion - darken at base
                half ao = 1.0;
                #if defined(_ENABLEAO_ON)
                {
                    float aoFactor = smoothstep(0.0, _AOHeight, heightFactor);
                    ao = lerp(1.0 - _AOStrength, 1.0, aoFactor);
                }
                #endif

                // Lighting setup
                float3 normalWS = normalize(input.normalWS);

                // Flip normal for back faces to get correct lighting
                normalWS = input.positionCS.w > 0 ? normalWS : -normalWS;

                float3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                Light mainLight = GetMainLight(input.shadowCoord);
                float shadow = mainLight.shadowAttenuation;

                // Wrap lighting for softer diffuse
                float NdotL = dot(normalWS, mainLight.direction);
                float wrappedNdotL = saturate((NdotL + _WrapLighting) / (1.0 + _WrapLighting));

                float3 diffuse = albedo * mainLight.color * wrappedNdotL * shadow;

                // Subsurface scattering - grass glows when backlit
                half3 sssColor = half3(0, 0, 0);
                #if defined(_ENABLESSS_ON)
                {
                    // Calculate view-dependent backlight transmission
                    float3 H = normalize(mainLight.direction + normalWS * _SubsurfaceDistortion);
                    float VdotH = saturate(dot(viewDirWS, -H));
                    float sss = pow(VdotH, 3.0) * _SubsurfacePower;

                    // SSS is stronger at grass tips (thinner) and when backlit
                    float backFacing = saturate(-NdotL);
                    sss *= heightFactor * (0.5 + backFacing * 0.5);

                    sssColor = _SubsurfaceColor.rgb * mainLight.color * sss * shadow;
                }
                #endif

                // Anisotropic specular (Kajiya-Kay style for grass blades)
                float3 tangent = float3(0, 1, 0); // Grass blade direction (up)
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float TdotH = dot(tangent, halfDir);
                float sinTH = sqrt(max(0.0, 1.0 - TdotH * TdotH));
                float spec = pow(sinTH, 32.0 * _Smoothness + 8.0) * _SpecularStrength;
                half3 specularColor = spec * mainLight.color * shadow * saturate(NdotL);

                // Ambient
                float3 ambient = albedo * SampleSH(normalWS);

                // Combine all lighting
                float3 finalColor = (diffuse + sssColor + specularColor + ambient) * ao;

                // Fog
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            StructuredBuffer<float4> _PositionBuffer;

            struct Attributes
            {
                float4 positionOS : POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TipColor;
                float _TipBlendStart;
                float _TipBlendEnd;
                float _WindStrength;
                float _WindSpeed;
                float _WindTurbulence;
                float _SecondaryWindScale;
                float4 _SubsurfaceColor;
                float _SubsurfacePower;
                float _SubsurfaceDistortion;
                float _AOStrength;
                float _AOHeight;
                float _ColorVariation;
                float _ColorVariationScale;
                float4 _VariationColor;
                float _Smoothness;
                float _SpecularStrength;
                float _WrapLighting;
                float _Cutoff;
                float _EnableSSS;
                float _EnableAO;
                float _EnableColorVariation;
            CBUFFER_END

            float4 _GlobalWindDirection;
            float _GlobalWindStrength;
            float _GlobalWindSpeed;
            float _GlobalWindTime;

            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float gradientNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                float2 ga = hash2(i + float2(0.0, 0.0));
                float2 gb = hash2(i + float2(1.0, 0.0));
                float2 gc = hash2(i + float2(0.0, 1.0));
                float2 gd = hash2(i + float2(1.0, 1.0));

                float va = dot(ga, f - float2(0.0, 0.0));
                float vb = dot(gb, f - float2(1.0, 0.0));
                float vc = dot(gc, f - float2(0.0, 1.0));
                float vd = dot(gd, f - float2(1.0, 1.0));

                return lerp(lerp(va, vb, u.x), lerp(vc, vd, u.x), u.y);
            }

            float3 _LightDirection;

            void setup() {}

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;

                // Get instance data (2 float4s per instance)
                uint baseIndex = input.instanceID * 2;
                float4 positionData = _PositionBuffer[baseIndex];
                float4 scaleData = _PositionBuffer[baseIndex + 1];

                float3 instancePos = positionData.xyz;
                float instanceRotation = positionData.w;
                float instanceHeight = scaleData.x;
                float instanceWidth = scaleData.y;
                float instanceTilt = scaleData.z;

                // Get vertex height factor
                float heightFactor = saturate(input.positionOS.y);

                // Scale vertex
                float3 scaledPos = float3(
                    input.positionOS.x * instanceWidth,
                    input.positionOS.y * instanceHeight,
                    input.positionOS.z * instanceWidth
                );

                // Apply tilt
                float tiltAmount = heightFactor * sin(instanceTilt);
                scaledPos.z += tiltAmount * instanceHeight * 0.2;

                // Rotate
                float sinR = sin(instanceRotation);
                float cosR = cos(instanceRotation);
                float3 rotatedPos = float3(
                    scaledPos.x * cosR - scaledPos.z * sinR,
                    scaledPos.y,
                    scaledPos.x * sinR + scaledPos.z * cosR
                );

                // Wind animation (simplified for shadow pass)
                float windTime = _GlobalWindTime * _GlobalWindSpeed * _WindSpeed;
                float2 windUV = instancePos.xz * 0.1 + windTime * 0.5;
                float windNoise = gradientNoise(windUV);

                float3 windDir = _GlobalWindDirection.xyz;
                if (length(windDir) < 0.01) windDir = float3(1, 0, 0);

                float windEffect = heightFactor * heightFactor * _GlobalWindStrength * _WindStrength;
                rotatedPos += windDir * windNoise * windEffect * 0.3;

                float3 worldPos = rotatedPos + instancePos;
                output.positionCS = TransformWorldToHClip(worldPos);

                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
