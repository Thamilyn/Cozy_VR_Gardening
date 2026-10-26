using System;
using UnityEngine;

public enum GardenHand { Left, Right }
public enum GardenColorPalette { Standard, Protanopia, Deuteranopia, Tritanopia }

/// <summary>Local user preferences only. Crop and tutorial progress never enter this store.</summary>
public static class GardenPreferences
{
    private const string Key = "CozyGarden.Preferences.v1";

    [Serializable]
    private sealed class Values
    {
        public float masterVolume = 1f;
        public float voiceVolume = 1f;
        public bool muted;
        public float brightness = 1f;
        public GardenHand mainHand = GardenHand.Right;
        public GardenHand movementHand = GardenHand.Left;
        public GardenHand calendarHand = GardenHand.Left;
        public GardenColorPalette palette;
    }

    private static Values values;
    public static event Action Changed;
    public static float MasterVolume => Current.masterVolume;
    public static float VoiceVolume => Current.voiceVolume;
    public static bool Muted => Current.muted;
    public static float Brightness => Current.brightness;
    public static GardenHand MainHand => Current.mainHand;
    public static GardenHand MovementHand => Current.movementHand;
    public static GardenHand CalendarHand => Current.calendarHand;
    public static GardenColorPalette Palette => Current.palette;
    private static Values Current { get { Load(); return values; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { values = null; Changed = null; }

    public static void Load()
    {
        if (values != null) return;
        values = new Values();
        if (PlayerPrefs.HasKey(Key))
        {
            try { JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(Key), values); }
            catch (ArgumentException) { values = new Values(); }
        }
        values.masterVolume = FiniteRange(values.masterVolume, 0f, 1f, 1f);
        values.voiceVolume = FiniteRange(values.voiceVolume, 0f, 1f, 1f);
        values.brightness = FiniteRange(values.brightness, 0.7f, 1.3f, 1f);
        if (!Enum.IsDefined(typeof(GardenHand), values.mainHand)) values.mainHand = GardenHand.Right;
        if (!Enum.IsDefined(typeof(GardenHand), values.movementHand)) values.movementHand = GardenHand.Left;
        if (!Enum.IsDefined(typeof(GardenHand), values.calendarHand)) values.calendarHand = GardenHand.Left;
        if (!Enum.IsDefined(typeof(GardenColorPalette), values.palette)) values.palette = GardenColorPalette.Standard;
        ApplyAudio();
    }

    public static void SetMasterVolume(float volume) { Current.masterVolume = FiniteRange(volume, 0f, 1f, 1f); Save(); }
    public static void SetVoiceVolume(float volume) { Current.voiceVolume = FiniteRange(volume, 0f, 1f, 1f); Save(); }
    public static void SetMuted(bool muted) { Current.muted = muted; Save(); }

    // These preferences are reserved for later integrations. Do not expose selectors until
    // the rig, demonstrations and spoken variants apply the same effective configuration.
    public static void SetBrightness(float brightness) { Current.brightness = FiniteRange(brightness, 0.7f, 1.3f, 1f); Save(); }
    public static void SetMainHand(GardenHand hand) { if (Enum.IsDefined(typeof(GardenHand), hand)) { Current.mainHand = hand; Save(); } }
    public static void SetMovementHand(GardenHand hand) { if (Enum.IsDefined(typeof(GardenHand), hand)) { Current.movementHand = hand; Save(); } }
    public static void SetCalendarHand(GardenHand hand) { if (Enum.IsDefined(typeof(GardenHand), hand)) { Current.calendarHand = hand; Save(); } }
    public static void SetPalette(GardenColorPalette palette) { if (Enum.IsDefined(typeof(GardenColorPalette), palette)) { Current.palette = palette; Save(); } }

    public static void RestoreSoundDefaults()
    {
        Current.masterVolume = Current.voiceVolume = 1f;
        Current.muted = false;
        Save();
    }

    public static void RestoreDefaults() { values = new Values(); Save(); }

    private static float FiniteRange(float value, float min, float max, float fallback) =>
        float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);

    private static void ApplyAudio() => AudioListener.volume = values.muted ? 0f : values.masterVolume;

    private static void Save()
    {
        ApplyAudio();
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(values));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
