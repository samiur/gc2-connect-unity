// ABOUTME: Stylized water shader for URP with scrolling normals, depth-based foam, and reflections.
// ABOUTME: Supports quality tier adjustments via WaterController and planar reflection on High quality.

Shader "OpenRange/StylizedWater"
{
    Properties
    {
        [Header(Colors)]
        _ShallowColor ("Shallow Color", Color) = (0.2, 0.6, 0.8, 0.8)
        _DeepColor ("Deep Color", Color) = (0.05, 0.2, 0.4, 1.0)
        _DepthFadeDistance ("Depth Fade Distance", Range(0.1, 20)) = 5.0

        [Header(Normal Maps)]
        _NormalMap1 ("Normal Map 1", 2D) = "bump" {}
        _NormalMap2 ("Normal Map 2", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Range(0, 2)) = 1.0
        _NormalSpeed1 ("Normal Speed 1", Vector) = (0.1, 0.1, 0, 0)
        _NormalSpeed2 ("Normal Speed 2", Vector) = (-0.08, 0.05, 0, 0)
        _NormalTiling ("Normal Tiling", Range(0.1, 10)) = 1.0

        [Header(Waves)]
        _WaveAmplitude ("Wave Amplitude", Range(0, 1)) = 0.1
        _WaveFrequency ("Wave Frequency", Range(0.1, 10)) = 1.0
        _WaveSpeed ("Wave Speed", Range(0.1, 5)) = 1.0

        [Header(Foam)]
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamThreshold ("Foam Threshold", Range(0.01, 2)) = 0.5
        _FoamSoftness ("Foam Softness", Range(0.01, 1)) = 0.3
        _FoamIntensity ("Foam Intensity", Range(0, 2)) = 1.0
        _FoamNoiseScale ("Foam Noise Scale", Range(0.1, 20)) = 5.0

        [Header(Reflection)]
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.5
        _ReflectionTex ("Reflection Texture", 2D) = "white" {}
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 5.0

        [Header(Specular)]
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularPower ("Specular Power", Range(1, 256)) = 64
        _SpecularIntensity ("Specular Intensity", Range(0, 2)) = 0.5

        [Header(Quality)]
        [Toggle] _EnableWaves ("Enable Waves", Float) = 1
        [Toggle] _EnableFoam ("Enable Foam", Float) = 1
        [Toggle] _EnableReflection ("Enable Planar Reflection", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ _ENABLEWAVES_ON
            #pragma multi_compile_local _ _ENABLEFOAM_ON
            #pragma multi_compile_local _ _ENABLEREFLECTION_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 tangentWS : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float3 positionWS : TEXCOORD4;
                float4 screenPos : TEXCOORD5;
                float fogFactor : TEXCOORD6;
                float3 viewDirWS : TEXCOORD7;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_NormalMap1);
            SAMPLER(sampler_NormalMap1);
            TEXTURE2D(_NormalMap2);
            SAMPLER(sampler_NormalMap2);
            TEXTURE2D(_ReflectionTex);
            SAMPLER(sampler_ReflectionTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half _DepthFadeDistance;

                float4 _NormalMap1_ST;
                float4 _NormalMap2_ST;
                half _NormalScale;
                float4 _NormalSpeed1;
                float4 _NormalSpeed2;
                half _NormalTiling;

                half _WaveAmplitude;
                half _WaveFrequency;
                half _WaveSpeed;

                half4 _FoamColor;
                half _FoamThreshold;
                half _FoamSoftness;
                half _FoamIntensity;
                half _FoamNoiseScale;

                half _ReflectionStrength;
                half _FresnelPower;

                half4 _SpecularColor;
                half _SpecularPower;
                half _SpecularIntensity;

                half _EnableWaves;
                half _EnableFoam;
                half _EnableReflection;
            CBUFFER_END

            // Simple noise for foam variation
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

            // Gerstner wave calculation
            float3 GerstnerWave(float2 position, float time, float amplitude, float frequency, float2 direction)
            {
                float k = 2.0 * PI * frequency;
                float speed = sqrt(9.81 / k);
                float phase = k * (dot(direction, position) - speed * time);

                float3 wave;
                wave.x = amplitude * direction.x * cos(phase);
                wave.z = amplitude * direction.y * cos(phase);
                wave.y = amplitude * sin(phase);

                return wave;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = input.positionOS.xyz;
                float3 positionWS = TransformObjectToWorld(positionOS);

                #if defined(_ENABLEWAVES_ON)
                // Apply Gerstner waves
                float time = _Time.y * _WaveSpeed;
                float3 wave1 = GerstnerWave(positionWS.xz, time, _WaveAmplitude * 0.5, _WaveFrequency, normalize(float2(1, 0.5)));
                float3 wave2 = GerstnerWave(positionWS.xz, time, _WaveAmplitude * 0.3, _WaveFrequency * 1.5, normalize(float2(-0.5, 1)));
                float3 wave3 = GerstnerWave(positionWS.xz, time, _WaveAmplitude * 0.2, _WaveFrequency * 2.0, normalize(float2(0.3, -0.8)));

                positionWS += wave1 + wave2 + wave3;
                #endif

                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.uv = input.uv;

                // Transform normal/tangent/bitangent to world space
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = normalInputs.tangentWS;
                output.bitangentWS = normalInputs.bitangentWS;

                // Screen position for depth sampling
                output.screenPos = ComputeScreenPos(output.positionCS);

                // Fog
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                // View direction for reflections/specular
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample and blend normal maps
                float2 uv1 = input.uv * _NormalTiling + _Time.y * _NormalSpeed1.xy;
                float2 uv2 = input.uv * _NormalTiling * 1.3 + _Time.y * _NormalSpeed2.xy;

                half3 normal1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap1, sampler_NormalMap1, uv1), _NormalScale);
                half3 normal2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap2, sampler_NormalMap2, uv2), _NormalScale);

                // Blend normals (Reoriented Normal Mapping)
                half3 blendedNormal = normalize(half3(normal1.xy + normal2.xy, normal1.z * normal2.z));

                // Transform normal from tangent to world space
                float3x3 tangentToWorld = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                float3 normalWS = normalize(mul(blendedNormal, tangentToWorld));

                // Calculate water depth
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float waterDepth = input.screenPos.w;
                float depthDifference = sceneDepth - waterDepth;

                // Depth-based color blending
                float depthFactor = saturate(depthDifference / _DepthFadeDistance);
                half4 waterColor = lerp(_ShallowColor, _DeepColor, depthFactor);

                // View direction
                float3 viewDirWS = normalize(input.viewDirWS);

                // Fresnel for reflection blend
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);

                // Reflection
                half3 reflectionColor = half3(0.5, 0.6, 0.7); // Default sky color fallback
                #if defined(_ENABLEREFLECTION_ON)
                // Sample planar reflection texture
                float2 reflectionUV = screenUV + normalWS.xz * 0.05;
                reflectionColor = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, reflectionUV).rgb;
                #else
                // Simple cubemap fallback or environment sample
                float3 reflectionDir = reflect(-viewDirWS, normalWS);
                reflectionColor = lerp(half3(0.4, 0.5, 0.7), half3(0.7, 0.8, 0.9), saturate(reflectionDir.y * 0.5 + 0.5));
                #endif

                waterColor.rgb = lerp(waterColor.rgb, reflectionColor, fresnel * _ReflectionStrength);

                // Foam at water-object intersections
                #if defined(_ENABLEFOAM_ON)
                float foamNoise = noise(input.positionWS.xz * _FoamNoiseScale + _Time.y * 0.5) * 0.3;
                float foamMask = saturate((1.0 - depthDifference / _FoamThreshold) + foamNoise);
                foamMask = smoothstep(1.0 - _FoamSoftness, 1.0, foamMask);
                waterColor.rgb = lerp(waterColor.rgb, _FoamColor.rgb, foamMask * _FoamIntensity);
                #endif

                // Lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));

                // Simple diffuse contribution
                half3 diffuse = waterColor.rgb * mainLight.color * (NdotL * 0.5 + 0.5);

                // Specular highlight
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                half3 specular = _SpecularColor.rgb * pow(NdotH, _SpecularPower) * _SpecularIntensity * mainLight.color;

                // Final color
                half3 finalColor = diffuse + specular;

                // Apply fog
                finalColor = MixFog(finalColor, input.fogFactor);

                // Alpha based on depth (more transparent in shallow areas)
                float alpha = lerp(_ShallowColor.a, _DeepColor.a, depthFactor);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }

        // Shadow caster pass - water typically doesn't cast shadows, but including for completeness
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
            #pragma multi_compile_local _ _ENABLEWAVES_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half _DepthFadeDistance;
                float4 _NormalMap1_ST;
                float4 _NormalMap2_ST;
                half _NormalScale;
                float4 _NormalSpeed1;
                float4 _NormalSpeed2;
                half _NormalTiling;
                half _WaveAmplitude;
                half _WaveFrequency;
                half _WaveSpeed;
                half4 _FoamColor;
                half _FoamThreshold;
                half _FoamSoftness;
                half _FoamIntensity;
                half _FoamNoiseScale;
                half _ReflectionStrength;
                half _FresnelPower;
                half4 _SpecularColor;
                half _SpecularPower;
                half _SpecularIntensity;
                half _EnableWaves;
                half _EnableFoam;
                half _EnableReflection;
            CBUFFER_END

            float3 _LightDirection;

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
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

        // Depth pass
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
            #pragma multi_compile_local _ _ENABLEWAVES_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half _DepthFadeDistance;
                float4 _NormalMap1_ST;
                float4 _NormalMap2_ST;
                half _NormalScale;
                float4 _NormalSpeed1;
                float4 _NormalSpeed2;
                half _NormalTiling;
                half _WaveAmplitude;
                half _WaveFrequency;
                half _WaveSpeed;
                half4 _FoamColor;
                half _FoamThreshold;
                half _FoamSoftness;
                half _FoamIntensity;
                half _FoamNoiseScale;
                half _ReflectionStrength;
                half _FresnelPower;
                half4 _SpecularColor;
                half _SpecularPower;
                half _SpecularIntensity;
                half _EnableWaves;
                half _EnableFoam;
                half _EnableReflection;
            CBUFFER_END

            float3 GerstnerWaveDepth(float2 position, float time, float amplitude, float frequency, float2 direction)
            {
                float k = 2.0 * 3.14159265 * frequency;
                float speed = sqrt(9.81 / k);
                float phase = k * (dot(direction, position) - speed * time);
                return float3(amplitude * direction.x * cos(phase), amplitude * sin(phase), amplitude * direction.y * cos(phase));
            }

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                #if defined(_ENABLEWAVES_ON)
                float time = _Time.y * _WaveSpeed;
                float3 wave1 = GerstnerWaveDepth(positionWS.xz, time, _WaveAmplitude * 0.5, _WaveFrequency, normalize(float2(1, 0.5)));
                float3 wave2 = GerstnerWaveDepth(positionWS.xz, time, _WaveAmplitude * 0.3, _WaveFrequency * 1.5, normalize(float2(-0.5, 1)));
                float3 wave3 = GerstnerWaveDepth(positionWS.xz, time, _WaveAmplitude * 0.2, _WaveFrequency * 2.0, normalize(float2(0.3, -0.8)));
                positionWS += wave1 + wave2 + wave3;
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
