Shader "Cozy Garden/Short Meadow"
{
    Properties
    {
        _BottomColor("Root green", Color) = (0.32,0.58,0.13,1)
        _TopColor("Tip green", Color) = (0.65,0.85,0.32,1)
        _WindStrength("Wind in metres", Range(0,0.04)) = 0.018
        [HideInInspector] _GroundCentre("Ground centre and flat radius", Vector) = (0,0,0,22)
        [HideInInspector] _GroundShape("Outer radius, drop, density, patch density", Vector) = (42,20,128,128)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Materials/GardenPastelLighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BottomColor;
                half4 _TopColor;
                float _WindStrength;
                float4 _GroundCentre;
                float4 _GroundShape;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS:POSITION;
                float2 uv:TEXCOORD0;
                float3 rootOS:TEXCOORD1;
                half4 color:COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                half3 color:TEXCOORD0;
                half fog:TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 root=TransformObjectToWorld(input.rootOS);
                float2 relative=root.xz-_GroundCentre.xz;
                float radius=length(relative);
                float width=max(0.01,_GroundShape.x-_GroundCentre.w);
                float t=saturate((radius-_GroundCentre.w)/width);
                float cosine=sqrt(max(0.000001,1-t*t));
                float slope=_GroundShape.y*t/(width*max(0.001,cosine));
                float2 radial=relative/max(radius,0.001);
                float3 normal=normalize(float3(slope*radial.x,1,slope*radial.y));
                root.y=_GroundCentre.y+_GroundShape.y*(cosine-1)+0.002;
                float3 right=normalize(cross(normal,float3(0,0,1)));
                float3 forward=cross(right,normal);
                float3 delta=input.positionOS.xyz-input.rootOS;
                float distanceToView=distance(root.xz,_WorldSpaceCameraPos.xz);
                float densityFade=lerp(1-smoothstep(4,7,distanceToView),1,input.color.a);
                float3 world=root+right*delta.x+normal*delta.y*densityFade+forward*delta.z-normal*0.02*(1-densityFade);
                float phase=dot(root.xz,float2(0.8,0.6))*1.7-_Time.y*0.9;
                float wind=sin(phase)*0.7+sin(phase*1.57+0.6)*0.3;
                world+=float3(0.8,0,0.6)*(_WindStrength*input.uv.y*input.uv.y*wind*densityFade);
                float targetDensity=_GroundShape.z*(radius<6?2.25:radius<14?1:radius<26?0.55:0.22);
                float random=frac(sin(dot(root.xz,float2(12.9898,78.233)))*43758.5453);
                bool visible=radius<_GroundShape.x-0.4 && random<saturate(targetDensity/_GroundShape.w);
                if(relative.y>=3 && relative.y<=18)
                {
                    float p=saturate((relative.y-3)/15);
                    float u=1-p;
                    float pathX=u*u*u*6+3*u*u*p*8-3*u*p*p*3-2*p*p*p;
                    float dx=6*u*u-66*u*p+3*p*p;
                    float tangentZ=15*rsqrt(dx*dx+225);
                    float pathWidth=lerp(0.95,0.72,smoothstep(0,1,p));
                    visible=visible && abs(relative.x-pathX)*tangentZ>pathWidth*1.15+0.07;
                }
                // All vertices share the same root, so discarded blades become degenerate without alpha overdraw.
                if(!visible) world=root;
                output.positionCS=TransformWorldToHClip(world);
                Light sun=GetMainLight();
                output.color=GardenPastelShade(lerp(_BottomColor.rgb,_TopColor.rgb,input.uv.y)*input.color.rgb,normal,sun.direction,sun.color,1);
                output.fog=ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return half4(MixFog(input.color,input.fog),1);
            }
            ENDHLSL
        }
    }
}
