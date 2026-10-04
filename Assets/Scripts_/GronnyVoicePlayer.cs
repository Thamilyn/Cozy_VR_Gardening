using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One voice, with contextual cancellation, deduplication and priority interruption.</summary>
[DisallowMultipleComponent]
public sealed class GronnyVoicePlayer : MonoBehaviour
{
    private sealed class Request
    {
        public GronnyAudioCatalogue.Cue cue;
        public string key;
        public Func<bool> relevant;
        public Func<bool> demonstrationRelevant;
        public Action started;
    }

    private readonly List<Request> pending = new();
    private readonly HashSet<string> delivered = new();
    private readonly Dictionary<string, float> lastPlayed = new();
    private Request current;
    private AudioSource source;
    private GronnyAudioCatalogue catalogue;
    private GardenControlDemonstration demonstration;
    private float nextVoiceAt;

    public void Configure(GronnyAudioCatalogue settings, GardenControlDemonstration visuals)
    {
        catalogue = settings;
        demonstration = visuals;
        // A dedicated source keeps tutorial voice independent of ambient audio.
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.pitch = catalogue.pitch;
        var gestures = FindFirstObjectByType<GardenerMixamoGreeting>();
        if (gestures != null) gestures.BindVoiceSource(source);
    }

    public void Enqueue(string id, string key, Func<bool> relevant, Action started = null, bool once = true,
        Func<bool> demonstrationRelevant = null)
    {
        GronnyAudioCatalogue.Cue cue = catalogue.Find(id);
        if (cue == null || cue.clip == null)
        {
            Debug.LogWarning("Gronny audio is missing: " + id, this);
            return;
        }
        if (!isActiveAndEnabled || relevant == null || !relevant() ||
            (once && delivered.Contains(key)) || (current != null && current.key == key) ||
            pending.Exists(request => request.key == key) ||
            (lastPlayed.TryGetValue(id, out float last) && Time.unscaledTime - last < cue.cooldown)) return;

        var request = new Request { cue = cue, key = key, relevant = relevant, started = started,
            demonstrationRelevant = demonstrationRelevant };
        if (current != null && cue.priority > current.cue.priority)
        {
            // An interrupted, still relevant instruction can resume after the urgent warning.
            Request interrupted = current;
            StopCurrent();
            if (interrupted.relevant()) pending.Insert(0, interrupted);
        }
        pending.Add(request);
    }

    public void CancelControls()
    {
        pending.RemoveAll(request => request.cue.mode != GardenInputMode.Unknown);
        if (current != null && current.cue.mode != GardenInputMode.Unknown) StopCurrent();
    }

    public void Clear()
    {
        pending.Clear();
        StopCurrent();
    }

    private void Update()
    {
        if (source == null) return;
        pending.RemoveAll(request => !request.relevant());
        if (current != null)
        {
            if (!current.relevant() || !source.isPlaying) StopCurrent();
            else
            {
                if (current.demonstrationRelevant != null && !current.demonstrationRelevant()) demonstration.Hide();
                else demonstration.Tick(source.time);
                return;
            }
        }
        if (Time.unscaledTime < nextVoiceAt || pending.Count == 0) return;
        int selected = 0;
        for (int i = 1; i < pending.Count; i++)
            if (pending[i].cue.priority > pending[selected].cue.priority) selected = i;
        current = pending[selected];
        pending.RemoveAt(selected);
        delivered.Add(current.key);
        lastPlayed[current.cue.id] = Time.unscaledTime;
        source.clip = current.cue.clip;
        source.volume = catalogue.voiceVolume * current.cue.volume;
        source.pitch = catalogue.pitch;
        source.Play();
        if (current.demonstrationRelevant == null || current.demonstrationRelevant()) demonstration.Show(current.cue);
        current.started?.Invoke();
        current.started = null;
    }

    private void StopCurrent()
    {
        if (source != null) source.Stop();
        if (demonstration != null) demonstration.Hide();
        current = null;
        nextVoiceAt = Time.unscaledTime + (catalogue != null ? catalogue.gapSeconds : 0f);
    }

    private void OnDisable() => Clear();
}
