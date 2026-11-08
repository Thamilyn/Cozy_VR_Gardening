#ifndef GARDEN_PASTEL_COLOR_INCLUDED
#define GARDEN_PASTEL_COLOR_INCLUDED
half3 GardenPastelAlbedo(half3 color)
{
    half value=dot(color,half3(0.2126h,0.7152h,0.0722h));
    color=lerp(color,value.xxx,0.28h);
    // Preserve dark lettering and white labels while softening the middle tones.
    half lift=smoothstep(0.03h,0.22h,value)*(1-smoothstep(0.8h,1.0h,value));
    return lerp(color,half3(0.94h,0.91h,0.85h),0.24h*lift);
}
half GardenCelBand(half normalDotLight)
{
    half value=normalDotLight*0.5h+0.5h;
    return 0.4h+0.3h*smoothstep(0.39h,0.42h,value)+0.3h*smoothstep(0.69h,0.72h,value);
}
half3 GardenPastelSky(half3 color)
{
    half value=dot(color,half3(0.2126h,0.7152h,0.0722h));
    half daylight=smoothstep(0.06h,0.5h,value);
    color=lerp(color,value.xxx,0.18h);
    color=lerp(color,half3(0.90h,0.90h,0.96h),0.18h*daylight);
    half3 bands=floor(color*10+0.5h)/10;
    return lerp(color,bands,0.35h*daylight);
}
#endif
