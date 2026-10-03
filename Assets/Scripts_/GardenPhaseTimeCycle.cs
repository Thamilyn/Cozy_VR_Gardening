using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Plays a short day/night passage using BOXOPHOBIC's Cubemap Blend skybox.</summary>
[DisallowMultipleComponent]
public sealed class GardenPhaseTimeCycle : MonoBehaviour
{
    [Serializable]
    private sealed class SkyMoment
    {
        [Range(0f, 1f)] public float nightBlend;
        public Color tint = new(0.5f, 0.5f, 0.5f, 1f);
        [Range(0f, 8f)] public float exposure = 1f;
        public float sunElevation = 50f;
        [Min(0f)] public float sunIntensity = 0.5f;
        public Color sunColor = new(1f, 0.957f, 0.839f, 1f);
        public Color ambientColor = new(0.4f, 0.45f, 0.52f, 1f);
    }

    [SerializeField] private Material skybox;
    [SerializeField] private Light sun;
    [Header("Timing in seconds")]
    [SerializeField, Min(0f)] private float dayHoldSeconds = 0.5f;
    [SerializeField, Min(0.1f)] private float transitionSeconds = 1.5f;
    [SerializeField, Min(0f)] private float afternoonHoldSeconds = 0.5f;
    [SerializeField, Min(0f)] private float nightHoldSeconds = 0.8f;
    [Header("Editable sky and lighting")]
    [SerializeField] private SkyMoment day = new();
    [SerializeField] private SkyMoment afternoon = new()
    {
        nightBlend = 0.35f, tint = new Color(0.65f, 0.38f, 0.24f, 1f),
        sunElevation = 8f, sunIntensity = 0.3f, sunColor = new Color(1f, 0.55f, 0.28f, 1f),
        ambientColor = new Color(0.3f, 0.2f, 0.17f, 1f)
    };
    [SerializeField] private SkyMoment night = new()
    {
        nightBlend = 1f, tint = new Color(0.38f, 0.43f, 0.65f, 1f),
        sunElevation = -25f, sunIntensity = 0.06f, sunColor = new Color(0.45f, 0.6f, 1f, 1f),
        ambientColor = new Color(0.08f, 0.11f, 0.18f, 1f)
    };

    private static readonly int Blend = Shader.PropertyToID("_CubemapTransition");
    private static readonly int Tint = Shader.PropertyToID("_TintColor");
    private static readonly int Exposure = Shader.PropertyToID("_Exposure");
    private Material runtimeSkybox;
    private Material originalSkybox;
    private Light originalSun;
    private Quaternion originalSunRotation;
    private float originalSunIntensity;
    private Color originalSunColor;
    private AmbientMode originalAmbientMode;
    private Color originalAmbientSky;
    private Color originalAmbientEquator;
    private Color originalAmbientGround;

    public float DurationSeconds => Mathf.Max(0f, dayHoldSeconds) + Mathf.Max(0f, afternoonHoldSeconds) +
        Mathf.Max(0f, nightHoldSeconds) + 3f * Mathf.Max(0.1f, transitionSeconds);

    private void Start()
    {
        if (!TryPrepare(out string reason)) Debug.LogError(reason, this);
    }

    public bool TryPrepare(out string reason)
    {
        reason = null;
        if (!isActiveAndEnabled || skybox == null || sun == null || sun.type != LightType.Directional ||
            day == null || afternoon == null || night == null ||
            !skybox.HasProperty(Blend) || !skybox.HasProperty(Tint) || !skybox.HasProperty(Exposure) ||
            skybox.GetTexture("_Tex") == null || skybox.GetTexture("_Tex_Blend") == null)
        {
            reason = "Check the Cubemap Blend material and the time-cycle light.";
            return false;
        }
        if (!skybox.shader.isSupported)
        {
            reason = "The sky shader is not available on this platform.";
            return false;
        }
        if (runtimeSkybox != null) return true;

        originalSkybox = RenderSettings.skybox;
        originalSun = RenderSettings.sun;
        originalSunRotation = sun.transform.rotation;
        originalSunIntensity = sun.intensity;
        originalSunColor = sun.color;
        originalAmbientMode = RenderSettings.ambientMode;
        originalAmbientSky = RenderSettings.ambientSkyColor;
        originalAmbientEquator = RenderSettings.ambientEquatorColor;
        originalAmbientGround = RenderSettings.ambientGroundColor;
        // Animate a private copy so Play never changes the saved material asset.
        runtimeSkybox = new Material(skybox) { name = "Garden phase sky (runtime)", hideFlags = HideFlags.DontSave };
        RenderSettings.skybox = runtimeSkybox;
        RenderSettings.sun = sun;
        ResetToDay();
        return true;
    }

    public IEnumerator PlayCycle()
    {
        ResetToDay();
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, dayHoldSeconds));
        yield return Fade(day, afternoon);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, afternoonHoldSeconds));
        yield return Fade(afternoon, night);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, nightHoldSeconds));
        yield return Fade(night, day);
        ResetToDay();
    }

    private IEnumerator Fade(SkyMoment from, SkyMoment to)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, transitionSeconds);
        while (elapsed < duration && runtimeSkybox != null && sun != null && isActiveAndEnabled)
        {
            // Do not skip the whole transition after a headset/application pause.
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            Apply(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        if (runtimeSkybox != null && sun != null && isActiveAndEnabled) Apply(to, to, 1f);
    }

    public void ResetToDay()
    {
        if (runtimeSkybox != null && sun != null) Apply(day, day, 1f);
    }

    private void Apply(SkyMoment from, SkyMoment to, float t)
    {
        runtimeSkybox.SetFloat(Blend, Mathf.Lerp(from.nightBlend, to.nightBlend, t));
        runtimeSkybox.SetColor(Tint, Color.Lerp(from.tint, to.tint, t));
        runtimeSkybox.SetFloat(Exposure, Mathf.Lerp(from.exposure, to.exposure, t));
        sun.transform.rotation = originalSunRotation * Quaternion.Euler(
            Mathf.Lerp(from.sunElevation, to.sunElevation, t) - day.sunElevation, 0f, 0f);
        sun.intensity = Mathf.Lerp(from.sunIntensity, to.sunIntensity, t);
        sun.color = Color.Lerp(from.sunColor, to.sunColor, t);
        // Explicit ambient colors avoid rebuilding sky lighting every frame on Quest.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        Color ambient = Color.Lerp(from.ambientColor, to.ambientColor, t);
        RenderSettings.ambientSkyColor = ambient;
        RenderSettings.ambientEquatorColor = ambient * 0.65f;
        RenderSettings.ambientGroundColor = ambient * 0.4f;
    }

    private void OnDisable()
    {
        if (runtimeSkybox == null) return;
        if (RenderSettings.skybox == runtimeSkybox) RenderSettings.skybox = originalSkybox;
        RenderSettings.sun = originalSun;
        RenderSettings.ambientMode = originalAmbientMode;
        RenderSettings.ambientSkyColor = originalAmbientSky;
        RenderSettings.ambientEquatorColor = originalAmbientEquator;
        RenderSettings.ambientGroundColor = originalAmbientGround;
        if (sun != null)
        {
            sun.transform.rotation = originalSunRotation;
            sun.intensity = originalSunIntensity;
            sun.color = originalSunColor;
        }
        Destroy(runtimeSkybox);
        runtimeSkybox = null;
    }
}
