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
        GameObject freshObject = null;
        try
        {
            clockObject = new GameObject("Calendar verification temporary");
            CalendarSystem clock = clockObject.AddComponent<CalendarSystem>();
            Assert(clock.CurrentDay == 0 && clock.PhaseIndexAt(0) == 0 && clock.Plants.Count == 0, "Fresh day zero");
            int[] expected = { 7, 45, 60, 80, 110 };
            foreach (int day in expected)
            {
                clock.AdvancePhase();
                Assert(clock.CurrentDay == day, "Milestone " + day);
            }
            clock.AdvancePhase();
            clock.AdvancePhase();
            Assert(clock.CurrentDay == 110 && clock.IsCycleComplete, "Final repeated clicks");

            // Late planting: at global day 45 the plant is only 38 days old.
            Set(clock, "elapsedGameDays", 7f);
            var planted = new CalendarSystem.PlantSave
            {
                seedId = "verification-tomato", crop = "Tomato", potId = "verification-pot",
                plantedAtGameDay = 7, stage = "Seed"
            };
            clock.RecordPlanting(planted);
            clock.RecordWater(planted, 125);
            Assert(clock.PhaseIndexAt(45 - planted.plantedAtGameDay) == 1, "Late plant at day 45");
            Assert(clock.PhaseIndexAt(110 - planted.plantedAtGameDay) == 4, "Late plant at day 110");
            controllerObject = new GameObject("Controller verification temporary");
            SeedsController controller = controllerObject.AddComponent<SeedsController>();
            Set(controller, "calendar", clock);
            MethodInfo calculate = typeof(SeedsController).GetMethod("CalculateStage", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert((GrowthStage)calculate.Invoke(controller, new object[] { SeedCrop.Tomato, 38f, 0f }) ==
                GrowthStage.Sprout, "Tomato ignores old water gate");
            Assert((GrowthStage)calculate.Invoke(controller, new object[] { SeedCrop.Tomato, 103f, 0f }) ==
                GrowthStage.GreenFruit, "Late tomato stays green");
            Assert(planted.waterMillilitres == 125 && clock.Journal.Count == 2, "Water journal");

            freshObject = new GameObject("Fresh calendar verification temporary");
            CalendarSystem fresh = freshObject.AddComponent<CalendarSystem>();
            Assert(fresh.CurrentDay == 0 && fresh.Plants.Count == 0 && fresh.Journal.Count == 0,
                "New session starts empty");
            Debug.Log("Garden calendar verification passed: milestones, final clicks, late tomato, water and fresh session state.");
        }
        finally
        {
            if (clockObject != null) UnityEngine.Object.DestroyImmediate(clockObject);
            if (controllerObject != null) UnityEngine.Object.DestroyImmediate(controllerObject);
            if (freshObject != null) UnityEngine.Object.DestroyImmediate(freshObject);
        }
    }

    private static void Set(object target, string field, object value)
    {
        typeof(CalendarSystem).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(target, value);
        if (target is SeedsController)
            typeof(SeedsController).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(target, value);
    }

    private static void Assert(bool passes, string name)
    {
        if (!passes) throw new Exception("Garden calendar verification failed: " + name);
    }
}
