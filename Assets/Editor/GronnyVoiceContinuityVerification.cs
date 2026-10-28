using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Real AudioSource continuity with synthetic action/context changes in Play.</summary>
[InitializeOnLoad]
public static class GronnyVoiceContinuityVerification
{
    private const string SessionKey = "CozyGarden.VoiceContinuityVerification";
    private static readonly List<string> checks = new();
    private static IEnumerator routine;
    private static double nextAt;
    private static double startedAt;

    static GronnyVoiceContinuityVerification() => EditorApplication.playModeStateChanged += OnPlayChanged;

    [MenuItem("Garden/Gronny/Verify voice continuity in Play")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start outside Play.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Garden_Moves")
            throw new InvalidOperationException("Open Garden_Moves first.");
        SessionState.SetBool(SessionKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(SessionKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            checks.Clear();
            startedAt = nextAt = EditorApplication.timeSinceStartup;
            routine = Run();
            EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick;
            (routine as IDisposable)?.Dispose();
            routine = null;
            SessionState.SetBool(SessionKey, false);
        }
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextAt) return;
        try
        {
            if (EditorApplication.timeSinceStartup - startedAt > 60) throw new TimeoutException("Voice continuity verification timed out.");
            if (routine.MoveNext()) { nextAt = EditorApplication.timeSinceStartup + Convert.ToSingle(routine.Current); return; }
            Finish(null);
        }
        catch (Exception error) { Finish(error); }
    }

    private static void Finish(Exception error)
    {
        EditorApplication.update -= Tick;
        (routine as IDisposable)?.Dispose();
        routine = null;
        Directory.CreateDirectory("output");
        File.WriteAllLines("output/VoiceContinuityPlay.txt", new[]
        {
            "Unity " + Application.unityVersion, "UTC " + DateTime.UtcNow.ToString("O"),
            "Real AudioSource with synthetic relevance changes and sample seeks. Physical headset audio not tested."
        }.Concat(checks.Select(check => "PASS: " + check)).Concat(new[] { error == null ? "RESULT: PASS" : "RESULT: FAIL\n" + error }));
        if (error == null) Debug.Log("Voice continuity passed: " + checks.Count + " checks.");
        else Debug.LogError(error);
        EditorApplication.isPlaying = false;
    }

