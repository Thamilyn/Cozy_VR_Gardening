using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Oculus.Interaction;
using UnityEditor;
using UnityEngine;

/// <summary>Exercises the spoken walking invitation and subsequent seed guidance.</summary>
[InitializeOnLoad]
public static class GronnyMovementLessonVerification
{
    private const string SessionKey = "CozyGarden.MovementLessonVerification";
    private static readonly List<string> checks = new();
    private static IEnumerator routine;
    private static double nextAt;
    private static double startedAt;

    static GronnyMovementLessonVerification() => EditorApplication.playModeStateChanged += OnPlayChanged;

    [MenuItem("Garden/Gronny/Verify welcome and movement in Play")]
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
            routine = null;
            SessionState.SetBool(SessionKey, false);
        }
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextAt) return;
        try
        {
            if (EditorApplication.timeSinceStartup - startedAt > 40) throw new TimeoutException("Movement lesson verification timed out.");
            if (routine.MoveNext()) { nextAt = EditorApplication.timeSinceStartup + Convert.ToSingle(routine.Current); return; }
            Finish(null);
        }
        catch (Exception error) { Finish(error); }
    }

    private static void Finish(Exception error)
    {
        EditorApplication.update -= Tick;
        routine = null;
        Directory.CreateDirectory("output");
        File.WriteAllLines("output/WelcomeMovementPlay.txt", new[]
        {
            "Unity " + Application.unityVersion, "UTC " + DateTime.UtcNow.ToString("O"),
            "Real lesson and AudioSource with audio seeks; spoken guidance only; no physical Quest test."
        }.Concat(checks.Select(check => "PASS: " + check)).Concat(new[] { error == null ? "RESULT: PASS" : "RESULT: FAIL\n" + error }));
        if (error == null) Debug.Log("Welcome and movement passed: " + checks.Count + " checks.");
        else Debug.LogError(error);
        EditorApplication.isPlaying = false;
    }

    private static IEnumerator Run()
    {
        yield return 1f;
        var rig = UnityEngine.Object.FindFirstObjectByType<OVRCameraRig>();
        rig.enabled = false;
        rig.centerEyeAnchor.position = rig.transform.position + Vector3.up * 1.6f;
        var menu = UnityEngine.Object.FindFirstObjectByType<GardenMenuController>();
        Call(menu, "HandleInputRelease", false, Time.unscaledTime);
        Call(menu, "HandleInputRelease", true, Time.unscaledTime);
        Call(menu, "HandleInputRelease", true, Time.unscaledTime + 0.16f);
        Call(menu, "Continue");
        Call(menu, "HandleInputRelease", true, Time.unscaledTime);
        Call(menu, "HandleInputRelease", true, Time.unscaledTime + 0.21f);
        yield return 0.6f;
        var tutorial = UnityEngine.Object.FindFirstObjectByType<GronnyTutorial>();
        var voice = tutorial.GetComponent<GronnyVoicePlayer>();
        var source = tutorial.GetComponent<AudioSource>();
        source.mute = true;
        var learned = (HashSet<string>)Field(tutorial, "learned");
        Set(tutorial, "mode", GardenInputMode.Unknown);
        Set(tutorial, "candidateMode", GardenInputMode.Unknown);
        Set(tutorial, "modeChangedAt", Time.unscaledTime + 100f);
        tutorial.GetComponent<GardenControlDemonstration>().SetInputMode(GardenInputMode.Hands);
        Call(tutorial, "QueueControls");
        Check(voice.CurrentCueId == "01_welcome", "Welcome begins after Play");
        Check(!learned.Contains("move"), "The welcome does not skip the spoken walking instruction");
        source.time = source.clip.length - 0.1f;
        yield return 0.9f;
        Check(voice.CurrentCueId == "roomscale_walk", "Gronny directs the player to the gardening table without requiring tracked hands or controllers");
        string walkingScript = Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial").Find("roomscale_walk").transcript;
        Check(walkingScript.Contains("gardening table") && walkingScript.Contains("Tomato seed"),
            "Walking speech connects the gardening table with the named tomato seed");
        voice.CancelControls();
        Check(voice.CurrentCueId == "roomscale_walk", "Changing input modes preserves the spoken walking guidance");
        yield return 0.2f;
        Check(!learned.Contains("move") && source.isPlaying, "Gronny finishes the walking sentence before explaining the seed");
        Set(tutorial, "mode", GardenInputMode.Hands);
        source.time = source.clip.length - 0.1f;
        yield return 0.9f;
        Check(learned.Contains("move"), "Finishing the voice instruction continues without measuring steps or distance");
        Check(voice.CurrentCueId == "controls_grab_hands", "Seed guidance follows the spoken invitation");
        var seed = UnityEngine.Object.FindObjectsByType<SeedItem>(FindObjectsSortMode.None)
            .First(item => item.Crop == SeedCrop.Tomato && !item.IsPlanted);
        var seedLabel = seed.GetComponent<GardenObjectLabel>();
        Check(((GameObject)Field(seedLabel, "label")).GetComponentInChildren<UnityEngine.UI.Text>(true).text == "Tomato seed",
            "The real seed label matches the spoken Tomato seed instruction");
        var grab = seed.GetComponent<Grabbable>();
        var pose = new Pose(seed.transform.position, seed.transform.rotation);
        grab.ProcessPointerEvent(new PointerEvent(9895, PointerEventType.Hover, pose));
        grab.ProcessPointerEvent(new PointerEvent(9895, PointerEventType.Select, pose));
        Check(grab.SelectingPointsCount == 1 && learned.Contains("grab") &&
            tutorial.TryGetInstruction(out string instruction, out _) && instruction.Contains("Step 3/6"),
            "The real seed Select event advances to planting while the seed is still held");
        Check(!seed.IsPlanted && GronnyTutorial.Allows(GardenTutorialAction.Plant),
            "Picking up a seed prepares planting without planting it or requiring a practice drop");
        yield return 0.2f;
        Check(voice.CurrentCueId == "controls_grab_hands" && source.isPlaying,
            "Picking up the seed lets the current grab sentence finish");
        source.time = source.clip.length - 0.1f;
        yield return 0.9f;
        Check(voice.CurrentCueId == "03_plant_seed" && grab.SelectingPointsCount == 1 && !seed.IsPlanted,
            "Planting speech follows the grab sentence while the seed is still held");
        Check(tutorial.TryGetSpokenInstruction(out instruction, out _) &&
            instruction == Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial").Find("03_plant_seed").instruction,
            "Planting text matches the spoken instruction before release");
        grab.ProcessPointerEvent(new PointerEvent(9895, PointerEventType.Unselect, pose));
        grab.ProcessPointerEvent(new PointerEvent(9895, PointerEventType.Unhover, pose));
    }

    private static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Call(object target, string method, params object[] arguments) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks.Add(message);
    }
}
