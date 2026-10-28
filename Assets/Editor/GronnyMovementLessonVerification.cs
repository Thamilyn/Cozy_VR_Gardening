using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Oculus.Interaction.Locomotion;
using UnityEditor;
using UnityEngine;

/// <summary>Exercises the real lesson and voice queue using synthetic Meta locomotion events.</summary>
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
            "Real lesson and AudioSource; synthetic locomotion events and audio seeks; no physical Quest test."
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
        rig.centerEyeAnchor.position = new Vector3(0f, 1.6f, -7.88f);
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
        Set(tutorial, "mode", GardenInputMode.Hands);
        Set(tutorial, "candidateMode", GardenInputMode.Unknown);
        Set(tutorial, "modeChangedAt", Time.unscaledTime + 100f);
        tutorial.GetComponent<GardenControlDemonstration>().SetInputMode(GardenInputMode.Hands);
        Call(tutorial, "QueueControls");
        Check(voice.CurrentCueId == "01_welcome", "Welcome begins after Play");
        Move(tutorial, rig, Vector3.right * 0.3f, LocomotionEvent.TranslationType.Relative);
        yield return 0.2f;
        Check(!learned.Contains("move"), "Movement during the greeting cannot skip the walking lesson");
        source.time = source.clip.length - 0.1f;
        yield return 0.9f;
        Check(voice.CurrentCueId == "controls_move_hands", "Walking follows the combined welcome without an extra reserve speech");
        Check((bool)Field(tutorial, "movementPracticeStarted"), "Walking detection starts with the actual instruction playback");
        yield return 0.6f;
        Check(!learned.Contains("move"), "Waiting alone does not complete the lesson");
        rig.centerEyeAnchor.position += Vector3.right * 0.5f;
        yield return 0.2f;
        Check(!learned.Contains("move"), "Moving the headset alone does not complete walking");
        var rotation = new LocomotionEvent(1, Quaternion.Euler(0f, 30f, 0f), LocomotionEvent.RotationType.Relative);
        Call(tutorial, "OnMoved", rotation, new Pose(Vector3.zero, rotation.Pose.rotation));
        yield return 0.2f;
        Check(!learned.Contains("move"), "A rotation event does not count as a step");
        Move(tutorial, rig, Vector3.up * 0.2f, LocomotionEvent.TranslationType.Velocity);
        yield return 0.2f;
        Check(!learned.Contains("move"), "Vertical motion does not count as walking");
        Move(tutorial, rig, Vector3.forward * 0.04f, LocomotionEvent.TranslationType.Velocity);
        yield return 0.2f;
        Check(!learned.Contains("move"), "A small joystick displacement does not yet complete walking");
        Move(tutorial, rig, Vector3.forward * 0.04f, LocomotionEvent.TranslationType.Velocity);
        yield return 0.2f;
        Check(!learned.Contains("move"), "Joystick movement accumulates until the minimum distance");
        Move(tutorial, rig, Vector3.forward * 0.04f, LocomotionEvent.TranslationType.Relative);
        yield return 0.2f;
        Check(learned.Contains("move"), "At least ten centimetres of controlled movement completes walking");
        Check(voice.CurrentCueId == "controls_move_hands" && source.isPlaying,
            "Successful movement lets the current spoken instruction finish");
        source.time = source.clip.length - 0.1f;
        yield return 0.9f;
        Check(voice.CurrentCueId == "movement_success", "Movement success plays after the instruction finishes");
        source.time = source.clip.length - 0.1f;
        yield return 0.9f;
        Check(voice.CurrentCueId == "controls_grab_hands", "Seed instructions follow the movement acknowledgement");
        Call(tutorial, "Learn", "move");
        source.time = source.clip.length - 0.1f;
        yield return 0.9f;
        Check(voice.CurrentCueId != "movement_success", "Movement success is delivered only once");
    }

    private static void Move(GronnyTutorial tutorial, OVRCameraRig rig, Vector3 delta, LocomotionEvent.TranslationType type)
    {
        rig.transform.position += delta;
        Call(tutorial, "OnMoved", new LocomotionEvent(1, delta, type), new Pose(delta, Quaternion.identity));
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
