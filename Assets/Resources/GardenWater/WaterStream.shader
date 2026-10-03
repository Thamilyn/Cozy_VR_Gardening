Shader "Cozy Garden/Water Stream"
{
    Properties
    {
        [MainColor] _BaseColor("Water tint", Color) = (0.8, 0.95, 1, 0.8)
        _HighlightColor("Highlight", Color) = (0.85, 0.97, 1, 1)
        _FlowSpeed("Flow speed", Range(0, 3)) = 0.7
        _FlowFrequency("Highlights per metre", Range(1, 30)) = 12
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }
        Pass
        {
            Name "WaterStream"
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
                float _FlowSpeed;
                float _FlowFrequency;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                half fogFactor : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // Tiled line UVs keep the flow speed stable as the hit distance changes.
                float phase = (input.uv.x - _Time.y * _FlowSpeed) * _FlowFrequency * 6.283185;
                float centre = 0.5 + sin(phase * 0.35) * 0.04;
                half edge = 1.0h - smoothstep(0.3, 0.5, abs(input.uv.y - centre));
                half highlight = smoothstep(0.6, 1.0, sin(phase));
                half core = 1.0h - smoothstep(0.0, 0.18, abs(input.uv.y - centre));
                half3 tint = _BaseColor.rgb * input.color.rgb;
                half3 color = lerp(tint, _HighlightColor.rgb, core * (0.2h + highlight * 0.45h));
                color = MixFog(color, input.fogFactor);
                return half4(color, saturate(_BaseColor.a * input.color.a * edge));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
