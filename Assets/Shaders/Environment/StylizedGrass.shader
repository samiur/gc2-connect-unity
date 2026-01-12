// ABOUTME: Stylized grass shader for URP with wind animation via vertex displacement.
// ABOUTME: Features SSS, ambient occlusion, color variation, and quality tier toggles.

Shader "OpenRange/StylizedGrass"
{
    Properties
    {
        [Header(Colors)]
        _BaseColor ("Base Color", Color) = (0.2, 0.5, 0.1, 1)
        _TipColor ("Tip Color", Color) = (0.4, 0.7, 0.2, 1)
        _TipBlendStart ("Tip Blend Start", Range(0, 1)) = 0.3
        _TipBlendEnd ("Tip Blend End", Range(0, 1)) = 1.0

        [Header(Grass Properties)]
        _GrassHeight ("Grass Height", Range(0, 2)) = 0.5
        _GrassWidth ("Grass Width", Range(0, 0.5)) = 0.1

        [Header(Wind)]
        _WindStrength ("Wind Strength", Range(0, 2)) = 1.0
        _WindSpeed ("Wind Speed", Range(0, 10)) = 2.0
        _WindTurbulence ("Wind Turbulence", Range(0, 2)) = 0.5
        _GustStrength ("Gust Strength", Range(0, 1)) = 0.3
        _GustFrequency ("Gust Frequency", Range(0, 5)) = 1.0
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

        [Header(Lighting)]
        _ShadowColor ("Shadow Tint", Color) = (0.1, 0.2, 0.1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.2
        _SpecularStrength ("Specular Strength", Range(0, 1)) = 0.1
        _WrapLighting ("Wrap Lighting", Range(0, 1)) = 0.5

        [Header(Quality)]
        [Toggle] _EnableWind ("Enable Wind Animation", Float) = 1
        [Toggle] _EnableTipColor ("Enable Tip Color", Float) = 1
        [Toggle] _EnableSSS ("Enable Subsurface Scattering", Float) = 1
        [Toggle] _EnableAO ("Enable Ambient Occlusion", Float) = 1
        [Toggle] _EnableColorVariation ("Enable Color Variation", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ _ENABLEWIND_ON
            #pragma multi_compile_local _ _ENABLETIPCOLOR_ON
            #pragma multi_compile_local _ _ENABLESSS_ON
            #pragma multi_compile_local _ _ENABLEAO_ON
            #pragma multi_compile_local _ _ENABLECOLORVARIATION_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 color : COLOR;
                float fogFactor : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                float heightFactor : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Material properties
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _TipColor;
                half _TipBlendStart;
                half _TipBlendEnd;
                half _GrassHeight;
                half _GrassWidth;
                half _WindStrength;
                half _WindSpeed;
                half _WindTurbulence;
                half _GustStrength;
                half _GustFrequency;
                half _SecondaryWindScale;
                half4 _SubsurfaceColor;
                half _SubsurfacePower;
                half _SubsurfaceDistortion;
                half _AOStrength;
                half _AOHeight;
                half _ColorVariation;
                half _ColorVariationScale;
                half4 _VariationColor;
                half4 _ShadowColor;
                half _Smoothness;
                half _SpecularStrength;
                half _WrapLighting;
                half _EnableWind;
                half _EnableTipColor;
                half _EnableSSS;
                half _EnableAO;
                half _EnableColorVariation;
            CBUFFER_END

            // Global wind parameters set by WindController
            float4 _GlobalWindDirection;  // xyz = normalized direction, w = enabled
            float _GlobalWindSpeed;       // Speed multiplier from settings
            float _GlobalWindStrength;    // Strength multiplier from settings
            float _GlobalWindTime;        // Animated time value

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

            // Simple hash for color variation (faster than gradient noise)
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

            // Calculate wind displacement with primary and secondary waves
            float3 CalculateWindDisplacement(float3 positionWS, float heightFactor)
            {
                // Get global wind settings (or use local if globals not set)
                float3 windDir = _GlobalWindDirection.w > 0.5 ? _GlobalWindDirection.xyz : float3(1, 0, 0);
                float windSpeedMult = _GlobalWindSpeed > 0.01 ? _GlobalWindSpeed : 1.0;
                float windStrengthMult = _GlobalWindStrength > 0.01 ? _GlobalWindStrength : 1.0;
                float time = _GlobalWindTime > 0.01 ? _GlobalWindTime : _Time.y;

                // Primary wind wave
                float windPhase = dot(positionWS.xz, windDir.xz * 0.1) + time * _WindSpeed * windSpeedMult;
                float primaryWave = sin(windPhase) * 0.6;

                // Secondary wind wave (higher frequency detail)
                float secondaryPhase = windPhase * 2.3 + positionWS.x * 0.5;
                float secondaryWave = sin(secondaryPhase) * _SecondaryWindScale * 0.4;

                float windWave = (primaryWave + secondaryWave) * 0.5 + 0.5;

                // Add turbulence using gradient noise for smoother motion
                float2 turbulenceUV = positionWS.xz * _WindTurbulence * 0.2;
                float turbulence = gradientNoise(turbulenceUV + time * windSpeedMult * 0.5);

                // Add gusts
                float gustPhase = time * _GustFrequency + dot(positionWS.xz, float2(0.3, 0.7));
                float gust = pow(max(0, sin(gustPhase)), 4.0) * _GustStrength;

                // Combine wind effects
                float windInfluence = (windWave + turbulence * 0.3 + gust) * _WindStrength * windStrengthMult;

                // Height-based influence - grass tips move more than base (quadratic falloff)
                float heightInfluence = heightFactor * heightFactor;

                // Calculate displacement
                float3 displacement = float3(0, 0, 0);
                displacement.xz = windDir.xz * windInfluence * heightInfluence * _GrassHeight;
                displacement.y = -abs(windInfluence) * heightInfluence * 0.1; // Slight downward bend

                return displacement;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = input.positionOS.xyz;
                float3 positionWS = TransformObjectToWorld(positionOS);

                // Use vertex color or UV.y for height factor (grass blades typically have UV.y = 0 at base, 1 at tip)
                float heightFactor = saturate(input.uv.y);
                output.heightFactor = heightFactor;

                #if defined(_ENABLEWIND_ON)
                if (_GlobalWindDirection.w > 0.5 || _EnableWind > 0.5)
                {
                    float3 windDisplacement = CalculateWindDisplacement(positionWS, heightFactor);
                    positionWS += windDisplacement;
                }
                #endif

                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.shadowCoord = TransformWorldToShadowCoord(positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float heightFactor = input.heightFactor;

                // Start with base color
                half3 baseColor = _BaseColor.rgb;

                // Color variation based on world position
                #if defined(_ENABLECOLORVARIATION_ON)
                {
                    float variation = valueNoise(input.positionWS.xz * _ColorVariationScale * 10.0);
                    variation = variation * _ColorVariation;
                    baseColor = lerp(baseColor, _VariationColor.rgb, variation);
                }
                #endif

                // Height-based tip color blending
                #if defined(_ENABLETIPCOLOR_ON)
                {
                    float tipBlend = smoothstep(_TipBlendStart, _TipBlendEnd, heightFactor);
                    baseColor = lerp(baseColor, _TipColor.rgb, tipBlend);
                }
                #endif

                // Ambient occlusion - darken at base
                half ao = 1.0;
                #if defined(_ENABLEAO_ON)
                {
                    float aoFactor = smoothstep(0.0, _AOHeight, heightFactor);
                    ao = lerp(1.0 - _AOStrength, 1.0, aoFactor);
                }
                #endif

                // Get main light
                Light mainLight = GetMainLight(input.shadowCoord);
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);

                // Wrap lighting for softer diffuse (grass is thin and scatters light)
                float NdotL = dot(normalWS, mainLight.direction);
                float wrappedNdotL = saturate((NdotL + _WrapLighting) / (1.0 + _WrapLighting));
                float shadow = mainLight.shadowAttenuation;

                // Apply shadow with tint
                half3 shadowedColor = lerp(_ShadowColor.rgb, half3(1, 1, 1), wrappedNdotL * shadow);
                half3 diffuseColor = baseColor * mainLight.color * shadowedColor;

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

                // Add ambient
                half3 ambient = SampleSH(normalWS);
                half3 ambientColor = baseColor * ambient * 0.5;

                // Combine all lighting
                half3 finalColor = (diffuseColor + sssColor + specularColor + ambientColor) * ao;

                // Apply fog
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
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_local _ _ENABLEWIND_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _TipColor;
                half _TipBlendStart;
                half _TipBlendEnd;
                half _GrassHeight;
                half _GrassWidth;
                half _WindStrength;
                half _WindSpeed;
                half _WindTurbulence;
                half _GustStrength;
                half _GustFrequency;
                half _SecondaryWindScale;
                half4 _SubsurfaceColor;
                half _SubsurfacePower;
                half _SubsurfaceDistortion;
                half _AOStrength;
                half _AOHeight;
                half _ColorVariation;
                half _ColorVariationScale;
                half4 _VariationColor;
                half4 _ShadowColor;
                half _Smoothness;
                half _SpecularStrength;
                half _WrapLighting;
                half _EnableWind;
                half _EnableTipColor;
                half _EnableSSS;
                half _EnableAO;
                half _EnableColorVariation;
            CBUFFER_END

            float4 _GlobalWindDirection;
            float _GlobalWindSpeed;
            float _GlobalWindStrength;
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

            float3 CalculateWindDisplacementShadow(float3 positionWS, float heightFactor)
            {
                float3 windDir = _GlobalWindDirection.w > 0.5 ? _GlobalWindDirection.xyz : float3(1, 0, 0);
                float windSpeedMult = _GlobalWindSpeed > 0.01 ? _GlobalWindSpeed : 1.0;
                float windStrengthMult = _GlobalWindStrength > 0.01 ? _GlobalWindStrength : 1.0;
                float time = _GlobalWindTime > 0.01 ? _GlobalWindTime : _Time.y;

                float windPhase = dot(positionWS.xz, windDir.xz * 0.1) + time * _WindSpeed * windSpeedMult;
                float primaryWave = sin(windPhase) * 0.6;
                float secondaryPhase = windPhase * 2.3 + positionWS.x * 0.5;
                float secondaryWave = sin(secondaryPhase) * _SecondaryWindScale * 0.4;
                float windWave = (primaryWave + secondaryWave) * 0.5 + 0.5;

                float2 turbulenceUV = positionWS.xz * _WindTurbulence * 0.2;
                float turbulence = gradientNoise(turbulenceUV + time * windSpeedMult * 0.5);
                float gustPhase = time * _GustFrequency + dot(positionWS.xz, float2(0.3, 0.7));
                float gust = pow(max(0, sin(gustPhase)), 4.0) * _GustStrength;
                float windInfluence = (windWave + turbulence * 0.3 + gust) * _WindStrength * windStrengthMult;
                float heightInfluence = heightFactor * heightFactor;

                float3 displacement = float3(0, 0, 0);
                displacement.xz = windDir.xz * windInfluence * heightInfluence * _GrassHeight;
                displacement.y = -abs(windInfluence) * heightInfluence * 0.1;
                return displacement;
            }

            float3 _LightDirection;

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = input.positionOS.xyz;
                float3 positionWS = TransformObjectToWorld(positionOS);
                float heightFactor = saturate(input.uv.y);

                #if defined(_ENABLEWIND_ON)
                if (_GlobalWindDirection.w > 0.5 || _EnableWind > 0.5)
                {
                    float3 windDisplacement = CalculateWindDisplacementShadow(positionWS, heightFactor);
                    positionWS += windDisplacement;
                }
                #endif

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // Depth pass for depth prepass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_local _ _ENABLEWIND_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _TipColor;
                half _TipBlendStart;
                half _TipBlendEnd;
                half _GrassHeight;
                half _GrassWidth;
                half _WindStrength;
                half _WindSpeed;
                half _WindTurbulence;
                half _GustStrength;
                half _GustFrequency;
                half _SecondaryWindScale;
                half4 _SubsurfaceColor;
                half _SubsurfacePower;
                half _SubsurfaceDistortion;
                half _AOStrength;
                half _AOHeight;
                half _ColorVariation;
                half _ColorVariationScale;
                half4 _VariationColor;
                half4 _ShadowColor;
                half _Smoothness;
                half _SpecularStrength;
                half _WrapLighting;
                half _EnableWind;
                half _EnableTipColor;
                half _EnableSSS;
                half _EnableAO;
                half _EnableColorVariation;
            CBUFFER_END

            float4 _GlobalWindDirection;
            float _GlobalWindSpeed;
            float _GlobalWindStrength;
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

            float3 CalculateWindDisplacementDepth(float3 positionWS, float heightFactor)
            {
                float3 windDir = _GlobalWindDirection.w > 0.5 ? _GlobalWindDirection.xyz : float3(1, 0, 0);
                float windSpeedMult = _GlobalWindSpeed > 0.01 ? _GlobalWindSpeed : 1.0;
                float windStrengthMult = _GlobalWindStrength > 0.01 ? _GlobalWindStrength : 1.0;
                float time = _GlobalWindTime > 0.01 ? _GlobalWindTime : _Time.y;

                float windPhase = dot(positionWS.xz, windDir.xz * 0.1) + time * _WindSpeed * windSpeedMult;
                float primaryWave = sin(windPhase) * 0.6;
                float secondaryPhase = windPhase * 2.3 + positionWS.x * 0.5;
                float secondaryWave = sin(secondaryPhase) * _SecondaryWindScale * 0.4;
                float windWave = (primaryWave + secondaryWave) * 0.5 + 0.5;

                float2 turbulenceUV = positionWS.xz * _WindTurbulence * 0.2;
                float turbulence = gradientNoise(turbulenceUV + time * windSpeedMult * 0.5);
                float gustPhase = time * _GustFrequency + dot(positionWS.xz, float2(0.3, 0.7));
                float gust = pow(max(0, sin(gustPhase)), 4.0) * _GustStrength;
                float windInfluence = (windWave + turbulence * 0.3 + gust) * _WindStrength * windStrengthMult;
                float heightInfluence = heightFactor * heightFactor;

                float3 displacement = float3(0, 0, 0);
                displacement.xz = windDir.xz * windInfluence * heightInfluence * _GrassHeight;
                displacement.y = -abs(windInfluence) * heightInfluence * 0.1;
                return displacement;
            }

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = input.positionOS.xyz;
                float3 positionWS = TransformObjectToWorld(positionOS);
                float heightFactor = saturate(input.uv.y);

                #if defined(_ENABLEWIND_ON)
                if (_GlobalWindDirection.w > 0.5 || _EnableWind > 0.5)
                {
                    float3 windDisplacement = CalculateWindDisplacementDepth(positionWS, heightFactor);
                    positionWS += windDisplacement;
                }
                #endif

                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
