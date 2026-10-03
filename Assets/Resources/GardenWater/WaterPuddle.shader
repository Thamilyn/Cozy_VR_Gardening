Shader "Cozy Garden/Water Puddle"
{
    Properties
    {
        [MainColor] _BaseColor("Water tint", Color) = (0.12, 0.45, 0.65, 0.65)
        _HighlightColor("Highlight", Color) = (0.7, 0.9, 1, 1)
        _WaterAmount("Visible water", Range(0, 1)) = 1
        _RippleSpeed("Ripple speed", Range(0, 2)) = 0.25
        _RippleFrequency("Ripple rings", Range(1, 10)) = 4
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }
        Pass
        {
            Name "WaterPuddle"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _HighlightColor;
                float _WaterAmount;
                float _RippleSpeed;
                float _RippleFrequency;
            CBUFFER_END

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
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float radius = length(input.uv * 2.0 - 1.0);
                float size = lerp(0.65, 1.0, saturate(_WaterAmount));
                half edge = 1.0h - smoothstep(size - 0.1, size, radius);
                half ripple = smoothstep(0.75, 1.0,
                    sin((radius * _RippleFrequency - _Time.y * _RippleSpeed) * 6.283185));
                half3 viewDirection = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half fresnel = 1.0h - saturate(abs(dot(normalize(input.normalWS), viewDirection)));
                fresnel *= fresnel;
                half3 color = lerp(_BaseColor.rgb, _HighlightColor.rgb,
                    saturate(fresnel * 0.5h + ripple * 0.15h));
                color = MixFog(color, input.fogFactor);
                half alpha = _BaseColor.a * edge * saturate(_WaterAmount);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
