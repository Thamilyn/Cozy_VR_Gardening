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
    private static bool harvestOnly;

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
    public static void Begin() => BeginCheck(false);

    [MenuItem("Garden/Verify ripe tomato harvest")]
    public static void BeginHarvestCheck() => BeginCheck(true);

    private static void BeginCheck(bool onlyHarvest)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Run this check outside Play.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Garden_Moves")
            throw new Exception("Open Garden_Moves before running this check.");
        SessionState.SetBool(SessionKey, true);
        SessionState.SetBool(SessionKey + ".HarvestOnly", onlyHarvest);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(SessionKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            harvestOnly = SessionState.GetBool(SessionKey + ".HarvestOnly", false);
            step = harvestOnly ? 100 : 0;
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
            if (step == 100)
            {
                // Isolate harvesting from locomotion lessons, watering and disabled scene
                // setup helpers. These changes exist only during this Play session.
                foreach (GronnyTutorial tutorial in UnityEngine.Object.FindObjectsByType<GronnyTutorial>(FindObjectsSortMode.None))
                    tutorial.enabled = false;
                GardenPot pot = Array.Find(UnityEngine.Object.FindObjectsByType<GardenPot>(FindObjectsSortMode.None),
                    p => p.AcceptsTomato && p.GetComponent<TomatoPotStages>() != null);
                SeedItem seed = Array.Find(UnityEngine.Object.FindObjectsByType<SeedItem>(FindObjectsSortMode.None),
                    s => s.Crop == SeedCrop.Tomato && !s.IsPlanted);
                Require(pot != null && seed != null, "Seed and pot for focused harvest check");
                seed.transform.position = pot.PlantingPoint;
                Require(seeds.TryPlant(seed), "Planting for focused harvest check");
                var plant = seeds.PrototypeTomato;
                plant.stage = GrowthStage.Mature;
                seeds.GetState(plant).tomatoPhase = 5;
                seeds.GetState(plant).stage = GrowthStage.Mature.ToString();
                seed.ShowStage(GrowthStage.Mature);
                Require(seed.StageVisual.GetComponentInChildren<TomatoFruit>() != null, "Final model yields a harvestable fruit");
                step = 1;
                nextStepAt = EditorApplication.timeSinceStartup + 1;
            }
            else if (step == 0)
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
                TomatoFruit fruit = seeds.PrototypeTomato.seed.StageVisual.GetComponentInChildren<TomatoFruit>();
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
                VerifyHarvestFruit(fruit);
                VerifyFruitDiameter(fruit, "attached");
                Grabbable grab = fruit.GetComponent<Grabbable>();
                Pose pose = new(fruit.transform.position, fruit.transform.rotation);
                grab.ProcessPointerEvent(new PointerEvent(901, PointerEventType.Hover, pose));
                grab.ProcessPointerEvent(new PointerEvent(901, PointerEventType.Select, pose));
                Require(grab.SelectingPointsCount == 1, "Simulated Meta selection");
                Require(fruit.transform.parent == null, "Selection detaches before the grab transform begins");
                VerifyFruitDiameter(fruit, "just selected");
                Require(!HarvestOutline(fruit).enabled,
                    "Harvest outline disappears on selection");
                step = 2;
                nextStepAt = EditorApplication.timeSinceStartup + 0.5;
            }
            else if (step == 2)
            {
                TomatoFruit fruit = UnityEngine.Object.FindFirstObjectByType<TomatoFruit>();
                Require(fruit.transform.parent == null, "Grab detaches fruit from plant");
                Pose pose = new(fruit.transform.position + Vector3.up * 0.12f, fruit.transform.rotation);
                Grabbable grab = fruit.GetComponent<Grabbable>();
                grab.ProcessPointerEvent(new PointerEvent(901, PointerEventType.Move, pose));
                VerifyFruitDiameter(fruit, "held after Move");
                grab.ProcessPointerEvent(new PointerEvent(901, PointerEventType.Unselect, pose));
                VerifyFruitDiameter(fruit, "released outside tray");
                grab.ProcessPointerEvent(new PointerEvent(901, PointerEventType.Unhover, pose));
                step = 20;
                nextStepAt = EditorApplication.timeSinceStartup + 0.1;
            }
            else if (step == 20)
            {
                TomatoFruit fruit = UnityEngine.Object.FindFirstObjectByType<TomatoFruit>();
                Require(!seeds.HarvestComplete && !fruit.GetComponent<Rigidbody>().isKinematic,
                    "Release outside tray enables physics without recording harvest");
                Grabbable grab = fruit.GetComponent<Grabbable>();
                Pose pose = new(fruit.transform.position, fruit.transform.rotation);
                grab.ProcessPointerEvent(new PointerEvent(901, PointerEventType.Hover, pose));
                grab.ProcessPointerEvent(new PointerEvent(901, PointerEventType.Select, pose));
                Require(grab.SelectingPointsCount == 1, "Released fruit can be picked up again");
                VerifyFruitDiameter(fruit, "picked up again");
                step = 21;
                nextStepAt = EditorApplication.timeSinceStartup + 0.1;
            }
            else if (step == 21)
            {
                TomatoFruit fruit = UnityEngine.Object.FindFirstObjectByType<TomatoFruit>();
                TomatoHarvestBasket tray = UnityEngine.Object.FindFirstObjectByType<TomatoHarvestBasket>();
                BoxCollider volume = new SerializedObject(tray).FindProperty("collectionVolume").objectReferenceValue as BoxCollider;
                Require(volume != null, "Tray collection volume");
                Vector3 deliveryPoint = volume.transform.TransformPoint(volume.center);
                Require(tray.Contains(deliveryPoint), "Simulated delivery targets the current tray volume");
                Pose pose = new(deliveryPoint, Quaternion.identity);
                Grabbable grab = fruit.GetComponent<Grabbable>();
                grab.ProcessPointerEvent(new PointerEvent(901, PointerEventType.Move, pose));
                VerifyFruitDiameter(fruit, "moved to tray");
                Require(tray.Contains(fruit.transform.position), "Held fruit reaches the tray volume");
                // Let the held pose settle so the simulated cross-scene movement
                // does not turn delivery into a high-speed throw on release.
                step = 22;
                nextStepAt = EditorApplication.timeSinceStartup + 0.3;
            }
            else if (step == 22)
            {
                TomatoFruit fruit = UnityEngine.Object.FindFirstObjectByType<TomatoFruit>();
                Grabbable grab = fruit.GetComponent<Grabbable>();
                Pose pose = new(fruit.transform.position, fruit.transform.rotation);
                grab.ProcessPointerEvent(new PointerEvent(901, PointerEventType.Move, pose));
                grab.ProcessPointerEvent(new PointerEvent(901, PointerEventType.Unselect, pose));
                grab.ProcessPointerEvent(new PointerEvent(901, PointerEventType.Unhover, pose));
                step = 3;
                nextStepAt = EditorApplication.timeSinceStartup + 1;
            }
            else if (step == 3)
            {
                TomatoFruit fruit = UnityEngine.Object.FindFirstObjectByType<TomatoFruit>();
                result += "DELIVERY: position " + fruit.transform.position.ToString("F4") +
                    ", held " + fruit.GetComponent<Grabbable>().SelectingPointsCount +
                    ", inside " + UnityEngine.Object.FindFirstObjectByType<TomatoHarvestBasket>().Contains(fruit.transform.position) +
                    Environment.NewLine;
                Require(seeds.HarvestComplete && !fruit.GetComponent<Grabbable>().enabled, "Release inside tray records harvest and disables grab");
                VerifyFruitDiameter(fruit, "stored in tray");
                TomatoHarvestBasket tray = UnityEngine.Object.FindFirstObjectByType<TomatoHarvestBasket>();
                Require(fruit.transform.parent == tray.transform,
                    "Stored fruit follows the tray instead of jumping to its root");
                MeshRenderer model = fruit.GetComponentInChildren<MeshFilter>().GetComponent<MeshRenderer>();
                Require(fruit.gameObject.activeInHierarchy && model.enabled && model.gameObject.activeInHierarchy,
                    "Delivered tomato model remains visible");
                BoxCollider floor = Array.Find(tray.GetComponentsInChildren<BoxCollider>(),
                    collider => collider.name == "Base" && collider.enabled && !collider.isTrigger);
                Require(floor != null, "Current scene has a solid box floor");
                MeshFilter filter = model.GetComponent<MeshFilter>();
                float lowestVertex = float.PositiveInfinity;
                foreach (Vector3 vertex in filter.sharedMesh.vertices)
                    lowestVertex = Mathf.Min(lowestVertex, filter.transform.TransformPoint(vertex).y);
                float floorGap = lowestVertex - floor.bounds.max.y;
                result += "RESTING: model-to-floor gap = " + floorGap.ToString("F6") + " m" + Environment.NewLine;
                Require(floorGap >= -0.001f && floorGap <= 0.001f,
                    "Delivered tomato rests on the box floor without floating or sinking");
                Vector3 floorPoint = new(fruit.transform.position.x, floor.bounds.center.y, fruit.transform.position.z);
                Require(floor.bounds.Contains(floorPoint), "Stored tomato is over the visible box floor");
                foreach (GrabInteractable interactable in fruit.GetComponentsInChildren<GrabInteractable>(true))
                    Require(!interactable.enabled, "Stored fruit cannot be controller-grabbed");
                foreach (HandGrabInteractable interactable in fruit.GetComponentsInChildren<HandGrabInteractable>(true))
                    Require(!interactable.enabled, "Stored fruit cannot be hand-grabbed");
                foreach (Collider collider in fruit.GetComponentsInChildren<Collider>(true))
                    Require(!collider.enabled, "Stored fruit is decorative without interaction colliders");
                Require(fruit.GetComponent<Rigidbody>().isKinematic && !fruit.GetComponent<Rigidbody>().useGravity,
                    "Delivered fruit stays still in the tray");
                if (harvestOnly)
                {
                    result += "PASS: RootswithTomatoes.036 retains its stalk, mesh and materials; all ripe fruit models are at most 6.5 cm; harvest fruit has its own collider and Meta hand/controller interactables" + Environment.NewLine;
                    result += "PASS: harvest outline visible on plant, Select detaches fruit immediately and hides outline, release outside tray does not harvest, re-grab and tray delivery succeed" + Environment.NewLine;
                    result += "PASS: delivered tomato remains visible on the box floor, with gravity, colliders and all Meta grabs disabled" + Environment.NewLine;
                    result += "NOT TESTED: physical hands/controllers, shader appearance in headset, watering/growth sequence, scene restart, APK and Quest 3 performance" + Environment.NewLine;
                    Finish();
                    return;
                }
                panel.RefillCans();
                foreach (WateringCan can in UnityEngine.Object.FindObjectsByType<WateringCan>(FindObjectsSortMode.None))
                    Require(Mathf.Approximately(can.CurrentWaterLitres, can.CapacityLitres), "Can refilled");
                foreach (Text text in UnityEngine.Object.FindFirstObjectByType<TomatoPrototypeGuide>().GetComponentsInChildren<Text>())
                    Require(text.preferredHeight <= text.rectTransform.rect.height + 1, "Guide text fits its panel");
                result += "PASS: isolated 6.5 cm fruit, harvest outline, simulated Meta detach, release and re-grab, tray delivery, refill and text layout" + Environment.NewLine;
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
        File.WriteAllText(harvestOnly ? "Docs/Validation/TomatoHarvestVerification.txt" : ReportPath, result);
        EditorApplication.update -= Tick;
        EditorApplication.isPlaying = false;
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new Exception("Tomato runtime verification: " + label);
    }

    private static void VerifyHarvestFruit(TomatoFruit fruit)
    {
        MeshFilter filter = fruit.GetComponentInChildren<MeshFilter>();
        Require(filter != null && filter.sharedMesh != null && filter.name == TomatoFruitMesh.HarvestModelName &&
            filter.sharedMesh.name == TomatoFruitMesh.HarvestModelName && filter.sharedMesh.subMeshCount == 2,
            "Harvest uses the original RootswithTomatoes.036 model with its stalk");
        Require(TomatoFruitMesh.TryGetFruitBounds(filter, filter.GetComponent<MeshRenderer>(), out Bounds bounds),
            "Harvest model has a tomato surface");
        Vector3 size = Vector3.Scale(bounds.size, filter.transform.lossyScale);
        float diameter = Mathf.Max(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        Require(Mathf.Abs(diameter - TomatoFruit.DiameterMetres) < 0.001f, "Fruit diameter is 6.5 cm");
        SphereCollider collider = fruit.GetComponent<SphereCollider>();
        Require(collider != null && collider.enabled && !collider.isTrigger &&
            collider.attachedRigidbody == fruit.GetComponent<Rigidbody>(), "Fruit has its own active solid collider");
        foreach (Collider candidate in fruit.GetComponentInChildren<HandGrabInteractable>().Colliders)
            Require(candidate == collider, "Hand grab targets only the fruit collider");
        MeshRenderer shell = HarvestOutline(fruit);
        Require(shell != null && shell.enabled &&
            shell.sharedMaterial.shader.name == "Cozy Garden/Tomato Harvest Outline" &&
            shell.GetComponent<MeshFilter>().sharedMesh.subMeshCount == 1 &&
            shell.GetComponent<Collider>() == null, "Outline follows only the tomato surface without a grab collider");
        foreach (MeshFilter model in fruit.transform.parent.GetComponentsInChildren<MeshFilter>())
        {
            if (!model.name.StartsWith("RootswithTomatoes", StringComparison.Ordinal) ||
                !TomatoFruitMesh.TryGetFruitBounds(model, model.GetComponent<MeshRenderer>(), out Bounds modelBounds)) continue;
            Vector3 modelSize = Vector3.Scale(modelBounds.size, model.transform.lossyScale);
            Require(Mathf.Max(Mathf.Abs(modelSize.x), Mathf.Abs(modelSize.y), Mathf.Abs(modelSize.z)) <=
                TomatoFruit.DiameterMetres + 0.001f, "Every ripe tomato is at most 6.5 cm");
        }
    }

    private static MeshRenderer HarvestOutline(TomatoFruit fruit) =>
        Array.Find(fruit.GetComponentsInChildren<MeshRenderer>(true), renderer => renderer.name == "Harvest outline");

    private static void VerifyFruitDiameter(TomatoFruit fruit, string state)
    {
        MeshFilter model = fruit.GetComponentInChildren<MeshFilter>();
        Require(TomatoFruitMesh.TryGetFruitBounds(model, model.GetComponent<MeshRenderer>(), out Bounds bounds),
            "Fruit bounds during " + state);
        float diameter = Mathf.Max(
            model.transform.TransformVector(Vector3.right * bounds.size.x).magnitude,
            model.transform.TransformVector(Vector3.up * bounds.size.y).magnitude,
            model.transform.TransformVector(Vector3.forward * bounds.size.z).magnitude);
        result += "MEASURE: " + state + " = " + diameter.ToString("F6") + " m" + Environment.NewLine;
        Require(Mathf.Abs(diameter - TomatoFruit.DiameterMetres) < 0.001f, "Fruit keeps its diameter during " + state);
    }
}

