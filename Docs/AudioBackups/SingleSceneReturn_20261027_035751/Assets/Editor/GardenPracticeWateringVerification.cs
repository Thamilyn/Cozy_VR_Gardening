using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Checks the real watered practice plant without changing its phase.</summary>
public static class GardenPracticeWateringVerification
{
    [MenuItem("Garden/Verify watered practice without growth")]
    public static void Verify()
    {
        Require(EditorApplication.isPlaying && SceneManager.GetActiveScene().name == GardenMenuController.PracticeSceneName,
            "Enter Play in Garden_Practice first.");
        Require(!GardenMenuController.GardenSuspended, "Close the menu before checking the watered practice.");
        var seeds = UnityEngine.Object.FindFirstObjectByType<SeedsController>();
        var plant = seeds != null ? seeds.PrototypeTomato : null;
        var state = seeds != null ? seeds.GetState(plant) : null;
        Require(plant != null && state != null, "Plant a seed in practice first.");
        float target = seeds.TargetMillilitres(plant);
        Require(target > 0f && state.retainedWaterMillilitres >= target, "Finish the first watering before running this check.");
        Require(state.tomatoPhase == 0 && plant.seed.StageVisual != null && plant.seed.StageVisual.name == "Tomate0",
            "The watered seed must retain the soil-only appearance.");
        Require(!GronnyTutorial.Allows(GardenTutorialAction.Advance), "Practice must not permit advancing time.");
        int originalDay = seeds.Calendar.CurrentDay;
        seeds.Calendar.AdvancePhase();
        Require(originalDay == 0 && seeds.Calendar.CurrentDay == originalDay && !seeds.Calendar.IsAdvancing && state.tomatoPhase == 0,
            "The clock must reject advance even after the water target is met.");
        var tutorial = UnityEngine.Object.FindFirstObjectByType<GronnyTutorial>();
        Require(tutorial != null && tutorial.GetComponent<GronnyVoicePlayer>() != null, "Practice tutorial and voice must be initialized.");
        var runtimeCatalogue = (GronnyAudioCatalogue)typeof(GronnyTutorial)
            .GetField("catalogue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tutorial);
        Require(runtimeCatalogue != Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial") &&
            runtimeCatalogue.Find("01_welcome").clip == Resources.Load<AudioClip>("GronnyAudio/PracticeWelcome"),
            "Practice must use its own welcome without modifying the main catalogue.");
        Require(runtimeCatalogue.Find("01_welcome").transcript.Contains("sustainable"), "Practice welcome must explain sustainability.");
        Require(tutorial.TryGetInstruction(out string instruction, out _) && instruction.Contains("Practice complete"),
            "After watering and stopping, practice must report completion.");
        Directory.CreateDirectory("output");
        File.WriteAllText("output/PracticeFirstWateringPlay.txt", "PASS: first watering complete; soil-only appearance; day zero; calendar advance blocked; independent sustainable welcome; practice completion.\nUnity " + Application.unityVersion);
        Debug.Log("Practice watering verification passed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
