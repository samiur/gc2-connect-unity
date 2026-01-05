// ABOUTME: Stylized grass shader for URP with wind animation via vertex displacement.
// ABOUTME: Supports global wind parameters, height-based tip movement, and quality tier toggles.

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

        [Header(Lighting)]
        _ShadowColor ("Shadow Tint", Color) = (0.1, 0.2, 0.1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.2
        _SpecularStrength ("Specular Strength", Range(0, 1)) = 0.1

        [Header(Quality)]
        [Toggle] _EnableWind ("Enable Wind Animation", Float) = 1
        [Toggle] _EnableTipColor ("Enable Tip Color", Float) = 1
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
                half4 _ShadowColor;
                half _Smoothness;
                half _SpecularStrength;
                half _EnableWind;
                half _EnableTipColor;
            CBUFFER_END

            // Global wind parameters set by WindController
            float4 _GlobalWindDirection;  // xyz = normalized direction, w = enabled
            float _GlobalWindSpeed;       // Speed multiplier from settings
            float _GlobalWindStrength;    // Strength multiplier from settings
            float _GlobalWindTime;        // Animated time value

            // Simple noise function
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
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

            // Calculate wind displacement
            float3 CalculateWindDisplacement(float3 positionWS, float heightFactor)
            {
                // Get global wind settings (or use local if globals not set)
                float3 windDir = _GlobalWindDirection.w > 0.5 ? _GlobalWindDirection.xyz : float3(1, 0, 0);
                float windSpeedMult = _GlobalWindSpeed > 0.01 ? _GlobalWindSpeed : 1.0;
                float windStrengthMult = _GlobalWindStrength > 0.01 ? _GlobalWindStrength : 1.0;
                float time = _GlobalWindTime > 0.01 ? _GlobalWindTime : _Time.y;

                // Base wind animation
                float windPhase = dot(positionWS.xz, windDir.xz * 0.1) + time * _WindSpeed * windSpeedMult;
                float windWave = sin(windPhase) * 0.5 + 0.5;

                // Add turbulence
                float2 turbulenceUV = positionWS.xz * _WindTurbulence * 0.2;
                float turbulence = noise(turbulenceUV + time * windSpeedMult * 0.5) * 2.0 - 1.0;

                // Add gusts
                float gustPhase = time * _GustFrequency + dot(positionWS.xz, float2(0.3, 0.7));
                float gust = pow(max(0, sin(gustPhase)), 4.0) * _GustStrength;

                // Combine wind effects
                float windInfluence = (windWave + turbulence * 0.3 + gust) * _WindStrength * windStrengthMult;

                // Height-based influence - grass tips move more than base
                float heightInfluence = pow(heightFactor, 2.0);

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
                // Height-based color blending
                float heightFactor = saturate(input.uv.y);
                float tipBlend = smoothstep(_TipBlendStart, _TipBlendEnd, heightFactor);

                half3 baseColor = _BaseColor.rgb;

                #if defined(_ENABLETIPCOLOR_ON)
                baseColor = lerp(_BaseColor.rgb, _TipColor.rgb, tipBlend);
                #endif

                // Get main light
                Light mainLight = GetMainLight(input.shadowCoord);
                float3 normalWS = normalize(input.normalWS);

                // Simple diffuse lighting
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float shadow = mainLight.shadowAttenuation;

                // Apply shadow with tint
                half3 shadowedColor = lerp(_ShadowColor.rgb, half3(1, 1, 1), NdotL * shadow);
                half3 finalColor = baseColor * mainLight.color * shadowedColor;

                // Simple specular highlight
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float spec = pow(saturate(dot(normalWS, halfDir)), 32.0 * _Smoothness + 1.0);
                finalColor += spec * _SpecularStrength * mainLight.color * shadow;

                // Add ambient
                half3 ambient = SampleSH(normalWS);
                finalColor += baseColor * ambient * 0.5;

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
                half4 _ShadowColor;
                half _Smoothness;
                half _SpecularStrength;
                half _EnableWind;
                half _EnableTipColor;
            CBUFFER_END

            float4 _GlobalWindDirection;
            float _GlobalWindSpeed;
            float _GlobalWindStrength;
            float _GlobalWindTime;

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
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

            float3 CalculateWindDisplacementShadow(float3 positionWS, float heightFactor)
            {
                float3 windDir = _GlobalWindDirection.w > 0.5 ? _GlobalWindDirection.xyz : float3(1, 0, 0);
                float windSpeedMult = _GlobalWindSpeed > 0.01 ? _GlobalWindSpeed : 1.0;
                float windStrengthMult = _GlobalWindStrength > 0.01 ? _GlobalWindStrength : 1.0;
                float time = _GlobalWindTime > 0.01 ? _GlobalWindTime : _Time.y;

                float windPhase = dot(positionWS.xz, windDir.xz * 0.1) + time * _WindSpeed * windSpeedMult;
                float windWave = sin(windPhase) * 0.5 + 0.5;
                float2 turbulenceUV = positionWS.xz * _WindTurbulence * 0.2;
                float turbulence = noise(turbulenceUV + time * windSpeedMult * 0.5) * 2.0 - 1.0;
                float gustPhase = time * _GustFrequency + dot(positionWS.xz, float2(0.3, 0.7));
                float gust = pow(max(0, sin(gustPhase)), 4.0) * _GustStrength;
                float windInfluence = (windWave + turbulence * 0.3 + gust) * _WindStrength * windStrengthMult;
                float heightInfluence = pow(heightFactor, 2.0);

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
                half4 _ShadowColor;
                half _Smoothness;
                half _SpecularStrength;
                half _EnableWind;
                half _EnableTipColor;
            CBUFFER_END

            float4 _GlobalWindDirection;
            float _GlobalWindSpeed;
            float _GlobalWindStrength;
            float _GlobalWindTime;

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
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

            float3 CalculateWindDisplacementDepth(float3 positionWS, float heightFactor)
            {
                float3 windDir = _GlobalWindDirection.w > 0.5 ? _GlobalWindDirection.xyz : float3(1, 0, 0);
                float windSpeedMult = _GlobalWindSpeed > 0.01 ? _GlobalWindSpeed : 1.0;
                float windStrengthMult = _GlobalWindStrength > 0.01 ? _GlobalWindStrength : 1.0;
                float time = _GlobalWindTime > 0.01 ? _GlobalWindTime : _Time.y;

                float windPhase = dot(positionWS.xz, windDir.xz * 0.1) + time * _WindSpeed * windSpeedMult;
                float windWave = sin(windPhase) * 0.5 + 0.5;
                float2 turbulenceUV = positionWS.xz * _WindTurbulence * 0.2;
                float turbulence = noise(turbulenceUV + time * windSpeedMult * 0.5) * 2.0 - 1.0;
                float gustPhase = time * _GustFrequency + dot(positionWS.xz, float2(0.3, 0.7));
                float gust = pow(max(0, sin(gustPhase)), 4.0) * _GustStrength;
                float windInfluence = (windWave + turbulence * 0.3 + gust) * _WindStrength * windStrengthMult;
                float heightInfluence = pow(heightFactor, 2.0);

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
