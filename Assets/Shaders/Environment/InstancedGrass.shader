// ABOUTME: GPU instanced grass blade shader for rendering thousands of grass blades.
// ABOUTME: Uses StructuredBuffer for per-instance data and wind animation via vertex displacement.

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

        [Header(Appearance)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.1
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
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
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TipColor;
                float _TipBlendStart;
                float _TipBlendEnd;
                float _WindStrength;
                float _WindSpeed;
                float _WindTurbulence;
                float _Smoothness;
                float _Cutoff;
            CBUFFER_END

            // Global wind parameters (set by WindController)
            float4 _GlobalWindDirection;
            float _GlobalWindStrength;
            float _GlobalWindSpeed;
            float _GlobalWindTime;

            // Noise function for wind variation
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
                // scaleData.w = colorVariation (unused for now)

                // Get vertex height factor (0 at base, 1 at tip)
                float heightFactor = saturate(input.positionOS.y);
                output.heightFactor = heightFactor;

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

                // Wind animation (only affects upper portion of blade)
                float windTime = _GlobalWindTime * _GlobalWindSpeed * _WindSpeed;
                float2 windUV = instancePos.xz * 0.1 + windTime * 0.5;

                // Create wind displacement
                float windNoise = noise(windUV) * 2.0 - 1.0;
                float turbulence = noise(windUV * 2.0 + 100.0) * _WindTurbulence;

                float3 windDir = _GlobalWindDirection.xyz;
                if (length(windDir) < 0.01) windDir = float3(1, 0, 0); // Default wind direction

                // Wind effect increases with height
                float windEffect = heightFactor * heightFactor * _GlobalWindStrength * _WindStrength;
                float3 windDisplacement = windDir * (windNoise + turbulence) * windEffect * 0.3;

                // Add slight sway perpendicular to wind
                float swayNoise = noise(windUV * 1.5 + 50.0);
                float3 perpDir = cross(windDir, float3(0, 1, 0));
                windDisplacement += perpDir * swayNoise * windEffect * 0.1;

                // Apply displacement
                rotatedPos += windDisplacement;

                // Transform to world space
                float3 worldPos = rotatedPos + instancePos;

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);

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
                // Blend between base and tip color based on height
                float blendFactor = smoothstep(_TipBlendStart, _TipBlendEnd, input.heightFactor);
                float3 albedo = lerp(_BaseColor.rgb, _TipColor.rgb, blendFactor);

                // Simple lighting
                float3 normalWS = normalize(input.normalWS);

                // Flip normal for back faces to get correct lighting
                normalWS = input.positionCS.w > 0 ? normalWS : -normalWS;

                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 diffuse = albedo * mainLight.color * NdotL;

                // Ambient
                float3 ambient = albedo * SampleSH(normalWS);

                float3 finalColor = diffuse + ambient;

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
