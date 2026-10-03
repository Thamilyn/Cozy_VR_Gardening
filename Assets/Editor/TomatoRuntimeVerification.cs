using System;
using System.IO;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Optional Play-mode integration check; physical headset input is still a manual check.</summary>
[InitializeOnLoad]
public static class TomatoRuntimeVerification
{
    private const string RequestPath = "Docs/Validation/TomatoRuntimeVerification.request";
    private const string ReportPath = "Docs/Validation/TomatoRuntimeVerification.txt";
    private const string SessionKey = "CozyGarden.TomatoRuntimeCheck";
    private static int step;
    private static double nextStepAt;
    private static double startedAt;
    private static string result;
    private static int runtimePhase;
    private static int phaseStartDay;
    private static bool sawNight;

    static TomatoRuntimeVerification()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += CheckRequest;
    }

    private static void CheckRequest()
    {
        if (!File.Exists(RequestPath)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += CheckRequest;
            return;
        }
        File.Delete(RequestPath);
        Begin();
    }

    [MenuItem("Garden/Verify tomato runtime and restart")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Run this check outside Play.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Garden_Moves")
            throw new Exception("Open Garden_Moves before running this check.");
        SessionState.SetBool(SessionKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(SessionKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            step = 0;
            runtimePhase = 0;
            startedAt = EditorApplication.timeSinceStartup;
            nextStepAt = startedAt + 1.5;
            result = "Unity " + Application.unityVersion + Environment.NewLine +
                "UTC: " + DateTime.UtcNow.ToString("O") + Environment.NewLine;
            EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(SessionKey, false);
        }
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextStepAt) return;
        try
        {
            GardenPhaseTimeCycle cycle = UnityEngine.Object.FindFirstObjectByType<GardenPhaseTimeCycle>();
            double timeout = Math.Max(30, (cycle != null ? cycle.DurationSeconds * 5 : 0) + 15);
            Require(EditorApplication.timeSinceStartup - startedAt < timeout, "Runtime check timeout");
            SeedsController seeds = UnityEngine.Object.FindFirstObjectByType<SeedsController>();
            CalendarPanel panel = UnityEngine.Object.FindFirstObjectByType<CalendarPanel>();
            Require(seeds != null && panel != null, "Runtime components");
            if (step == 0)
            {
                Require(seeds.Calendar.CurrentDay == 0 && seeds.PlantedSeeds.Count == 0, "Clean startup");
                Require(UnityEngine.Object.FindFirstObjectByType<TomatoPrototypeGuide>() != null, "Guide installed");
                Require(UnityEngine.Object.FindFirstObjectByType<GardenGrabSetup>() != null, "Meta setup installed");
                Button[] buttons = panel.GetComponentsInChildren<Button>(true);
                Require(Array.Exists(buttons, b => b.name == "Restart") && Array.Exists(buttons, b => b.name == "Refill"), "Ray buttons created");
                GardenPot pot = Array.Find(UnityEngine.Object.FindObjectsByType<GardenPot>(FindObjectsSortMode.None), p => p.AcceptsTomato && p.SubstrateAreaSquareMetres > 0);
                SeedItem seed = Array.Find(UnityEngine.Object.FindObjectsByType<SeedItem>(FindObjectsSortMode.None), s => s.Crop == SeedCrop.Tomato);
                Require(pot != null && seed != null, "Seed and soil in runtime scene");
                seed.transform.position = pot.PlantingPoint;
                Require(seeds.TryPlant(seed), "Planting in runtime scene");
                Require(cycle != null && cycle.TryPrepare(out _), "Sky cycle configured");
                step = 10;
            }
            else if (step == 10)
            {
                var plant = seeds.PrototypeTomato;
                phaseStartDay = seeds.Calendar.CurrentDay;
                seeds.Calendar.AdvancePhase();
                Require(!seeds.Calendar.IsAdvancing && seeds.Calendar.CurrentDay == phaseStartDay, "Dry phase blocks sky cycle");
                seeds.ReceiveWater(plant.pot, seeds.TargetMillilitres(plant) * 1.01f / 1000f);
                seeds.GetState(plant).lastWaterAt = Time.unscaledTime - 1f;
                seeds.Calendar.AdvancePhase();
                Require(seeds.Calendar.IsAdvancing && seeds.Calendar.CurrentDay == phaseStartDay, "Sky precedes growth");
                float waterBefore = seeds.GetState(plant).waterMillilitres;
                seeds.ReceiveWater(plant.pot, 1f);
                Require(seeds.GetState(plant).waterMillilitres == waterBefore, "Water paused during cycle");
                seeds.Calendar.AdvancePhase();
                sawNight = false;
                step = 11;
            }
            else if (step == 11)
            {
                if (seeds.Calendar.IsAdvancing)
                {
                    Require(seeds.Calendar.CurrentDay == phaseStartDay &&
                        seeds.GetState(seeds.PrototypeTomato).tomatoPhase == runtimePhase, "Growth waits for sky");
                    if (RenderSettings.skybox.GetFloat("_CubemapTransition") > 0.95f) sawNight = true;
                    return;
                }
                Require(sawNight && Mathf.Approximately(RenderSettings.skybox.GetFloat("_CubemapTransition"), 0f),
                    "Night shown and daylight restored");
                runtimePhase++;
                Require(seeds.Calendar.CurrentDay > phaseStartDay && seeds.Calendar.CurrentPhaseIndex == runtimePhase &&
                    seeds.GetState(seeds.PrototypeTomato).tomatoPhase == runtimePhase, "Exactly one phase after cycle");
                if (runtimePhase < 5)
                {
                    step = 10;
                    return;
                }
                TomatoFruit fruit = seeds.PrototypeTomato.seed.GetComponentInChildren<TomatoFruit>();
                Require(fruit != null && fruit.GetComponent<Grabbable>() != null &&
                    fruit.GetComponentInChildren<HandGrabInteractable>() != null &&
                    fruit.GetComponentInChildren<GrabInteractable>() != null, "Ripe Meta grab components");
                Require(fruit.GetComponent<Rigidbody>().isKinematic, "Attached fruit stays on plant");
                result += "PASS: startup, planting, five water-gated sky cycles, deferred growth, repeat-press guard and ripe Meta components" + Environment.NewLine;
                step = 1;
                nextStepAt = EditorApplication.timeSinceStartup + 1;
            }
            else if (step == 1)
            {
                TomatoFruit fruit = UnityEngine.Object.FindFirstObjectByType<TomatoFruit>();
                Require(fruit != null && fruit.GetComponent<Rigidbody>().isKinematic, "Fruit remains attached after Meta Start and scene scan");
                Grabbable grab = fruit.GetComponent<Grabbable>();
                Pose pose = new(fruit.transform.position, fruit.transform.rotation);
                grab.ProcessPointerEvent(new PointerEvent(901, PointerEventType.Hover, pose));
                grab.ProcessPointerEvent(new PointerEvent(901, PointerEventType.Select, pose));
                Require(grab.SelectingPointsCount == 1, "Simulated Meta selection");
                step = 2;
                nextStepAt = EditorApplication.timeSinceStartup + 0.5;
            }
            else if (step == 2)
            {
                TomatoFruit fruit = UnityEngine.Object.FindFirstObjectByType<TomatoFruit>();
                Require(fruit.transform.parent == null, "Grab detaches fruit from plant");
                Transform tray = GameObject.Find("Bandeja Cosecha").transform;
                Pose pose = new(tray.position + Vector3.up * 0.06f, Quaternion.identity);
                Grabbable grab = fruit.GetComponent<Grabbable>();
                grab.ProcessPointerEvent(new PointerEvent(901, PointerEventType.Move, pose));
                grab.ProcessPointerEvent(new PointerEvent(901, PointerEventType.Unselect, pose));
                grab.ProcessPointerEvent(new PointerEvent(901, PointerEventType.Unhover, pose));
                step = 3;
                nextStepAt = EditorApplication.timeSinceStartup + 1;
            }
            else if (step == 3)
            {
                TomatoFruit fruit = UnityEngine.Object.FindFirstObjectByType<TomatoFruit>();
                Require(seeds.HarvestComplete && !fruit.GetComponent<Grabbable>().enabled, "Release inside tray records harvest and disables grab");
                panel.RefillCans();
                foreach (WateringCan can in UnityEngine.Object.FindObjectsByType<WateringCan>(FindObjectsSortMode.None))
                    Require(Mathf.Approximately(can.CurrentWaterLitres, can.CapacityLitres), "Can refilled");
                foreach (Text text in UnityEngine.Object.FindFirstObjectByType<TomatoPrototypeGuide>().GetComponentsInChildren<Text>())
                    Require(text.preferredHeight <= text.rectTransform.rect.height + 1, "Guide text fits its panel");
                result += "PASS: simulated Meta grab detaches fruit, release in tray completes harvest, refill and text layout" + Environment.NewLine;
                panel.RestartPrototype();
                step = 4;
                nextStepAt = EditorApplication.timeSinceStartup + 2;
            }
            else
            {
                Require(seeds.Calendar.CurrentDay == 0 && seeds.Calendar.Journal.Count == 0 && seeds.PlantedSeeds.Count == 0, "Restart clears session");
                Require(UnityEngine.Object.FindFirstObjectByType<GardenGrabSetup>() != null, "Meta setup restored after reload");
                Require(UnityEngine.Object.FindFirstObjectByType<WateringCanSetup>() != null, "Can setup restored after reload");
                Require(UnityEngine.Object.FindFirstObjectByType<TomatoPrototypeGuide>() != null, "Guide restored after reload");
                result += "PASS: text layout and scene reload restore day, journal, props and helpers" + Environment.NewLine;
                result += "NOT TESTED: real hand/controller input, water stream aiming, APK, Quest 3 comfort/performance" + Environment.NewLine;
                Finish();
            }
        }
        catch (Exception exception)
        {
            result += "FAIL: " + exception + Environment.NewLine;
            Finish();
        }
    }

    private static void Finish()
    {
        Directory.CreateDirectory("Docs/Validation");
        File.WriteAllText(ReportPath, result);
        EditorApplication.update -= Tick;
        EditorApplication.isPlaying = false;
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new Exception("Tomato runtime verification: " + label);
    }
}

