using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One voice: finish spoken phrases, discard stale queued help, and preserve urgent resume positions.</summary>
[DisallowMultipleComponent]
public sealed class GronnyVoicePlayer : MonoBehaviour
{
    private sealed class Request
    {
        public GronnyAudioCatalogue.Cue cue;
        public AudioClip clip;
        public string key;
        public Func<bool> relevant;
        public Func<bool> demonstrationRelevant;
        public Action started;
        public bool waitForAction;
        public bool audioFinished;
        public bool playbackStarted;
        public bool contextExpired;
        public int resumeSample;
        public float visualElapsed;
    }

    public event Action<string> AudioFinished;

    private readonly List<Request> pending = new();
    private readonly HashSet<string> delivered = new();
    private readonly Dictionary<string, float> lastPlayed = new();
    private readonly Dictionary<string, int> nextVariation = new();
    private Request current;
    private AudioSource source;
    private GronnyAudioCatalogue catalogue;
    private GardenControlDemonstration demonstration;
    private float nextVoiceAt;
    private float visualStartedAt;
    private GronnyAudioCatalogue.Cue backgroundDemonstration;
    private Func<bool> backgroundRelevant;
    private float backgroundStartedAt;
    private bool suspended;
    private float suspendedAt;
    private bool menuPlayback;

    public string CurrentCueId => current != null ? current.cue.id : null;

    public bool TryGetInstruction(out string instruction, out string detail)
    {
        instruction = detail = null;
        if (current == null || suspended || current.contextExpired || !current.relevant() ||
            string.IsNullOrEmpty(current.cue.instruction)) return false;
        instruction = current.cue.instruction;
        detail = current.cue.instructionDetail;
        return true;
    }

    public void Configure(GronnyAudioCatalogue settings, GardenControlDemonstration visuals, bool menuPlayback = false)
    {
        catalogue = settings;
        demonstration = visuals;
        this.menuPlayback = menuPlayback;
        // A dedicated source keeps tutorial voice independent of ambient audio.
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.pitch = catalogue.pitch;
        var gestures = FindFirstObjectByType<GardenerMixamoGreeting>();
        if (gestures != null && !menuPlayback) gestures.BindVoiceSource(source);
    }

    public void SetBackgroundDemonstration(string id, Func<bool> relevant)
    {
        backgroundDemonstration = catalogue.Find(id);
        backgroundRelevant = relevant;
    }

    public void Enqueue(string id, string key, Func<bool> relevant, Action started = null, bool once = true,
        Func<bool> demonstrationRelevant = null, bool waitForAction = false)
    {
        GronnyAudioCatalogue.Cue cue = menuPlayback ? catalogue.Find(id) : catalogue.FindTutorialCue(id);
        if (cue == null || cue.clip == null)
        {
            Debug.LogWarning("Gronny audio is missing: " + id, this);
            return;
        }
        if (!isActiveAndEnabled || relevant == null || !relevant() ||
            (once && delivered.Contains(key)) || (current != null && current.key == key) ||
            pending.Exists(request => request.key == key) ||
            (!waitForAction && lastPlayed.TryGetValue(id, out float last) && Time.unscaledTime - last < cue.cooldown)) return;

        var request = new Request { cue = cue, key = key, relevant = relevant, started = started,
            demonstrationRelevant = demonstrationRelevant, waitForAction = waitForAction };
        if (current != null && cue.priority >= 100 && cue.priority > current.cue.priority)
        {
            // Only urgent warnings interrupt. Resume at the sample, never at the first word.
            Request interrupted = current;
            interrupted.resumeSample = interrupted.audioFinished ? 0 : source.timeSamples;
            interrupted.visualElapsed = Time.unscaledTime - visualStartedAt;
            StopCurrent();
            if (!interrupted.contextExpired && interrupted.relevant()) pending.Insert(0, interrupted);
        }
        pending.Add(request);
    }

    public void CancelControls()
    {
        pending.RemoveAll(request => request.cue.mode != GardenInputMode.Unknown);
        if (current != null && current.cue.mode != GardenInputMode.Unknown) ExpireCurrentContext();
    }

    public void FinishCurrent()
    {
        pending.Clear();
        ExpireCurrentContext();
    }

    private void ExpireCurrentContext()
    {
        if (current == null) return;
        current.contextExpired = true;
        current.waitForAction = false;
        demonstration.Hide();
    }

    public void Clear()
    {
        pending.Clear();
        StopCurrent();
    }

    public void SuspendForMenu()
    {
        if (source == null || suspended || menuPlayback) return;
        suspended = true;
        suspendedAt = Time.unscaledTime;
        source.Pause();
        demonstration.Hide();
    }

