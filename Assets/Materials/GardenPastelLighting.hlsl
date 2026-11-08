#ifndef GARDEN_PASTEL_LIGHTING_INCLUDED
#define GARDEN_PASTEL_LIGHTING_INCLUDED
#include "Assets/Materials/GardenPastelColor.hlsl"
#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
half3 GardenPastelShade(half3 color,half3 normal,half3 direction,half3 lightColor,half attenuation)
{
    normal=normalize(normal);
    half band=GardenCelBand(dot(normal,direction));
    half3 palette=lerp(half3(0.86h,0.86h,1.0h),half3(1.02h,1.0h,0.94h),band);
    half3 lighting=max(SampleSH(normal),0.025h)*0.8h+lightColor*band*attenuation;
    return GardenPastelAlbedo(color)*palette*lighting;
}
#endif
void GardenPastel_float(float3 Color,float3 Normal,out float3 Out)
{
    #ifdef SHADERGRAPH_PREVIEW
        Out=GardenPastelAlbedo(Color)*0.85;
    #else
        Light sun=GetMainLight();
        Out=GardenPastelShade(Color,Normal,sun.direction,sun.color,1);
    #endif
}
void GardenPastel_half(half3 Color,half3 Normal,out half3 Out)
{
    #ifdef SHADERGRAPH_PREVIEW
        Out=GardenPastelAlbedo(Color)*0.85h;
    #else
        Light sun=GetMainLight();
        Out=GardenPastelShade(Color,Normal,sun.direction,sun.color,1);
    #endif
}
#endif
