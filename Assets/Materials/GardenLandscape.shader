Shader "Cozy Garden/Landscape"
{
    Properties
    {
        _BaseMap("Meadow grass", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Name "Landscape"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Materials/GardenPastelLighting.hlsl"
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 color : TEXCOORD0;
                half fog : TEXCOORD1;
                float2 grassUV : TEXCOORD2;
                half grassWeight : TEXCOORD3;
                half3 meadowColor : TEXCOORD4;
                half3 lighting : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 normal = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.grassUV = TransformObjectToWorld(input.positionOS.xyz).xz * 0.25f;
                output.grassWeight = input.color.a;
                Light sun = GetMainLight();
                half diffuse = GardenCelBand(dot(normal, sun.direction));
                half3 palette = lerp(half3(0.86h, 0.86h, 1.0h), half3(1.02h, 1.0h, 0.94h), diffuse);
                half3 lighting = (max(SampleSH(normal), 0.025h) * 0.8h + sun.color * diffuse) * palette;
                output.color = input.color.rgb;
                output.meadowColor = half3(1.5h, 1.8h, 1.3h);
                output.lighting = lighting;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 grass = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.grassUV).rgb;
                half3 surface = input.color * grass;
                // Blend the lit soil and meadow separately to avoid a bright rim at the shoulders.
                if (input.grassWeight <= 0)
                    surface = lerp(input.color, input.meadowColor * grass, input.grassWeight + 1);
                surface = GardenPastelAlbedo(surface) * input.lighting;
                return half4(MixFog(surface, input.fog), 1);
            }
            ENDHLSL
        }
    }
}