    private static IEnumerator Run()
    {
        yield return 1f;
        var rig = UnityEngine.Object.FindFirstObjectByType<OVRCameraRig>();
        rig.enabled = false;
        rig.centerEyeAnchor.position = new Vector3(0f, 1.6f, -7.88f);
        var catalogue = Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial");
        var fixture = new GameObject("Voice continuity fixture (temporary)");
        try
        {
            var demo = fixture.AddComponent<GardenControlDemonstration>();
            demo.Configure(catalogue);
            demo.SetInputMode(GardenInputMode.Hands);
            var voice = fixture.AddComponent<GronnyVoicePlayer>();
            voice.Configure(catalogue, demo, menuPlayback: true);
            var source = fixture.GetComponent<AudioSource>();
            source.mute = true;

            bool actionPending = true;
            int actionStarts = 0;
            voice.Enqueue("controls_grab_hands", "action", () => actionPending, () => actionStarts++, waitForAction: true);
            yield return 0.6f;
            Check(source.isPlaying && voice.CurrentCueId == "controls_grab_hands", "A control instruction starts normally");
            source.time = 1f;
            actionPending = false;
            yield return 0.25f;
            Check(source.isPlaying && voice.CurrentCueId == "controls_grab_hands" && source.time > 1f,
                "Completing an action preserves the playing sentence instead of cutting it");
            Check(!demo.IsShowing(catalogue.Find("controls_grab_hands")), "Completed action hides its obsolete hand demonstration immediately");
            voice.Enqueue("03_plant_seed", "after-action", () => true);
            SeekToEnd(source);
            yield return 0.9f;
            Check(voice.CurrentCueId == "03_plant_seed" && actionStarts == 1, "Next instruction waits for the previous sentence and never restarts it");

            voice.Clear();
            bool pendingRelevant = true;
            voice.Enqueue("01_welcome", "ordinary", () => true);
            yield return 0.6f;
            voice.Enqueue("03_plant_seed", "stale-pending", () => pendingRelevant);
            pendingRelevant = false;
            voice.Enqueue("06_enough_water", "positive-feedback", () => true);
            yield return 0.25f;
            Check(voice.CurrentCueId == "01_welcome" && source.isPlaying, "Ordinary higher-priority feedback does not cut the welcome");
            SeekToEnd(source);
            yield return 0.9f;
            Check(voice.CurrentCueId == "06_enough_water", "Obsolete pending instructions are discarded before playback");

            voice.Clear();
            voice.Enqueue("01_welcome", "before-phase", () => true);
            yield return 0.6f;
            source.time = 1f;
            voice.Enqueue("03_plant_seed", "old-phase", () => true);
            typeof(GronnyVoicePlayer).GetMethod("FinishCurrent").Invoke(voice, null);
            voice.Enqueue("08_sprout", "new-phase", () => true);
            yield return 0.25f;
            Check(source.isPlaying && voice.CurrentCueId == "01_welcome", "Changing phase finishes the spoken sentence while clearing old queued instructions");
            SeekToEnd(source);
            yield return 0.9f;
            Check(voice.CurrentCueId == "08_sprout", "The new phase starts after the sentence finishes");

            voice.Clear();
            voice.Enqueue("controls_grab_hands", "mode-hands", () => true, waitForAction: true);
            yield return 0.6f;
            source.time = 1f;
            voice.CancelControls();
            demo.SetInputMode(GardenInputMode.Controllers);
            voice.Enqueue("controls_grab_controllers", "mode-controllers", () => true, waitForAction: true);
            yield return 0.25f;
            Check(source.isPlaying && voice.CurrentCueId == "controls_grab_hands", "Changing input mode does not sever the current phrase");
            Check(!demo.IsShowing(catalogue.Find("controls_grab_hands")), "Input change hides the previous mode's visual immediately");
            SeekToEnd(source);
            yield return 0.9f;
            Check(voice.CurrentCueId == "controls_grab_controllers", "The new input instructions follow the completed phrase");

            voice.Clear();
            int resumedStarts = 0;
            voice.Enqueue("controls_grab_controllers", "resume", () => true, () => resumedStarts++, waitForAction: true);
            yield return 0.6f;
            source.time = 1.5f;
            float interruptedAt = source.time;
            voice.Enqueue("help_excess_water", "urgent", () => true);
            yield return 0.6f;
            Check(voice.CurrentCueId == "help_excess_water", "An urgent excess-water warning can interrupt");
            SeekToEnd(source);
            yield return 0.9f;
            Check(voice.CurrentCueId == "controls_grab_controllers" && source.time >= interruptedAt - 0.05f,
                "A still-relevant interrupted instruction resumes from its saved audio position");
            Check(resumedStarts == 1, "Resuming an urgent interruption does not log another instruction start");
            voice.Clear();
            Check(!source.isPlaying && voice.CurrentCueId == null, "Explicit Stop still stops playback immediately");

            // Exercise the actual tutorial callers as well as the isolated queue.
            var menu = UnityEngine.Object.FindFirstObjectByType<GardenMenuController>();
            Call(menu, "HandleInputRelease", false, Time.unscaledTime);
            Call(menu, "HandleInputRelease", true, Time.unscaledTime);
            Call(menu, "HandleInputRelease", true, Time.unscaledTime + 0.16f);
            Call(menu, "Continue");
            Call(menu, "HandleInputRelease", true, Time.unscaledTime);
            Call(menu, "HandleInputRelease", true, Time.unscaledTime + 0.21f);
            yield return 0.6f;
            var tutorial = UnityEngine.Object.FindFirstObjectByType<GronnyTutorial>();
            var tutorialVoice = tutorial.GetComponent<GronnyVoicePlayer>();
            var tutorialAudio = tutorial.GetComponent<AudioSource>();
            tutorialAudio.mute = true;
            Check(tutorialVoice.CurrentCueId == "01_welcome", "The real tutorial begins its welcome after Play");
            tutorialAudio.time = 1f;
            Call(tutorial, "Learn", "move");
            var seeds = UnityEngine.Object.FindFirstObjectByType<SeedsController>();
            var seed = UnityEngine.Object.FindObjectsByType<SeedItem>(FindObjectsSortMode.None).First(item => item.Crop == SeedCrop.Tomato);
            var pot = UnityEngine.Object.FindObjectsByType<GardenPot>(FindObjectsSortMode.None).First(item => item.AcceptsTomato);
            GronnyTutorial.RecordSeedRelease(seed);
            seed.transform.position = pot.PlantingPoint;
            Check(seeds.TryPlant(seed), "Integration fixture plants through the real tutorial permissions");
            yield return 0.25f;
            Check(tutorialVoice.CurrentCueId == "01_welcome" && tutorialAudio.isPlaying && tutorialAudio.time > 1f,
                "The real planting phase change preserves the welcome sentence");
            seeds.ReceiveWater(pot, seeds.TargetMillilitres(seeds.PrototypeTomato) * 1.01f / 1000f);
            seeds.GetState(seeds.PrototypeTomato).lastWaterAt = Time.unscaledTime - 1f;
            yield return 0.25f;
            Check(tutorialVoice.CurrentCueId == "01_welcome" && tutorialAudio.isPlaying,
                "Reaching the actual water target queues feedback without cutting the welcome");
            SeekToEnd(tutorialAudio);
            yield return 0.9f;
            Check(tutorialVoice.CurrentCueId == "06_enough_water", "Real watering feedback follows the finished sentence");
            tutorialAudio.time = 1f;
            UnityEngine.Object.FindFirstObjectByType<CalendarVisibilityController>().Open();
            UnityEngine.Object.FindFirstObjectByType<CalendarPanel>(FindObjectsInactive.Include).AdvancePhase();
            yield return 0.25f;
            Check(seeds.Calendar.IsAdvancing && tutorialVoice.CurrentCueId == "06_enough_water" && tutorialAudio.isPlaying && tutorialAudio.time > 1f,
                "Starting the real day transition does not repeatedly clear the current audio");
        }
        finally { UnityEngine.Object.Destroy(fixture); }
    }

    private static void SeekToEnd(AudioSource source) => source.time = source.clip.length - 0.1f;
    private static void Call(object target, string method, params object[] arguments) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks.Add(message);
    }
}