    private void Update()
    {
        if (source == null) return;
        // Apply on every frame so changing a preference also changes an utterance already playing.
        if (current != null) source.volume = catalogue.voiceVolume * current.cue.volume * GardenPreferences.VoiceVolume;
        if (!menuPlayback && GardenMenuController.GardenSuspended)
        {
            SuspendForMenu();
            return;
        }
        if (suspended)
        {
            float duration = Time.unscaledTime - suspendedAt;
            visualStartedAt += duration;
            backgroundStartedAt += duration;
            nextVoiceAt += duration;
            suspended = false;
            if (current != null && !current.audioFinished) source.UnPause();
        }
        pending.RemoveAll(request => !request.relevant());
        if (current != null)
        {
            if (!current.contextExpired && !current.relevant()) ExpireCurrentContext();
            Request playing = current;
            // Completing an action ends the demo immediately but lets the sentence finish.
            if (!source.isPlaying && !playing.audioFinished)
            {
                playing.audioFinished = true;
                AudioFinished?.Invoke(playing.cue.id);
                if (current != playing) return;
            }
            if (!source.isPlaying && !playing.waitForAction) StopCurrent();
            else
            {
                if (playing.contextExpired) demonstration.Hide();
                else if (playing.cue.demo == GardenControlDemo.None) UpdateBackgroundDemonstration();
                else if (playing.demonstrationRelevant != null && !playing.demonstrationRelevant()) demonstration.Hide();
                else
                {
                    if (!demonstration.IsShowing(playing.cue)) demonstration.Show(playing.cue);
                    // Spoken playback follows the clip; the slower teaching cycle starts after speech ends.
                    if (source.isPlaying) demonstration.TickSpoken(source.time);
                    else
                    {
                        float practiceElapsed = Mathf.Max(0f, Time.unscaledTime - visualStartedAt -
                            playing.clip.length / catalogue.pitch);
                        if (demonstration.UsesHandTeachingTiming)
                            demonstration.TickTeaching(Mathf.Repeat(practiceElapsed, demonstration.CycleDurationSeconds));
                        else demonstration.Tick(Mathf.Repeat(practiceElapsed,
                            playing.clip.length / catalogue.pitch) * catalogue.pitch);
                    }
                }
                return;
            }
        }
        if (Time.unscaledTime < nextVoiceAt || pending.Count == 0)
        {
            UpdateBackgroundDemonstration();
            return;
        }
        int selected = 0;
        for (int i = 1; i < pending.Count; i++)
            if (pending[i].cue.priority > pending[selected].cue.priority) selected = i;
        current = pending[selected];
        pending.RemoveAt(selected);
        if (!current.playbackStarted)
        {
            // Choose only when speech starts. Cancelled requests do not consume a take,
            // and interrupted speech keeps the same recording and resume sample.
            current.clip = SelectClip(current.cue);
            delivered.Add(current.key);
            lastPlayed[current.cue.id] = Time.unscaledTime;
        }
        source.clip = current.clip;
        source.volume = catalogue.voiceVolume * current.cue.volume * GardenPreferences.VoiceVolume;
        source.pitch = catalogue.pitch;
        if (!current.audioFinished)
        {
            source.Play();
            if (current.resumeSample > 0) source.timeSamples = Mathf.Min(current.resumeSample, current.clip.samples - 1);
        }
        visualStartedAt = Time.unscaledTime - current.visualElapsed;
        if (current.cue.demo == GardenControlDemo.None) UpdateBackgroundDemonstration();
        else if (current.demonstrationRelevant == null || current.demonstrationRelevant()) demonstration.Show(current.cue);
        current.playbackStarted = true;
        Action onStarted = current.started;
        current.started = null;
        onStarted?.Invoke();
    }

    private AudioClip SelectClip(GronnyAudioCatalogue.Cue cue)
    {
        int count = 1 + (cue.variations?.Length ?? 0);
        nextVariation.TryGetValue(cue.id, out int next);
        for (int offset = 0; offset < count; offset++)
        {
            int index = (next + offset) % count;
            AudioClip clip = index == 0 ? cue.clip : cue.variations[index - 1]?.clip;
            if (clip == null) continue;
            nextVariation[cue.id] = (index + 1) % count;
            return clip;
        }
        return cue.clip;
    }

    private void UpdateBackgroundDemonstration()
    {
        if (backgroundDemonstration == null || backgroundDemonstration.clip == null ||
            backgroundRelevant == null || !backgroundRelevant())
        {
            demonstration.Hide();
            return;
        }
        // Continue the visual while later spoken instructions play, without replaying its audio.
        if (!demonstration.IsShowing(backgroundDemonstration))
        {
            demonstration.Show(backgroundDemonstration);
            backgroundStartedAt = Time.unscaledTime;
        }
        float duration = Mathf.Max(0.01f, backgroundDemonstration.clip.length / catalogue.pitch);
        demonstration.Tick(Mathf.Repeat(Time.unscaledTime - backgroundStartedAt, duration) * catalogue.pitch);
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
