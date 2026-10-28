using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Oculus.Interaction;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Play integration checks with synthetic release samples and pointer events; no scene saves.</summary>
[InitializeOnLoad]
public static class GardenMenuVerification
{
    private const string SessionKey = "CozyGarden.MenuVerification";
    private const string PreferenceKey = "CozyGarden.Preferences.v1";
    private static readonly List<string> checks = new();
    private static IEnumerator routine;
    private static double nextAt;
    private static double startedAt;

    static GardenMenuVerification() => EditorApplication.playModeStateChanged += OnPlayChanged;

    [MenuItem("Garden/Verify menu in Play")]
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
            if (EditorApplication.timeSinceStartup - startedAt > 90) throw new TimeoutException("Menu verification timed out.");
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
        var lines = new List<string> { "Unity " + Application.unityVersion, "UTC " + DateTime.UtcNow.ToString("O"),
            "Synthetic Play checks: camera pose, release decisions and pointer events; physical XR input not tested." };
        lines.AddRange(checks.Select(check => "PASS: " + check));
        lines.Add(error == null ? "RESULT: PASS" : "RESULT: FAIL\n" + error);
        File.WriteAllLines("output/MenuPlayVerification.txt", lines);
        if (error == null) Debug.Log("Menu Play verification passed: " + checks.Count + " checks.");
        else Debug.LogError("Menu Play verification failed: " + error);
        EditorApplication.isPlaying = false;
    }

    private static IEnumerator Run()
    {
        yield return 1.0f;
        var menu = UnityEngine.Object.FindFirstObjectByType<GardenMenuController>();
        var seeds = UnityEngine.Object.FindFirstObjectByType<SeedsController>();
        var calendar = seeds.Calendar;
        var tutorial = UnityEngine.Object.FindFirstObjectByType<GronnyTutorial>();
        Check(menu != null && !GardenMenuController.HasStarted && GardenMenuController.GardenSuspended,
            "Initial menu suspends the garden before Play");
        Check(tutorial.GetComponent<GronnyVoicePlayer>() == null,
            "Cultivation voice waits for Play");
        var menuVoice = (GronnyVoicePlayer)Field(menu, "menuVoice");
        Check(menuVoice != null && menuVoice.CurrentCueId == null, "Launch menu does not greet before Play");
        Check(calendar.Journal.Count == 0 && seeds.PlantedSeeds.Count == 0, "Opening menu does not create crop progress");

        // No XR device is required for these fixtures. This pose exists only during Play.
        var rig = UnityEngine.Object.FindFirstObjectByType<OVRCameraRig>();
        rig.enabled = false;
        rig.centerEyeAnchor.position = new Vector3(0f, 1.6f, -7.88f);
        menu.Open();
        Arm(menu);
        yield return 0.2f;
        var view = (GardenMenuView)Field(menu, "menu");
        var launcher = (GardenMenuView)Field(menu, "launcher");
        Pose station = new(view.Root.transform.position, view.Root.transform.rotation);
        Check(view.Root.GetComponent<Canvas>().enabled && view.Collider.enabled && view.Collider.isTrigger,
            "Launch menu remains visible and a non-solid ray target");
        var guidePanel = UnityEngine.Object.FindFirstObjectByType<TomatoPrototypeGuide>().GetComponent<RectTransform>();
        Check(Vector3.Distance(rig.transform.InverseTransformPoint(station.position), new Vector3(0f, 1.45f, 1.3f)) < 0.01f,
            "Launch menu appears in front of the player's entry point");
        Check(!launcher.Root.activeInHierarchy, "Pause menu button is hidden until Play");
        Check(Physics.OverlapBox(station.position, new Vector3(0.43f, 0.355f, 0.01f), station.rotation,
            ~0, QueryTriggerInteraction.Ignore).All(hit => hit.transform.IsChildOf(rig.transform)),
            "Launch panel occupies clear space in Garden_Moves");
        Check(view.Root.GetComponent<PointableCanvas>().Canvas == view.Root.GetComponent<Canvas>() &&
            view.Root.GetComponent<RayInteractable>().Surface != null, "Runtime Meta canvas and ray dependencies are bound");
        Check(GardenMenuController.RayHitsMenu(new Ray(rig.centerEyeAnchor.position,
            view.Root.transform.position - rig.centerEyeAnchor.position), 10f), "Menu target consumes a ray before calendar pinches");
        Vector3 initialRigPosition = rig.transform.position;
        Quaternion initialRigRotation = rig.transform.rotation;
        rig.transform.SetPositionAndRotation(initialRigPosition + new Vector3(3f, 0f, 2f), Quaternion.Euler(0f, 150f, 0f));
        rig.centerEyeAnchor.position += Vector3.up * 0.4f;
        menu.Open();
        yield return 0.2f;
        CheckPose(view, station, "Launch menu stays fixed at entry when the player moves or turns");
        rig.transform.SetPositionAndRotation(initialRigPosition, initialRigRotation);
        Vector3 previewEyePosition = guidePanel.position + guidePanel.up * 0.4f - guidePanel.forward * 1.4f;
        rig.centerEyeAnchor.SetPositionAndRotation(new Vector3(0f, 1.6f, -7.88f), Quaternion.identity);
        CheckText(view);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("output/MenuLaunchHome.png"));
        yield return 0.2f;
        Click(view, "Controls");
        CheckText(view);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("output/MenuValidatedControls.png"));
        yield return 0.2f;
        Click(view, "Back");
        Click(view, "Sound settings");
        CheckText(view);
        CheckPreferences(calendar);
        CheckPose(view, station, "Changing menu pages preserves the station position and orientation");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("output/MenuValidatedSound.png"));
        yield return 0.2f;
        Click(view, "Back");
        var tank = UnityEngine.Object.FindFirstObjectByType<GardenWaterTankDisplay>();
        Pose tankStation = new(tank.transform.position, tank.transform.rotation);
        Click(view, "Play");
        Check(GardenMenuController.HasStarted && GardenMenuController.GardenSuspended,
            "Play waits for selection release before starting garden actions");
        Check(tutorial.GetComponent<GronnyVoicePlayer>() == null && menuVoice.CurrentCueId == null,
            "Greeting waits for the Play selection to be released");
        Check(Vector3.Distance(view.Root.transform.position, station.position) > 1f,
            "First Play relocates the menu from entry to Gronny");
        station = new Pose(view.Root.transform.position, view.Root.transform.rotation);
        Pose launcherStation = new(launcher.Root.transform.position, launcher.Root.transform.rotation);
        CheckAboveGuide(view, guidePanel, "Pause panel sits 8 cm above the guide without covering its instructions");
        CheckPose(launcher, tankStation, "Pause menu button takes the tank's former station beside the guide");
        Vector3 guideTop = guidePanel.TransformPoint(new Vector3(guidePanel.rect.center.x, guidePanel.rect.yMax, 0f));
        Vector3 formerLauncherPosition = guideTop + guidePanel.up *
            (0.08f + launcher.Collider.size.y * launcher.Root.transform.localScale.y * 0.5f);
        Check(Vector3.Distance(tank.transform.position, formerLauncherPosition) < 0.001f &&
            Quaternion.Angle(tank.transform.rotation, guidePanel.rotation) < 0.01f,
            "Tank takes the Menu button's former station above the guide");
        Check(Physics.OverlapBox(station.position, new Vector3(0.43f, 0.355f, 0.01f), station.rotation,
            ~0, QueryTriggerInteraction.Ignore).All(hit => hit.transform.IsChildOf(rig.transform)),
            "Pause panel occupies clear space beside Gronny");
        Sample(menu, false, 0f);
        Sample(menu, false, 5f);
        Check(GardenMenuController.GardenSuspended, "Holding an input cannot resume the garden");
        Sample(menu, true, 5.1f);
        Sample(menu, true, 5.2f);
        Check(GardenMenuController.GardenSuspended, "Brief release cannot resume the garden");
        Sample(menu, true, 5.4f);
        Check(!GardenMenuController.GardenSuspended, "Stable release resumes through the same production decision");
        CheckPose(launcher, launcherStation, "Closed menu leaves its Menu button fixed beside the guide");
        rig.centerEyeAnchor.SetPositionAndRotation(previewEyePosition, guidePanel.rotation);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("output/MenuAboveGuideClosed.png"));
        yield return 0.2f;
        rig.centerEyeAnchor.SetPositionAndRotation(new Vector3(3f, 1.2f, -4f), Quaternion.Euler(0f, 180f, 0f));
        yield return 0.7f;
        CheckPose(launcher, launcherStation, "Menu button stays fixed after moving and looking away");
        var voice = tutorial.GetComponent<GronnyVoicePlayer>();
        var audio = voice.GetComponent<AudioSource>();
        Check(voice != null && voice.CurrentCueId == "01_welcome",
            "Gronny greets and invites the player to the table after Play");
        Check(voice.CurrentCueId == "01_welcome" &&
            Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial").Find("01_welcome").transcript.Contains("limited water supply"),
            "The greeting itself explains the limited water reserve");
        var learned = (HashSet<string>)Field(tutorial, "learned");
        int learnedCount = learned.Count;
        int journalCount = calendar.Journal.Count;
        AudioClip currentClip = audio.clip;
        Click(launcher, "Menu");
        CheckPose(view, station, "Opening pause from another player position keeps the station beside Gronny");
        Check(view.Content.GetComponentsInChildren<Button>().Any(button => button.name == "Return to garden") &&
            !view.Content.GetComponentsInChildren<Button>().Any(button => button.name == "Play"),
            "Pause menu offers Return to garden instead of starting a new game");
        rig.centerEyeAnchor.SetPositionAndRotation(previewEyePosition, guidePanel.rotation);
        float pausedVoiceTime = audio.time;
        yield return 0.6f;
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("output/MenuPauseHome.png"));
        yield return 0.2f;
        Click(view, "Controls");
        CheckText(view);
        Click(view, "Movement");
        yield return 0.5f;
        CheckText(view);
        Check(menuVoice.CurrentCueId == "controls_move_hands" && !audio.isPlaying,
            "Menu plays the recorded hands demonstration while the tutorial voice stays paused");
        var demo = (GardenControlDemonstration)Field(menu, "menuDemonstration");
        Check(Field(demo, "model") != null, "Review shows its independent hand model");
        Check(Mathf.Approximately(menuVoice.GetComponent<AudioSource>().pitch, 1f),
            "Hand instructions preserve the original voice pitch");
        var handTimes = (float[])Field(demo, "handStepTimes");
        Check(handTimes != null && handTimes.Length > 1 &&
            handTimes.Zip(handTimes.Skip(1), (a, b) => b - a >= 2.49f).All(held => held),
            "Hand demonstration gives each step at least 2.5 seconds without slowing the voice");
        Click(view, "Preview right hand");
        yield return 0.5f;
        CheckText(view);
        Check(menuVoice.CurrentCueId == "controls_move_hands_right" &&
            view.Content.GetComponentsInChildren<Text>().Any(text => text.text.Contains("still use the left side")),
            "Right-hand preview uses its own voice and clearly identifies the current live controls");
        Click(view, "Stop");
        Check(menuVoice.CurrentCueId == null && Field(demo, "anchor") == null, "Stop clears both preview voice and model");
        Click(view, "Replay");
        yield return 0.5f;
        Check(menuVoice.CurrentCueId == "controls_move_hands_right", "Replay works without recording tutorial learning");
        Click(view, "Back");
        Click(view, "Controllers");
        Click(view, "Open and close calendar");
        Click(view, "Preview right hand");
        yield return 0.5f;
        Check(menuVoice.CurrentCueId == "controls_calendar_controllers_right" &&
            ((GameObject)Field(demo, "model")).transform.Find("right_oculus_controller_world") != null,
            "B-button preview pairs the right controller model and recording");
        Click(view, "Back");
        Click(view, "Back");
        Click(view, "Sound settings");
        CheckText(view);
        Click(view, "Test voice");
        yield return 0.5f;
        Check(menuVoice.CurrentCueId == "settings_voice_sample" && !audio.isPlaying,
            "Voice sample plays during pause without resuming the tutorial");
        Click(view, "Stop voice");
        Check(menuVoice.CurrentCueId == null, "Voice sample can be stopped");
        Click(view, "Back");
        CheckPose(view, station, "Pause controls and sound settings keep the menu beside Gronny");
        Check(!audio.isPlaying && Mathf.Abs(audio.time - pausedVoiceTime) < 0.02f,
            "Opening menu pauses the existing voice without losing its position");
        Check(learned.Count == learnedCount && calendar.Journal.Count == journalCount,
            "Menu navigation records neither learning nor original tutorial assistance");
        Close(menu, view);
        CheckPose(view, station, "Returning from pause does not move the menu back to entry");
        yield return 0.3f;
        Check(audio.clip == currentClip && audio.isPlaying && audio.time > pausedVoiceTime,
            "Returning resumes the same voice rather than replaying the welcome");

        // Isolate garden suspension from progressive teaching for the following cultivation fixtures.
        tutorial.enabled = false;
        var can = UnityEngine.Object.FindFirstObjectByType<WateringCan>();
        var grab = can.GetComponent<Grabbable>();
        can.transform.position = rig.centerEyeAnchor.position + Vector3.right * 2f;
        can.transform.rotation = Quaternion.identity;
        Pose pose = new(can.transform.position, can.transform.rotation);
        grab.ProcessPointerEvent(new PointerEvent(9871, PointerEventType.Hover, pose));
        grab.ProcessPointerEvent(new PointerEvent(9871, PointerEventType.Select, pose));
        yield return 0.15f;
        can.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
        yield return 0.2f;
        Check(can.IsHeld && can.IsPouring, "Synthetic held can pours before menu suspension");
        menu.Open();
        float waterBefore = can.CurrentWaterLitres;
        yield return 0.4f;
        Check(can.IsHeld && !can.IsPouring && Mathf.Approximately(can.CurrentWaterLitres, waterBefore),
            "Menu preserves an existing grab and stops water consumption");
        UnityEngine.Object.FindFirstObjectByType<CalendarPanel>(FindObjectsInactive.Include).RefillCans();
        Check(Mathf.Approximately(can.CurrentWaterLitres, waterBefore), "Calendar refill is blocked while menu is open");
        Close(menu, view);
        yield return 0.3f;
        Check(!can.IsPouring && Mathf.Approximately(can.CurrentWaterLitres, waterBefore),
            "Returning with a tilted can cannot resume pouring");
        can.transform.rotation = Quaternion.identity;
        yield return 0.15f;
        can.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
        yield return 0.2f;
        Check(can.IsPouring && can.CurrentWaterLitres < waterBefore, "Upright then tilted rearms intentional pouring");
        can.transform.rotation = Quaternion.identity;
        typeof(WateringCan).GetField("_currentWaterLitres", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(can, 0f);
        yield return 0.15f;
        Check(GardenWaterSupply.Active.IsPractice && !GardenWaterSupply.Active.Exhausted && can.CurrentWaterLitres > 0f &&
            GardenWaterSupply.Active.ReserveMillilitres == 0f, "Practice automatically refills a held empty can without consuming a limited budget");
        grab.ProcessPointerEvent(new PointerEvent(9871, PointerEventType.Unselect, pose));
        grab.ProcessPointerEvent(new PointerEvent(9871, PointerEventType.Unhover, pose));

        GardenPot pot = UnityEngine.Object.FindObjectsByType<GardenPot>(FindObjectsSortMode.None).First(p => p.AcceptsTomato);
        SeedItem seed = UnityEngine.Object.FindObjectsByType<SeedItem>(FindObjectsSortMode.None).First(s => s.Crop == SeedCrop.Tomato);
        seed.transform.position = pot.PlantingPoint;
        Check(seeds.TryPlant(seed), "Cultivation fixture plants a tomato after leaving the menu");
        var plant = seeds.PrototypeTomato;
        seeds.ReceiveWater(pot, seeds.TargetMillilitres(plant) * 1.01f / 1000f);
        seeds.GetState(plant).lastWaterAt = Time.unscaledTime - 1f;
        int advances = 0;
        calendar.PhaseAdvanced += () => advances++;
        calendar.AdvancePhase();
        Check(calendar.IsAdvancing, "Real phase cycle begins before the suspension fixture");
        yield return 0.8f;
        menu.Open();
        int day = calendar.CurrentDay;
        float sunlight = RenderSettings.sun.intensity;
        Quaternion sunRotation = RenderSettings.sun.transform.rotation;
        float retainedWater = seeds.GetState(plant).retainedWaterMillilitres;
        yield return 1.0f;
        Check(calendar.IsAdvancing && calendar.CurrentDay == day && advances == 0 &&
            Mathf.Approximately(RenderSettings.sun.intensity, sunlight) &&
            Quaternion.Angle(RenderSettings.sun.transform.rotation, sunRotation) < 0.01f,
            "Opening menu freezes phase lighting and prevents committing the day");
        seeds.ReceiveWater(pot, 1f);
        calendar.AdvancePhase();
        Check(Mathf.Approximately(seeds.GetState(plant).retainedWaterMillilitres, retainedWater),
            "Menu blocks additional crop watering and phase requests");
        Close(menu, view);
        float deadline = Time.unscaledTime + 20f;
        while (calendar.IsAdvancing && Time.unscaledTime < deadline) yield return 0.2f;
        Check(!calendar.IsAdvancing && calendar.CurrentDay > day && advances == 1,
            "Returning completes the interrupted phase exactly once");
        var waterChecks = VerifyWaterSupply(seeds, calendar, can);
        try { while (waterChecks.MoveNext()) yield return waterChecks.Current; }
        finally { (waterChecks as IDisposable)?.Dispose(); }
    }

    private static IEnumerator VerifyWaterSupply(SeedsController seeds, CalendarSystem calendar, WateringCan can)
    {
        GardenWaterSupply supply = GardenWaterSupply.Active;
        Check(supply != null && !supply.IsPractice && supply.Attempt == 1,
            "First completed care phase switches from free practice to a finite reserve");
        float remaining = seeds.RemainingTomatoWaterMillilitres();
        float available = supply.ReserveMillilitres + supply.WaterInCansMillilitres;
        Check(Mathf.Abs(available - remaining * 2f) < 0.05f,
            "Finite supply includes a 100 percent margin for all remaining stages");
        Check(Mathf.Approximately(can.CapacityLitres, 0.24f) &&
            Mathf.Approximately((float)Field(can, "litresPerSecond"), 0.01f),
            "A full can lasts 24 seconds at the slower beginner watering rate");
        float reserve = supply.ReserveMillilitres;
        can.Refill();
        Check(Mathf.Abs(supply.ReserveMillilitres - reserve) < 0.01f, "Refilling a full can cannot debit or create water");
        GameObject secondObject = new("Second can budget fixture");
        WateringCan second = secondObject.AddComponent<WateringCan>();
        try
        {
            yield return 0.2f;
            Check(second.CurrentWaterLitres == 0f && Mathf.Abs(supply.ReserveMillilitres + supply.WaterInCansMillilitres - available) < 0.01f,
                "A newly spawned can starts empty in the limited lesson instead of creating free water");
            second.Refill();
            Check(second.CurrentWaterLitres > 0f && supply.ReserveMillilitres < reserve &&
                Mathf.Abs(supply.ReserveMillilitres + supply.WaterInCansMillilitres - available) < 0.01f,
                "Direct can refill transfers water from the shared reserve without changing the available total");

            var planted = seeds.PrototypeTomato;
            var saved = seeds.GetState(planted);
            var grab = can.GetComponent<Grabbable>();
            Transform spout = (Transform)Field(can, "spout");
            can.transform.rotation = Quaternion.identity;
            Pose pose = new(can.transform.position, can.transform.rotation);
            grab.ProcessPointerEvent(new PointerEvent(9891, PointerEventType.Hover, pose));
            grab.ProcessPointerEvent(new PointerEvent(9891, PointerEventType.Select, pose));
            yield return 0.15f;
            float pouredBefore = supply.PouredMillilitres;
            float usedBefore = supply.UsefulMillilitres;
            float appliedBefore = saved.waterMillilitres;
            float canBefore = can.CurrentWaterLitres;
            can.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
            spout.position = planted.pot.PlantingPoint + Vector3.right * 0.025f + Vector3.up * 0.15f;
            Physics.SyncTransforms();
            yield return 0.25f;
            can.transform.rotation = Quaternion.identity;
            yield return 0.05f;
            Check(supply.UsefulMillilitres > usedBefore && saved.waterMillilitres > appliedBefore,
                "A real held tilted-can soil cast records useful water and waters the planted tomato");
            Check(Mathf.Abs((canBefore - can.CurrentWaterLitres) * 1000f - (supply.PouredMillilitres - pouredBefore)) < 0.05f,
                "Measured discharge equals the actual decrease in can water");
            float missedBefore = supply.MissedMillilitres;
            appliedBefore = saved.waterMillilitres;
            can.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
            spout.position = new Vector3(20f, 2f, 20f);
            Physics.SyncTransforms();
            yield return 0.25f;
            can.transform.rotation = Quaternion.identity;
            yield return 0.05f;
            Check(supply.MissedMillilitres > missedBefore && Mathf.Approximately(saved.waterMillilitres, appliedBefore),
                "Pouring away from pots records missed water without crediting the plant");
            float target = seeds.TargetMillilitres(planted);
            seeds.ReceiveWater(planted.pot, Mathf.Max(0f, target - saved.retainedWaterMillilitres - 1f) / 1000f);
            float excessBefore = supply.ExcessMillilitres;
            usedBefore = supply.UsefulMillilitres;
            float crossingBefore = supply.PouredMillilitres;
            can.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
            spout.position = planted.pot.PlantingPoint + Vector3.right * 0.025f + Vector3.up * 0.15f;
            Physics.SyncTransforms();
            yield return 0.25f;
            can.transform.rotation = Quaternion.identity;
            grab.ProcessPointerEvent(new PointerEvent(9891, PointerEventType.Unselect, pose));
            grab.ProcessPointerEvent(new PointerEvent(9891, PointerEventType.Unhover, pose));
            yield return 0.05f;
            Check(supply.ExcessMillilitres > excessBefore && supply.UsefulMillilitres - usedBefore <= 1.05f,
                "Pour crossing the target splits useful water from surplus in the same stream"
                + " (poured=" + (supply.PouredMillilitres - crossingBefore) + ", useful=" + (supply.UsefulMillilitres - usedBefore)
                + ", surplus=" + (supply.ExcessMillilitres - excessBefore) + ", retained=" + saved.retainedWaterMillilitres + ", target=" + target + ")");
            Check(Mathf.Abs(supply.PouredMillilitres - supply.UsefulMillilitres - supply.MissedMillilitres - supply.ExcessMillilitres) < 0.05f &&
                !supply.HasNoMeasuredWaste, "Water balance closes and wasted water cannot earn the no-waste narration");

            // Prescribed empty-can samples isolate refill/exhaustion boundaries from XR tracking.
            var panel = UnityEngine.Object.FindFirstObjectByType<CalendarPanel>(FindObjectsInactive.Include);
            int rounds = 0;
            while (supply.ReserveMillilitres + supply.WaterInCansMillilitres > 0.001f && rounds++ < 20)
            {
                foreach (WateringCan item in UnityEngine.Object.FindObjectsByType<WateringCan>(FindObjectsSortMode.None).Where(item => item.isActiveAndEnabled))
                {
                    supply.RecordPour(item.CurrentWaterLitres, 0f, false);
                    typeof(WateringCan).GetField("_currentWaterLitres", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(item, 0f);
                }
                float beforeTransfer = supply.ReserveMillilitres + supply.WaterInCansMillilitres;
                panel.RefillCans();
                Check(Mathf.Abs(supply.ReserveMillilitres + supply.WaterInCansMillilitres - beforeTransfer) < 0.05f,
                    "Calendar refill also preserves the shared finite budget: sample " + rounds);
                if (supply.ReserveEmpty && supply.WaterInCansMillilitres >= seeds.RemainingTomatoWaterMillilitres())
                {
                    supply.EvaluateExhaustion();
                    Check(!supply.Exhausted && supply.StatusText.Contains("RESERVE EMPTY"),
                        "Reserve reaching zero produces an explicit warning while water in cans remains usable");
                }
            }
            int day = calendar.CurrentDay;
            int phase = saved.tomatoPhase;
            float retained = saved.retainedWaterMillilitres;

            // A completed final target remains playable with no water left.
            saved.tomatoPhase = 4;
            saved.retainedWaterMillilitres = seeds.TargetMillilitres(planted);
            supply.EvaluateExhaustion();
            Check(!supply.Exhausted && seeds.RemainingTomatoWaterMillilitres() == 0f &&
                GronnyTutorial.Allows(GardenTutorialAction.Advance),
                "Meeting the last target with no water left does not trigger an unfair retry");
            saved.harvested = true;
            float finalPoured = supply.PouredMillilitres;
            second.Refill();
            supply.RecordPour(0.01f, 0f, false);
            Check(supply.Completed && second.CurrentWaterLitres == 0f && supply.ReserveMillilitres == 0f &&
                supply.PouredMillilitres == finalPoured,
                "After harvest, refills and outflow cannot change the completed balance");
            saved.harvested = false;
            saved.tomatoPhase = phase;
            saved.retainedWaterMillilitres = retained;

            float needed = seeds.RemainingTomatoWaterMillilitres();
            float firstCanWater = Mathf.Min(needed, can.CapacityLitres * 1000f);
            typeof(WateringCan).GetField("_currentWaterLitres", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(can, firstCanWater / 1000f);
            typeof(WateringCan).GetField("_currentWaterLitres", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(second, (needed - firstCanWater) / 1000f);
            supply.EvaluateExhaustion();
            Check(!supply.Exhausted, "Exactly enough water for all remaining targets keeps the attempt playable");
            typeof(WateringCan).GetField("_currentWaterLitres", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(can, (firstCanWater - 1f) / 1000f);
            supply.EvaluateExhaustion();
            Check(supply.Exhausted && supply.AvailableMillilitres > 0f &&
                supply.FailureMessage.Contains("Not enough water"),
                "Insufficient water triggers a clear failure before the player empties every can");
            Check(supply.Exhausted && !GronnyTutorial.Allows(GardenTutorialAction.Pour) &&
                !GronnyTutorial.Allows(GardenTutorialAction.Advance) && GronnyTutorial.Allows(GardenTutorialAction.OpenCalendar),
                "Exhaustion pauses watering and growth but leaves the recovery UI accessible");
            can.Refill();
            calendar.AdvancePhase();
            Check(Mathf.Abs(can.CurrentWaterLitres * 1000f - (firstCanWater - 1f)) < 0.01f &&
                calendar.CurrentDay == day && !calendar.IsAdvancing,
                "Empty-reserve direct refill and advance cannot bypass exhaustion");
            yield return 0.4f;
            var menuController = UnityEngine.Object.FindFirstObjectByType<GardenMenuController>();
            var gameOverView = (GardenMenuView)Field(menuController, "gameOver");
            var gameOverVoice = (GronnyVoicePlayer)Field(menuController, "menuVoice");
            Check(GardenMenuController.GardenSuspended && gameOverView.Root.activeInHierarchy &&
                gameOverView.Root.GetComponent<Canvas>().enabled && gameOverView.Collider.enabled,
                "Insufficient supply automatically opens a visible interactive game-over panel without needing the tutorial");
            Check(gameOverView.Content.GetComponentsInChildren<Text>().Any(text => text.text == "Game over") &&
                gameOverView.Content.GetComponentsInChildren<Button>().Any(button => button.name == "Try again"),
                "Game-over panel clearly names the outcome and offers Try again");
            CheckText(gameOverView);
            Check(gameOverVoice.CurrentCueId == "water_retry" &&
                gameOverView.Content.GetComponentsInChildren<Text>().Any(text => text.text == supply.FailureMessage),
                "Game over explains the insufficient supply and announces Try again while cultivation is paused");
            var guide = UnityEngine.Object.FindFirstObjectByType<TomatoPrototypeGuide>();
            Check(((Text)Field(guide, "water")).text.Contains("Try again") &&
                !((Text)Field(guide, "water")).text.Contains("Return to garden"),
                "Guide board agrees with the game-over panel rather than offering a missing Return button");
            OVRCameraRig rig = UnityEngine.Object.FindFirstObjectByType<OVRCameraRig>();
            Check(GardenMenuController.RayHitsMenu(new Ray(rig.centerEyeAnchor.position,
                gameOverView.Root.transform.position - rig.centerEyeAnchor.position), 10f),
                "Meta rays can select the game-over panel in front of the player");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("output/WaterGameOver.png"));
            yield return 0.2f;
            float masterVolume = GardenPreferences.MasterVolume;
            float voiceVolume = GardenPreferences.VoiceVolume;
            bool muted = GardenPreferences.Muted;
            Arm(menuController);
            Click(gameOverView, "Try again");
            Check(!supply.Retry(), "Repeated retry clicks cannot queue multiple scene reloads");
            float reloadDeadline = Time.realtimeSinceStartup + 10f;
            GardenMenuController restartedMenu = null;
            while (Time.realtimeSinceStartup < reloadDeadline)
            {
                restartedMenu = UnityEngine.Object.FindFirstObjectByType<GardenMenuController>();
                if (restartedMenu != null && restartedMenu != menuController && GardenMenuController.HasStarted) break;
                yield return 0.1f;
            }
            Check(restartedMenu != null && menuController == null && seeds == null && gameOverVoice == null,
                "Try again unloads the previous crop, menu and voice player");
            var freshSeeds = UnityEngine.Object.FindFirstObjectByType<SeedsController>();
            var freshSupply = GardenWaterSupply.Active;
            var freshTutorial = UnityEngine.Object.FindFirstObjectByType<GronnyTutorial>();
            Check(GardenMenuController.StartsAtPlanting && GardenMenuController.HasStarted &&
                !((GardenMenuView)Field(restartedMenu, "gameOver")).Root.activeInHierarchy,
                "Retry starts the planting session directly without another Play selection");
            Check(freshSeeds.Calendar.CurrentDay == 0 && freshSeeds.PlantedSeeds.Count == 0 &&
                freshSeeds.Calendar.Journal.Count == 0 && freshSupply.IsPractice && freshSupply.Attempt == 0 &&
                freshSupply.TotalUsefulMillilitres == 0f && freshSupply.TotalWastedMillilitres == 0f && !freshSupply.Retry(),
                "Retry resets crop, day, journal and all water counters to the planting session");
            Check(GardenPreferences.MasterVolume == masterVolume && GardenPreferences.VoiceVolume == voiceVolume &&
                GardenPreferences.Muted == muted, "Retry preserves the player's sound settings");
            Sample(restartedMenu, false, Time.unscaledTime);
            Check(GardenMenuController.GardenSuspended && freshTutorial.GetComponent<GronnyVoicePlayer>() == null,
                "The planting restart waits for the retry selection to be released before speaking");
            Sample(restartedMenu, true, Time.unscaledTime);
            Sample(restartedMenu, true, Time.unscaledTime + 0.21f);
            yield return 0.4f;
            var freshVoice = freshTutorial.GetComponent<GronnyVoicePlayer>();
            var freshLearned = (HashSet<string>)Field(freshTutorial, "learned");
            Check(freshVoice != null && freshVoice.CurrentCueId == "03_plant_seed" &&
                freshLearned.SetEquals(new[] { "move", "grab" }) &&
                GronnyTutorial.Allows(GardenTutorialAction.Plant),
                "Gronny restarts at the planting dialogue with planting unlocked and later lessons reset");
        }
        finally { UnityEngine.Object.Destroy(secondObject); }
    }

    private static void CheckPose(GardenMenuView view, Pose expected, string description) =>
        Check(Vector3.Distance(view.Root.transform.position, expected.position) < 0.001f &&
            Quaternion.Angle(view.Root.transform.rotation, expected.rotation) < 0.01f, description);

    private static void CheckAboveGuide(GardenMenuView view, RectTransform guide, string description)
    {
        var corners = new Vector3[4];
        ((RectTransform)view.Root.transform).GetWorldCorners(corners);
        Vector3 bottom = (corners[0] + corners[3]) * 0.5f;
        Vector3 top = guide.TransformPoint(new Vector3(guide.rect.center.x, guide.rect.yMax, 0f));
        Check(Vector3.Distance(bottom, top + guide.up * 0.08f) < 0.001f &&
            Quaternion.Angle(view.Root.transform.rotation, guide.rotation) < 0.01f, description);
    }

    private static void CheckPreferences(CalendarSystem calendar)
    {
        bool existed = PlayerPrefs.HasKey(PreferenceKey);
        string original = PlayerPrefs.GetString(PreferenceKey);
        float listener = AudioListener.volume;
        int journal = calendar.Journal.Count;
        try
        {
            GardenPreferences.SetMasterVolume(0.4f);
            GardenPreferences.SetVoiceVolume(0.3f);
            GardenPreferences.SetBrightness(1.2f);
            GardenPreferences.SetMainHand(GardenHand.Left);
            GardenPreferences.RestoreSoundDefaults();
            Check(Mathf.Approximately(GardenPreferences.Brightness, 1.2f) && GardenPreferences.MainHand == GardenHand.Left,
                "Sound defaults preserve independent image and handedness preferences");
            GardenPreferences.SetMasterVolume(0.4f);
            GardenPreferences.SetVoiceVolume(0.3f);
            GardenPreferences.SetMuted(true);
            typeof(GardenPreferences).GetField("values", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
            GardenPreferences.Load();
            Check(Mathf.Approximately(GardenPreferences.MasterVolume, 0.4f) &&
                Mathf.Approximately(GardenPreferences.VoiceVolume, 0.3f) && GardenPreferences.Muted && AudioListener.volume == 0f &&
                calendar.Journal.Count == journal, "Preferences survive an in-process reload and never modify crop history");
        }
        finally
        {
            if (existed) PlayerPrefs.SetString(PreferenceKey, original); else PlayerPrefs.DeleteKey(PreferenceKey);
            PlayerPrefs.Save();
            typeof(GardenPreferences).GetField("values", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
            GardenPreferences.Load();
            AudioListener.volume = listener;
        }
    }

    private static void CheckText(GardenMenuView view)
    {
        Check(view.Content.GetComponentsInChildren<Text>().All(t => t.preferredHeight <= t.rectTransform.rect.height + 2f),
            "Text fits its rectangles on page " + view.Content.GetComponentInChildren<Text>().text);
    }

    private static void Close(GardenMenuController menu, GardenMenuView view)
    {
        Arm(menu);
        Click(view, "Return to garden");
        Sample(menu, true, Time.unscaledTime);
        Sample(menu, true, Time.unscaledTime + 0.21f);
    }

    private static void Arm(GardenMenuController menu)
    {
        Sample(menu, false, Time.unscaledTime);
        Sample(menu, true, Time.unscaledTime);
        Sample(menu, true, Time.unscaledTime + 0.16f);
    }

    private static void Sample(GardenMenuController menu, bool released, float now) =>
        typeof(GardenMenuController).GetMethod("HandleInputRelease", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(menu, new object[] { released, now });
    private static object Field(object target, string name) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Click(GardenMenuView view, string name) =>
        view.Content.GetComponentsInChildren<Button>().Single(b => b.name == name).onClick.Invoke();
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        checks.Add(name);
    }
}
