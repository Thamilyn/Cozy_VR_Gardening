using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Real scene transition and crop events, with synthetic learning/input samples.</summary>
[InitializeOnLoad]
public static class GardenPracticeVerification
{
    private const string SessionKey = "CozyGarden.PracticeVerification";
    private static readonly List<string> checks = new();
    private static IEnumerator routine;
    private static double nextAt;
    private static double startedAt;

    static GardenPracticeVerification() => EditorApplication.playModeStateChanged += OnPlayChanged;

    [MenuItem("Garden/Verify practice to gardening in Play")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !GardenSessionFlow.IsPractice)
            throw new InvalidOperationException("Open Garden_Practice outside Play first.");
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
            if (EditorApplication.timeSinceStartup - startedAt > 100) throw new TimeoutException("Practice verification timed out.");
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
        File.WriteAllLines("output/PracticeToGardenVerification.txt", new[]
        {
            "Unity " + Application.unityVersion, "UTC " + DateTime.UtcNow.ToString("O"),
            "Synthetic learning and release samples; actual scenes, planting, water, calendar and voice playback. Physical XR input not tested."
        }.Concat(checks.Select(check => "PASS: " + check)).Concat(new[] { error == null ? "RESULT: PASS" : "RESULT: FAIL\n" + error }));
        if (error != null) Debug.LogError(error); else Debug.Log("Practice to garden passed: " + checks.Count + " checks.");
        EditorApplication.isPlaying = false;
    }

    private static IEnumerator Run()
    {
        yield return 1f;
        var menu = UnityEngine.Object.FindFirstObjectByType<GardenMenuController>();
        var seeds = UnityEngine.Object.FindFirstObjectByType<SeedsController>();
        var tutorial = UnityEngine.Object.FindFirstObjectByType<GronnyTutorial>();
        var supply = GardenWaterSupply.Active;
        var calendar = seeds.Calendar;
        var can = UnityEngine.Object.FindFirstObjectByType<WateringCan>();
        var rig = UnityEngine.Object.FindFirstObjectByType<OVRCameraRig>();
        rig.enabled = false;
        rig.centerEyeAnchor.position = new Vector3(0f, 1.6f, -7.88f);
        Check(supply.IsPractice && supply.ReserveMillilitres == 0f && supply.Attempt == 0, "Practice has its own free water supply");
        Check(!GardenMenuController.HasStarted && tutorial.GetComponent<GronnyVoicePlayer>() == null, "Practice waits for Play without starting the lesson");
        Arm(menu);
        View(menu).Content.GetComponentsInChildren<Button>().Single(button => button.name == "Play").onClick.Invoke();
        Release(menu);
        yield return 0.6f;
        var voice = tutorial.GetComponent<GronnyVoicePlayer>();
        Check(voice.CurrentCueId == "02_tomato_intro", "Practice uses the separate practice welcome");
        Check(!GronnyTutorial.Allows(GardenTutorialAction.Plant), "Practice requires movement before planting");
        Invoke(tutorial, "Learn", "move");
        SeedItem seed = UnityEngine.Object.FindObjectsByType<SeedItem>(FindObjectsSortMode.None).First(item => item.Crop == SeedCrop.Tomato);
        GronnyTutorial.RecordSeedRelease(seed);
        GardenPot pot = UnityEngine.Object.FindObjectsByType<GardenPot>(FindObjectsSortMode.None).First(item => item.AcceptsTomato);
        seed.transform.position = pot.PlantingPoint;
        Check(seeds.TryPlant(seed), "Practice uses real seed planting after synthetic learning samples");
        Check(seed.StageVisual.name == "Tomatev1.4", "Planting shows the seed model, not the empty pot");
        seeds.ReceiveWater(pot, seeds.TargetMillilitres(seeds.PrototypeTomato) * 1.01f / 1000f);
        seeds.GetState(seeds.PrototypeTomato).lastWaterAt = Time.unscaledTime - 1f;
        Invoke(can, "SetSessionWater", 0.12f, 0.06f);
        var visibility = UnityEngine.Object.FindFirstObjectByType<CalendarVisibilityController>();
        visibility.Open();
        Check(GronnyTutorial.Allows(GardenTutorialAction.Refill) && !GronnyTutorial.Allows(GardenTutorialAction.Advance), "Practice teaches refill before advancing time");
        var panel = UnityEngine.Object.FindFirstObjectByType<CalendarPanel>(FindObjectsInactive.Include);
        panel.RefillCans();
        Check(can.CurrentWaterLitres > 0.06f && supply.IsPractice && supply.ReserveMillilitres == 0f, "Practice refill is real and does not consume a reserve");
        Check(GronnyTutorial.Allows(GardenTutorialAction.Advance), "Successful refill unlocks the practice calendar advance");
        voice.Clear();
        panel.AdvancePhase();
        Check(calendar.IsAdvancing, "Practice uses the real day and night transition");
        while (calendar.IsAdvancing) yield return 0.1f;
        Check(seeds.GetState(seeds.PrototypeTomato).tomatoPhase == 1 && seed.StageVisual.name == "TomatPhase2", "The sprout model appears in the same committed day change");
        yield return 0.6f;
        Check(voice.CurrentCueId == "08_sprout", "Sprout voice starts promptly without queued reserve announcements");
        Check(!GronnyTutorial.Allows(GardenTutorialAction.Pour), "Practice stops growing after the first sprout");
        tutorial.GetComponent<AudioSource>().time = Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial").Find("08_sprout").clip.length - 0.1f;
        yield return 0.5f;
        Check(GardenMenuController.GardenSuspended && View(menu).Content.GetComponentsInChildren<Button>().Any(button => button.name == "Enter garden"), "Completed practice offers an explicit Enter garden button");
        Check(View(menu).Content.GetComponentsInChildren<Text>().All(text => text.preferredHeight <= text.rectTransform.rect.height + 2f), "Practice completion text fits the panel");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("output/PracticeComplete.png"));
        Arm(menu);
        View(menu).Content.GetComponentsInChildren<Button>().Single(button => button.name == "Enter garden").onClick.Invoke();
        Check(GardenMenuController.GardenSuspended && GardenSessionFlow.IsPractice, "Entering the garden waits for the completion selection to be released");
        Release(menu);
        while (SceneManager.GetActiveScene().name != GardenSessionFlow.GardeningScene) yield return 0.1f;
        yield return 1f;
        menu = UnityEngine.Object.FindFirstObjectByType<GardenMenuController>();
        Release(menu);
        yield return 0.6f;
        seeds = UnityEngine.Object.FindFirstObjectByType<SeedsController>();
        tutorial = UnityEngine.Object.FindFirstObjectByType<GronnyTutorial>();
        supply = GardenWaterSupply.Active;
        voice = tutorial.GetComponent<GronnyVoicePlayer>();
        Check(!GardenSessionFlow.IsPractice && GardenMenuController.HasStarted && !GardenMenuController.GardenSuspended, "Practice transitions to the real garden without a second Play screen");
        Check(seeds.PlantedSeeds.Count == 0 && seeds.Calendar.CurrentDay == 0 && seeds.Calendar.Journal.Count == 0, "Gardening starts a fresh crop and history without the practice plant");
        Check(!supply.IsPractice && supply.Attempt == 1 && supply.ReserveMillilitres > 0f && supply.TotalWastedMillilitres == 0f, "Gardening has a finite fresh reserve from the start");
        Check(Mathf.Abs(supply.AttemptBudgetMillilitres - supply.ReserveMillilitres - supply.WaterInCansMillilitres) < 0.05f, "Initial can water is debited from the finite garden budget");
        Check(GronnyTutorial.Allows(GardenTutorialAction.Plant) && GronnyTutorial.Allows(GardenTutorialAction.OpenCalendar) && !tutorial.TryGetInstruction(out _, out _), "Gardening does not repeat progressive practice locks");
        Check(voice.CurrentCueId == "01_welcome", "Gronny greets the player on entering the sustainable garden");
        string text = Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial").Find("01_welcome").transcript;
        Check(text.Contains("I'm Gronny") && text.Contains("sustainable") && text.Contains("reserve is limited"), "Garden welcome introduces Gronny and explains sustainability and limited water");
        tutorial.GetComponent<AudioSource>().time = tutorial.GetComponent<AudioSource>().clip.length - 0.1f;
        yield return 0.9f;
        Check(voice.CurrentCueId == "water_reserve_explanation", "Refill explanation follows the garden welcome");
    }

    private static GardenMenuView View(GardenMenuController menu) => (GardenMenuView)Field(menu, "menu");
    private static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    private static void Arm(GardenMenuController menu)
    {
        Invoke(menu, "HandleInputRelease", false, Time.unscaledTime);
        Invoke(menu, "HandleInputRelease", true, Time.unscaledTime);
        Invoke(menu, "HandleInputRelease", true, Time.unscaledTime + 0.16f);
    }
    private static void Release(GardenMenuController menu)
    {
        Invoke(menu, "HandleInputRelease", true, Time.unscaledTime);
        Invoke(menu, "HandleInputRelease", true, Time.unscaledTime + 0.21f);
    }
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        checks.Add(name);
    }
}
