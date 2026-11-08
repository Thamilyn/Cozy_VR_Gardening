using System.Collections;
using UnityEngine;

/// <summary>Quiet, non-positional music shared by the garden and its menus.</summary>
[DisallowMultipleComponent]
public sealed class GardenBackgroundMusic : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float volume = 0.01f;
    [SerializeField, Min(0f)] private float fadeInSeconds = 2f;

    private IEnumerator Start()
    {
        AudioClip clip = Resources.Load<AudioClip>("Music/Morning Garden");
        if (clip == null)
        {
            Debug.LogWarning("Garden background music is missing: Morning Garden.", this);
            yield break;
        }

        GardenPreferences.Load();
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.clip = clip;
        source.loop = true;
        source.spatialBlend = 0f;
        source.spatialize = false;
        source.dopplerLevel = 0f;
        source.priority = 192;
        source.volume = 0f;
        // The existing AudioListener preferences apply master volume and mute once.
        source.ignoreListenerVolume = false;
        source.Play();

        float elapsed = 0f;
        while (elapsed < fadeInSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = volume * Mathf.Clamp01(elapsed / fadeInSeconds);
            yield return null;
        }
        source.volume = volume;
    }
}
