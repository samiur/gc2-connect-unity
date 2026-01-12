// ABOUTME: Golf course terrain shader with texture splatting for fairway, rough, green, and sand.
// ABOUTME: Uses vertex colors for blending and supports normal maps for surface detail.

Shader "OpenRange/GolfTerrain"
{
    Properties
    {
        [Header(Fairway)]
        _FairwayTex ("Fairway Texture", 2D) = "white" {}
        _FairwayNormal ("Fairway Normal", 2D) = "bump" {}
        _FairwayScale ("Fairway Tiling", Float) = 10
        _FairwayColor ("Fairway Tint", Color) = (1, 1, 1, 1)

        [Header(Rough)]
        _RoughTex ("Rough Texture", 2D) = "white" {}
        _RoughNormal ("Rough Normal", 2D) = "bump" {}
        _RoughScale ("Rough Tiling", Float) = 8
        _RoughColor ("Rough Tint", Color) = (1, 1, 1, 1)

        [Header(Green)]
        _GreenTex ("Green Texture", 2D) = "white" {}
        _GreenNormal ("Green Normal", 2D) = "bump" {}
        _GreenScale ("Green Tiling", Float) = 15
        _GreenColor ("Green Tint", Color) = (1, 1, 1, 1)

        [Header(Sand)]
        _SandTex ("Sand Texture", 2D) = "white" {}
        _SandNormal ("Sand Normal", 2D) = "bump" {}
        _SandScale ("Sand Tiling", Float) = 5
        _SandColor ("Sand Tint", Color) = (1, 1, 1, 1)

        [Header(Blending)]
        _BlendSharpness ("Blend Sharpness", Range(0.1, 10)) = 2
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1

        [Header(Distance Fade)]
        _DetailFadeStart ("Detail Fade Start", Float) = 50
        _DetailFadeEnd ("Detail Fade End", Float) = 100
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // Vertex colors for splatting: R=Rough, G=Green, B=Sand, A=unused
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float4 vertexColor : TEXCOORD5;
                float fogFactor : TEXCOORD6;
                float distanceToCamera : TEXCOORD7;
            };

            // Textures
            TEXTURE2D(_FairwayTex); SAMPLER(sampler_FairwayTex);
            TEXTURE2D(_FairwayNormal); SAMPLER(sampler_FairwayNormal);
            TEXTURE2D(_RoughTex); SAMPLER(sampler_RoughTex);
            TEXTURE2D(_RoughNormal); SAMPLER(sampler_RoughNormal);
            TEXTURE2D(_GreenTex); SAMPLER(sampler_GreenTex);
            TEXTURE2D(_GreenNormal); SAMPLER(sampler_GreenNormal);
            TEXTURE2D(_SandTex); SAMPLER(sampler_SandTex);
            TEXTURE2D(_SandNormal); SAMPLER(sampler_SandNormal);

            CBUFFER_START(UnityPerMaterial)
                float _FairwayScale;
                float4 _FairwayColor;
                float _RoughScale;
                float4 _RoughColor;
                float _GreenScale;
                float4 _GreenColor;
                float _SandScale;
                float4 _SandColor;
                float _BlendSharpness;
                float _NormalStrength;
                float _DetailFadeStart;
                float _DetailFadeEnd;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = normalInputs.tangentWS;
                output.bitangentWS = normalInputs.bitangentWS;
                output.uv = input.uv;
                output.vertexColor = input.color;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                output.distanceToCamera = length(_WorldSpaceCameraPos - posInputs.positionWS);

                return output;
            }

            // Sample texture with world-space tiling
            float4 SampleTerrainTexture(TEXTURE2D_PARAM(tex, samp), float3 worldPos, float scale)
            {
                float2 uv = worldPos.xz * scale * 0.01; // Scale to reasonable tiling
                return SAMPLE_TEXTURE2D(tex, samp, uv);
            }

            // Unpack and blend normal maps
            float3 UnpackAndBlendNormal(float4 normalSample, float strength)
            {
                float3 normal = UnpackNormal(normalSample);
                normal.xy *= strength;
                return normalize(normal);
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Calculate blend weights from vertex colors
                // R = Rough, G = Green, B = Sand
                // Fairway = 1 - (R + G + B)
                float roughWeight = input.vertexColor.r;
                float greenWeight = input.vertexColor.g;
                float sandWeight = input.vertexColor.b;
                float fairwayWeight = saturate(1.0 - (roughWeight + greenWeight + sandWeight));

                // Apply blend sharpness
                float totalWeight = fairwayWeight + roughWeight + greenWeight + sandWeight;
                fairwayWeight = pow(fairwayWeight / totalWeight, _BlendSharpness);
                roughWeight = pow(roughWeight / totalWeight, _BlendSharpness);
                greenWeight = pow(greenWeight / totalWeight, _BlendSharpness);
                sandWeight = pow(sandWeight / totalWeight, _BlendSharpness);

                // Renormalize
                totalWeight = fairwayWeight + roughWeight + greenWeight + sandWeight;
                fairwayWeight /= totalWeight;
                roughWeight /= totalWeight;
                greenWeight /= totalWeight;
                sandWeight /= totalWeight;

                // Sample textures
                float4 fairwayColor = SampleTerrainTexture(TEXTURE2D_ARGS(_FairwayTex, sampler_FairwayTex), input.positionWS, _FairwayScale) * _FairwayColor;
                float4 roughColor = SampleTerrainTexture(TEXTURE2D_ARGS(_RoughTex, sampler_RoughTex), input.positionWS, _RoughScale) * _RoughColor;
                float4 greenColor = SampleTerrainTexture(TEXTURE2D_ARGS(_GreenTex, sampler_GreenTex), input.positionWS, _GreenScale) * _GreenColor;
                float4 sandColor = SampleTerrainTexture(TEXTURE2D_ARGS(_SandTex, sampler_SandTex), input.positionWS, _SandScale) * _SandColor;

                // Blend colors
                float4 albedo = fairwayColor * fairwayWeight +
                               roughColor * roughWeight +
                               greenColor * greenWeight +
                               sandColor * sandWeight;

                // Sample and blend normal maps
                float4 fairwayNormalSample = SampleTerrainTexture(TEXTURE2D_ARGS(_FairwayNormal, sampler_FairwayNormal), input.positionWS, _FairwayScale);
                float4 roughNormalSample = SampleTerrainTexture(TEXTURE2D_ARGS(_RoughNormal, sampler_RoughNormal), input.positionWS, _RoughScale);
                float4 greenNormalSample = SampleTerrainTexture(TEXTURE2D_ARGS(_GreenNormal, sampler_GreenNormal), input.positionWS, _GreenScale);
                float4 sandNormalSample = SampleTerrainTexture(TEXTURE2D_ARGS(_SandNormal, sampler_SandNormal), input.positionWS, _SandScale);

                float3 blendedTangentNormal = UnpackAndBlendNormal(fairwayNormalSample, _NormalStrength) * fairwayWeight +
                                              UnpackAndBlendNormal(roughNormalSample, _NormalStrength) * roughWeight +
                                              UnpackAndBlendNormal(greenNormalSample, _NormalStrength) * greenWeight +
                                              UnpackAndBlendNormal(sandNormalSample, _NormalStrength) * sandWeight;
                blendedTangentNormal = normalize(blendedTangentNormal);

                // Distance fade for detail (reduce normal strength at distance)
                float distanceFade = 1.0 - saturate((input.distanceToCamera - _DetailFadeStart) / (_DetailFadeEnd - _DetailFadeStart));
                blendedTangentNormal = lerp(float3(0, 0, 1), blendedTangentNormal, distanceFade);

                // Transform normal to world space
                float3x3 TBN = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                float3 normalWS = normalize(mul(blendedTangentNormal, TBN));

                // Lighting
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalWS;
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lightingInput.fogCoord = input.fogFactor;
                lightingInput.bakedGI = SampleSH(normalWS);
                lightingInput.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.alpha = 1.0;
                surfaceData.metallic = 0.0;
                surfaceData.smoothness = 0.1; // Grass is not smooth
                surfaceData.normalTS = blendedTangentNormal;
                surfaceData.occlusion = 1.0;

                half4 color = UniversalFragmentPBR(lightingInput, surfaceData);
                color.rgb = MixFog(color.rgb, input.fogFactor);

                return color;
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

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
