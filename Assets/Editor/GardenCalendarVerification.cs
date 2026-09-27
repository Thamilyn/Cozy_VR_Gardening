using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Optional editor check for milestone and session-only state behavior.</summary>
public static class GardenCalendarVerification
{
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
