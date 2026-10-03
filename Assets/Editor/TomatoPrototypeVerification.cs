using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Runs the prototype checks and records evidence inside the project.</summary>
[InitializeOnLoad]
public static class TomatoPrototypeVerification
{
    private const string RequestPath = "Docs/Validation/TomatoVerification.request";
    private const string ReportPath = "Docs/Validation/TomatoVerification.txt";

    static TomatoPrototypeVerification()
    {
        EditorApplication.delayCall += RunRequestedChecks;
    }

    private static void RunRequestedChecks()
    {
        if (!File.Exists(RequestPath)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += RunRequestedChecks;
            return;
        }
        File.Delete(RequestPath);
        VerifyAndReport();
    }

    [MenuItem("Garden/Verify tomato prototype and save report")]
    public static void VerifyAndReport()
    {
        Directory.CreateDirectory("Docs/Validation");
        string result = "Unity " + Application.unityVersion + Environment.NewLine;
        result += "UTC: " + DateTime.UtcNow.ToString("O") + Environment.NewLine;
        try
        {
            VerifyScene();
            result += "PASS: Garden_Moves entry scene and serialized references" + Environment.NewLine;
            GardenCalendarVerification.VerifyHandInput();
            result += "PASS: existing Meta ray/pinch calendar checks" + Environment.NewLine;
            GardenCalendarVerification.Verify();
            result += "PASS: soil area, water gates, all phases, accounting, harvest and empty session" + Environment.NewLine;
            result += "NOT TESTED: physical hands/controllers, runtime scene reload, APK and Quest 3" + Environment.NewLine;
            File.WriteAllText(ReportPath, result);
            Debug.Log("Tomato prototype checks passed. Report: " + ReportPath);
        }
        catch (Exception exception)
        {
            File.WriteAllText(ReportPath, result + "FAIL: " + exception + Environment.NewLine);
            Debug.LogException(exception);
        }
    }

    private static void VerifyScene()
    {
        EditorBuildSettingsScene entry = Array.Find(EditorBuildSettings.scenes, scene => scene.enabled);
        Require(entry != null && entry.path == "Assets/Scenes/Garden_Moves.unity", "Entry scene");
        var preview = EditorSceneManager.OpenPreviewScene(entry.path);
        try
        {
            CalendarSystem calendar = null;
            SeedsController seeds = null;
            TomatoPrototypeGuide guide = null;
            TomatoHarvestBasket basket = null;
            GardenPhaseTimeCycle timeCycle = null;
            int validPots = 0;
            foreach (GameObject root in preview.GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                    Require(component != null, "No missing scripts in saved scene");
                calendar ??= root.GetComponentInChildren<CalendarSystem>(true);
                seeds ??= root.GetComponentInChildren<SeedsController>(true);
                guide ??= root.GetComponentInChildren<TomatoPrototypeGuide>(true);
                basket ??= root.GetComponentInChildren<TomatoHarvestBasket>(true);
                timeCycle ??= root.GetComponentInChildren<GardenPhaseTimeCycle>(true);
                foreach (GardenPot pot in root.GetComponentsInChildren<GardenPot>(true))
                    if (pot.AcceptsTomato && pot.SubstrateAreaSquareMetres > 0f) validPots++;
            }
            Require(calendar != null && seeds != null && guide != null && basket != null, "Prototype scene components");
            Require(validPots == 3, "Three valid tomato pots");
            Require(new SerializedObject(seeds).FindProperty("calendar").objectReferenceValue == calendar, "Calendar binding");
            Require(new SerializedObject(guide).FindProperty("seeds").objectReferenceValue == seeds, "Guide binding");
            var guideSettings = new SerializedObject(guide);
            Require(guideSettings.FindProperty("instruction").objectReferenceValue != null &&
                    guideSettings.FindProperty("water").objectReferenceValue != null, "Saved guide text bindings");
            BoxCollider volume = new SerializedObject(basket).FindProperty("collectionVolume").objectReferenceValue as BoxCollider;
            Require(volume != null && volume.isTrigger && volume.transform.IsChildOf(basket.transform),
                "Saved harvest volume belongs to tray");
            Require(timeCycle != null && new SerializedObject(calendar).FindProperty("timeCycle").objectReferenceValue == timeCycle,
                "Calendar sky cycle binding");
            var cycleSettings = new SerializedObject(timeCycle);
            Material sky = cycleSettings.FindProperty("skybox").objectReferenceValue as Material;
            Require(sky != null && sky.HasProperty("_CubemapTransition") && sky.GetTexture("_Tex") != null &&
                sky.GetTexture("_Tex_Blend") != null && cycleSettings.FindProperty("sun").objectReferenceValue != null,
                "Blend skybox, both cubemaps and sun assigned");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new Exception("Tomato scene verification: " + label);
    }
}
