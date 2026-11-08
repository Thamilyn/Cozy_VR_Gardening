Shader "Cozy Garden/Styled Props"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        [Normal] _BumpMap("Normal", 2D) = "bump" {}
        _BumpScale("Normal strength", Range(0,1)) = 0.4
        _Cutoff("Alpha cutoff", Range(0,1)) = 0.5
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Cutout", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Faces", Float) = 2
        _EmissionMap("Emission", 2D) = "white" {}
        [HDR] _EmissionColor("Emission tint", Color) = (0,0,0,0)
        [HideInInspector] _Color("Legacy tint", Color) = (1,1,1,1)
        [Toggle(_GARDEN_WIND)] _WindEnabled("Decorative grass wind", Float) = 0
        _WindStrength("Wind displacement in metres", Range(0,0.05)) = 0.02
        [HideInInspector] _WindBase("Wind root height", Float) = 0
        [HideInInspector] _WindHeight("Wind mesh height", Float) = 1
        _StoneMossColor("Soft stone moss", Color) = (0.36,0.46,0.29,1)
        _StoneMossStrength("Stone moss amount", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Assets/Materials/GardenPastelLighting.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _Color;
            half4 _EmissionColor;
            half _BumpScale;
            half _Cutoff;
            half _AlphaClip;
            half _Cull;
            float _WindEnabled;
            float _WindStrength;
            float _WindBase;
            float _WindHeight;
            half4 _StoneMossColor;
            half _StoneMossStrength;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        void ClipAlpha(float2 uv)
        {
            #if defined(_ALPHATEST_ON)
                clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).a * _BaseColor.a - _Cutoff);
            #endif
        }
        float3 ApplyMeadowWind(float3 positionOS)
        {
            #if defined(_GARDEN_WIND)
                float anchor=saturate((positionOS.y-_WindBase)/max(_WindHeight,0.001));
                float3 world=TransformObjectToWorld(positionOS);
                float phase=dot(world.xz,float2(0.8,0.6))*1.7-_Time.y*0.9;
                float sway=sin(phase)*0.7+sin(phase*1.57+0.6)*0.3;
                world+=float3(0.8,0,0.6)*(_WindStrength*anchor*anchor*sway);
                return TransformWorldToObject(world);
            #else
                return positionOS;
            #endif
        }
        ENDHLSL
        Pass
        {
            Name "StyledForward"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local_vertex _GARDEN_WIND
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half fog : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input,output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position=GetVertexPositionInputs(ApplyMeadowWind(input.positionOS.xyz));
                output.positionCS=position.positionCS;
                output.positionWS=position.positionWS;
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS=half4(TransformObjectToWorldDir(input.tangentOS.xyz),input.tangentOS.w*GetOddNegativeScale());
                output.uv=TRANSFORM_TEX(input.uv,_BaseMap);
                output.fog=ComputeFogFactor(position.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv)*_BaseColor;
                #if defined(_ALPHATEST_ON)
                    clip(albedo.a-_Cutoff);
                #endif
                half3 normal=normalize(input.normalWS);
                #if defined(_NORMALMAP)
                    half3 tangentNormal=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,input.uv),_BumpScale);
                    half3 tangent=normalize(input.tangentWS.xyz);
                    half3 bitangent=cross(normal,tangent)*input.tangentWS.w;
                    normal=normalize(TransformTangentToWorld(tangentNormal,half3x3(tangent,bitangent,normal)));
                #endif
                if (_StoneMossStrength > 0)
                    albedo.rgb=lerp(albedo.rgb,_StoneMossColor.rgb,smoothstep(0.35h,0.8h,normal.y)*_StoneMossStrength);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord=float4(GetNormalizedScreenSpaceUV(input.positionCS),0,1);
                #else
                    float4 shadowCoord=TransformWorldToShadowCoord(input.positionWS);
                #endif
                Light sun=GetMainLight(shadowCoord);
                half3 color=GardenPastelShade(albedo.rgb,normal,sun.direction,sun.color,sun.shadowAttenuation);
                #if defined(_EMISSION)
                    color+=SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,input.uv).rgb*_EmissionColor.rgb;
                #endif
                return half4(MixFog(color,input.fog),albedo.a);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_vertex _GARDEN_WIND
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection;
            float3 _LightPosition;
            struct ShadowVaryings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            ShadowVaryings ShadowVert(Attributes input)
            {
                ShadowVaryings output=(ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 position=TransformObjectToWorld(ApplyMeadowWind(input.positionOS.xyz));
                float3 normal=TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 direction=normalize(_LightPosition-position);
                #else
                    float3 direction=_LightDirection;
                #endif
                output.positionCS=TransformWorldToHClip(ApplyShadowBias(position,normal,direction));
                #if UNITY_REVERSED_Z
                    output.positionCS.z=min(output.positionCS.z,UNITY_NEAR_CLIP_VALUE*output.positionCS.w);
                #else
                    output.positionCS.z=max(output.positionCS.z,UNITY_NEAR_CLIP_VALUE*output.positionCS.w);
                #endif
                output.uv=TRANSFORM_TEX(input.uv,_BaseMap);
                return output;
            }
            half4 ShadowFrag(ShadowVaryings input):SV_Target { ClipAlpha(input.uv); return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_vertex _GARDEN_WIND
            #pragma multi_compile_instancing
            struct DepthVaryings { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            DepthVaryings DepthVert(Attributes input)
            {
                DepthVaryings output=(DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformObjectToHClip(ApplyMeadowWind(input.positionOS.xyz));
                output.uv=TRANSFORM_TEX(input.uv,_BaseMap);
                return output;
            }
            half4 DepthFrag(DepthVaryings input):SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); ClipAlpha(input.uv); return 0; }
            ENDHLSL
        }
    }
}
