using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Optional editor check for milestone and session-only state behavior.</summary>
public static class GardenCalendarVerification
{
    [MenuItem("Garden/Verify calendar hand input")]
    public static void VerifyHandInput()
    {
        GameObject prefab = PrefabUtility.LoadPrefabContents("Assets/Prefabs/GardenCalendarVR.prefab");
        try
        {
            CalendarVisibilityController visibility = prefab.GetComponent<CalendarVisibilityController>();
            Assert(visibility != null, "Visibility controller");
            Collider collider = prefab.GetComponent<Collider>();
            Assert(collider != null, "Calendar ray collider");
            SerializedObject settings = new SerializedObject(visibility);
            Assert(settings.FindProperty("rayCollider").objectReferenceValue == collider, "Visibility ray collider binding");
            SerializedObject surface = new SerializedObject(prefab.GetComponent<Oculus.Interaction.Surfaces.ColliderSurface>());
            Assert(surface.FindProperty("_collider").objectReferenceValue == collider, "Meta ray surface binding");
            Assert(settings.FindProperty("closeButton").objectReferenceValue != null, "Close button binding");
            SerializedObject panel = new SerializedObject(prefab.GetComponent<CalendarPanel>());
            Assert(panel.FindProperty("advanceButton").objectReferenceValue != null, "Advance button binding");

            MethodInfo sample = typeof(CalendarVisibilityController).GetMethod("UpdatePinchState", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert(sample != null, "Debounced pinch sampler");
            CheckPinch(visibility, sample, true, true, 1f, 0f, false, "Held on startup");
            CheckPinch(visibility, sample, true, false, 0f, 1f, false, "Begin release");
            CheckPinch(visibility, sample, true, false, 0f, 1.11f, false, "Stable release");
            CheckPinch(visibility, sample, true, true, 1f, 1.2f, true, "First pinch");
            for (int i = 0; i < 90; i++)
                CheckPinch(visibility, sample, true, true, 1f, 1.21f + i / 90f, false, "Held pinch " + i);
            CheckPinch(visibility, sample, true, false, 0.5f, 3f, false, "Partial release");
            CheckPinch(visibility, sample, true, false, 0f, 3.1f, false, "Brief tracking noise");
            CheckPinch(visibility, sample, true, true, 1f, 3.15f, false, "Noise must not rearm");
            CheckPinch(visibility, sample, true, false, 0f, 4f, false, "Second release");
            CheckPinch(visibility, sample, true, false, 0f, 4.11f, false, "Second stable release");
            CheckPinch(visibility, sample, true, true, 1f, 4.2f, true, "Second pinch");
            CheckPinch(visibility, sample, false, false, 0f, 5f, false, "Tracking lost");
            CheckPinch(visibility, sample, true, true, 1f, 6f, false, "Held through tracking recovery");
            CheckPinch(visibility, sample, true, false, 0f, 7f, false, "Recovery release");
            CheckPinch(visibility, sample, true, false, 0f, 7.11f, false, "Recovery stable release");
            CheckPinch(visibility, sample, true, true, 1f, 7.2f, true, "Pinch after recovery");
            CheckPinch(visibility, sample, true, false, 0f, 8f, false, "Release before grabbing");
            CheckPinch(visibility, sample, true, false, 0f, 8.11f, false, "Arm before grabbing");
            CheckPinch(visibility, sample, false, true, 1f, 8.2f, false, "Object grab consumes pinch");
            CheckPinch(visibility, sample, false, false, 0f, 8.3f, false, "Object release consumes gesture");
            CheckPinch(visibility, sample, true, true, 1f, 8.4f, false, "Held pinch after object release");
            CheckPinch(visibility, sample, true, false, 0f, 8.5f, false, "Release after grabbing");
            CheckPinch(visibility, sample, true, false, 0f, 8.61f, false, "Stable release after grabbing");
            CheckPinch(visibility, sample, true, true, 1f, 8.7f, true, "Fresh calendar gesture after grabbing");
            Debug.Log("Garden calendar hand input verification passed: ray/button wiring, held pinches, release debounce, tracking recovery and consumed object gestures.");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }

    private static void CheckPinch(CalendarVisibilityController visibility, MethodInfo sample,
        bool valid, bool pinching, float strength, float now, bool expected, string name)
    {
        Assert((bool)sample.Invoke(visibility, new object[] { valid, pinching, strength, now }) == expected, name);
    }

    [MenuItem("Garden/Verify calendar session behavior")]
    public static void Verify()
    {
        GameObject clockObject = null;
        GameObject controllerObject = null;
        GameObject potObject = null;
        GameObject seedObject = null;
        GameObject freshObject = null;
        try
        {
            clockObject = new GameObject("Calendar verification temporary");
            CalendarSystem clock = clockObject.AddComponent<CalendarSystem>();
            int blocked = 0;
            clock.AdvanceBlocked += _ => blocked++;
            clock.AdvancePhase();
            Assert(clock.CurrentDay == 0 && blocked == 1, "No planting, no time advance");

            potObject = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/MamkinEnthusiast/3D Mini Garden Props/Prefabs/PotSmall.prefab"));
            potObject.transform.localScale = Vector3.one * 0.6f;
            GardenPot pot = potObject.AddComponent<GardenPot>();
            Set(pot, "potId", "verification-pot");
            Set(pot, "localPlantingPoint", new Vector3(0, 0.23f, 0));
            Assert(Mathf.Abs(pot.SubstrateAreaSquareMetres - 0.0144f) < 0.00001f, "Measured soil area");
            Assert(!pot.ContainsSoilPoint(pot.PlantingPoint + Vector3.right * 0.2f), "Rim and outside soil rejected");
            Assert(!pot.CanReceiveWaterAt(pot.PlantingPoint - Vector3.up * 0.2f), "Pot bottom is not soil watering");
            seedObject = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/SeedsPrefab/TomatoSeed.prefab"));
            SeedItem seed = seedObject.GetComponent<SeedItem>();
            seed.AssignId("verification-tomato");
            seed.transform.position = pot.PlantingPoint;
            controllerObject = new GameObject("Controller verification temporary");
            SeedsController controller = controllerObject.AddComponent<SeedsController>();
            Set(controller, "calendar", clock);
            Set(controller, "pots", new[] { pot });
            Invoke(controller, "Start");
            Assert(controller.TryPlant(seed), "Planting valid tomato");
            var plant = controller.PrototypeTomato;
            var saved = controller.GetState(plant);
            Assert(Mathf.Abs(controller.TargetMillilitres(plant) - 51.42857f) < 0.01f, "Area and care-day conversion");
            clock.RecordHarvest(saved);
            Assert(!saved.harvested, "No harvest before maturity");
            float accumulated = 0f;
            int[] days = { 7, 45, 60, 80, 110 };
            GrowthStage[] stages = { GrowthStage.Sprout, GrowthStage.YoungPlant, GrowthStage.Flowering,
                GrowthStage.GreenFruit, GrowthStage.Mature };
            for (int phase = 0; phase < days.Length; phase++)
            {
                int before = clock.CurrentDay;
                clock.AdvancePhase();
                Assert(clock.CurrentDay == before, "Dry phase blocks " + phase);
                float target = controller.TargetMillilitres(plant);
                controller.ReceiveWater(pot, target * 0.5f / 1000f);
                accumulated += target * 0.5f;
                SettlePour(saved);
                clock.AdvancePhase();
                Assert(clock.CurrentDay == before, "Partial water blocks " + phase);
                float remainder = target * (phase == 0 ? 2f : 0.5f);
                controller.ReceiveWater(pot, remainder / 1000f);
                accumulated += remainder;
                SettlePour(saved);
                if (phase == 0)
                {
                    clock.AdvancePhase();
                    Assert(clock.CurrentDay == before, "Excess blocks until drainage");
                    saved.lastWaterAt = Time.unscaledTime - 10f;
                    Invoke(controller, "Update");
                    Assert(saved.phaseWaterMillilitres > controller.ExcessLimit(plant), "Drainage preserves applied water");
                }
                Assert(plant.stage != stages[phase], "Water alone does not grow plant");
                clock.AdvancePhase();
                Assert(clock.CurrentDay == days[phase] && plant.stage == stages[phase], "Water permits phase " + phase);
                Assert(saved.phaseWaterMillilitres == 0f && saved.currentPourMillilitres == 0f,
                    "Phase counters reset " + phase);
                Assert(Mathf.Abs(saved.waterMillilitres - accumulated) < 0.1f, "Cumulative water retained " + phase);
            }
            var visuals = (GameObject[])typeof(SeedItem).GetField("_tomatoStageInstances", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(seed);
            Assert(visuals[5] != null && visuals[5].activeSelf, "Mature visual is available; physical grabbing is tested in Play");
            clock.RecordHarvest(saved);
            clock.RecordHarvest(saved);
            Assert(saved.harvested, "Mature harvest recorded");
            int harvests = 0;
            foreach (var entry in clock.Journal) if (entry.action == "Harvested") harvests++;
            Assert(harvests == 1, "Harvest is idempotent");
            clock.AdvancePhase();
            Assert(clock.CurrentDay == 110, "No advance past final milestone");
            freshObject = new GameObject("Fresh calendar verification temporary");
            CalendarSystem fresh = freshObject.AddComponent<CalendarSystem>();
            Assert(fresh.CurrentDay == 0 && fresh.Plants.Count == 0 && fresh.Journal.Count == 0,
                "New session starts empty");
            Debug.Log("Garden tomato verification passed: area, dry/partial/excess water gates, five transitions, counters, harvest and fresh session.");
        }
        finally
        {
            if (controllerObject != null) UnityEngine.Object.DestroyImmediate(controllerObject);
            if (seedObject != null) UnityEngine.Object.DestroyImmediate(seedObject);
            if (potObject != null) UnityEngine.Object.DestroyImmediate(potObject);
            if (clockObject != null) UnityEngine.Object.DestroyImmediate(clockObject);
            if (freshObject != null) UnityEngine.Object.DestroyImmediate(freshObject);
        }
    }

    private static void SettlePour(CalendarSystem.PlantSave saved) => saved.lastWaterAt = Time.unscaledTime - 1f;

    private static void Invoke(object target, string method) =>
        target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);

    private static void Set(object target, string field, object value)
    {
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(target, value);
    }

    private static void Assert(bool passes, string name)
    {
        if (!passes) throw new Exception("Garden calendar verification failed: " + name);
    }
}
