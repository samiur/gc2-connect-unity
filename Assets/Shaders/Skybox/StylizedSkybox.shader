// ABOUTME: Procedural stylized skybox shader for URP with gradient sky, sun, and clouds.
// ABOUTME: Supports quality tier adjustments and HDR output for bloom effects.

Shader "OpenRange/StylizedSkybox"
{
    Properties
    {
        [Header(Sky Colors)]
        _TopColor ("Top Color", Color) = (0.1, 0.3, 0.8, 1)
        _HorizonColor ("Horizon Color", Color) = (1.0, 0.6, 0.4, 1)
        _GroundColor ("Ground Color", Color) = (0.3, 0.25, 0.2, 1)
        _GradientExponent ("Gradient Exponent", Range(0.5, 4.0)) = 1.5

        [Header(Sun)]
        _SunColor ("Sun Color (HDR)", Color) = (1.5, 1.4, 1.0, 1)
        _SunSize ("Sun Size", Range(0.01, 0.2)) = 0.05
        _SunFalloff ("Sun Falloff", Range(1, 100)) = 50
        _SunDirection ("Sun Direction", Vector) = (0.3, 0.5, -0.8, 0)

        [Header(Clouds)]
        _CloudDensity ("Cloud Density", Range(0, 1)) = 0.4
        _CloudSpeed ("Cloud Speed", Range(0, 0.1)) = 0.01
        _CloudScale ("Cloud Scale", Range(1, 20)) = 8
        _CloudColor ("Cloud Color", Color) = (1, 1, 1, 1)
        _CloudShadowColor ("Cloud Shadow Color", Color) = (0.6, 0.6, 0.7, 1)
        _CloudHeight ("Cloud Height", Range(0, 1)) = 0.3

        [Header(Horizon)]
        _HorizonFogDensity ("Horizon Fog Density", Range(0, 1)) = 0.3
        _HorizonFogHeight ("Horizon Fog Height", Range(0, 1)) = 0.1

        [Header(Quality)]
        [Toggle] _EnableClouds ("Enable Clouds", Float) = 1
        [Toggle] _EnableSun ("Enable Sun", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "StylizedSkybox"
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ _ENABLECLOUDS_ON
            #pragma multi_compile_local _ _ENABLESUN_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDir : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                half _GradientExponent;

                half4 _SunColor;
                half _SunSize;
                half _SunFalloff;
                float4 _SunDirection;

                half _CloudDensity;
                half _CloudSpeed;
                half _CloudScale;
                half4 _CloudColor;
                half4 _CloudShadowColor;
                half _CloudHeight;

                half _HorizonFogDensity;
                half _HorizonFogHeight;

                half _EnableClouds;
                half _EnableSun;
            CBUFFER_END

            // Simple noise function for clouds
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

            float fbm(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;

                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * noise(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }

                return value;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.viewDir = input.positionOS.xyz;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDir = normalize(input.viewDir);

                // Calculate vertical gradient factor
                float verticalFactor = viewDir.y;
                float skyFactor = saturate(pow(max(0, verticalFactor), _GradientExponent));
                float groundFactor = saturate(pow(max(0, -verticalFactor), _GradientExponent * 0.5));

                // Base sky gradient
                half3 skyColor = lerp(_HorizonColor.rgb, _TopColor.rgb, skyFactor);
                skyColor = lerp(skyColor, _GroundColor.rgb, groundFactor);

                // Horizon fog effect
                float horizonMask = 1.0 - saturate(abs(verticalFactor) / _HorizonFogHeight);
                horizonMask = pow(horizonMask, 2.0) * _HorizonFogDensity;
                skyColor = lerp(skyColor, _HorizonColor.rgb, horizonMask);

                #if defined(_ENABLESUN_ON)
                // Sun
                float3 sunDir = normalize(_SunDirection.xyz);
                float sunDot = dot(viewDir, sunDir);
                float sunMask = saturate((sunDot - (1.0 - _SunSize)) / _SunSize);
                sunMask = pow(sunMask, _SunFalloff);

                // Sun glow (wider, softer)
                float sunGlow = saturate((sunDot - 0.7) / 0.3);
                sunGlow = pow(sunGlow, 4.0) * 0.5;

                skyColor += _SunColor.rgb * (sunMask + sunGlow);
                #endif

                #if defined(_ENABLECLOUDS_ON)
                // Clouds (only above horizon)
                if (verticalFactor > 0 && _CloudDensity > 0)
                {
                    // Project view direction onto cloud plane
                    float cloudY = _CloudHeight + 0.1;
                    float2 cloudUV = viewDir.xz / max(0.01, viewDir.y) * _CloudScale;

                    // Animate clouds
                    cloudUV += _Time.y * _CloudSpeed * float2(1, 0.3);

                    // Generate cloud pattern
                    float cloudNoise = fbm(cloudUV, 4);
                    float cloudMask = saturate((cloudNoise - (1.0 - _CloudDensity)) / _CloudDensity);
                    cloudMask *= saturate(verticalFactor * 5.0); // Fade near horizon
                    cloudMask *= saturate(1.0 - (verticalFactor - _CloudHeight) * 3.0); // Fade at top

                    // Cloud lighting (simple sun-facing bias)
                    #if defined(_ENABLESUN_ON)
                    float cloudLighting = saturate(dot(float3(cloudUV.x, 1, cloudUV.y), sunDir) * 0.5 + 0.5);
                    #else
                    float cloudLighting = 0.7;
                    #endif
                    half3 cloudFinal = lerp(_CloudShadowColor.rgb, _CloudColor.rgb, cloudLighting);

                    skyColor = lerp(skyColor, cloudFinal, cloudMask);
                }
                #endif

                return half4(skyColor, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
