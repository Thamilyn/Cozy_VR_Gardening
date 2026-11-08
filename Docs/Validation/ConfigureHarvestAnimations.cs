using System;
using UnityEditor;
using UnityEngine;

public static class ConfigureHarvestAnimations
{
    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Configure outside Play.");
        var catalogue = AssetDatabase.LoadAssetAtPath<GronnyAudioCatalogue>("Assets/Resources/GronnyAudio/Tutorial.asset");
        catalogue.pruningShearsVisual = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/MamkinEnthusiast/3D Mini Garden Props/Prefabs/Pruner.prefab");
        if (catalogue.pruningShearsVisual == null) throw new Exception("Existing Pruner prefab missing");
        var cut = catalogue.Find("harvest_cut_stem");
        cut.demo = GardenControlDemo.CutStem;
        cut.controllerAnimation = catalogue.controllerTrigger;
        cut.controllerTarget = AnimationUtility.GetCurveBindings(catalogue.controllerTrigger)[0].path;
        cut.steps = new[] {
            Step(0f, GardenDemoPose.Curled, new Vector3(-0.13f, -0.05f, 0f), 0f, "Pick up Pruner"),
            Step(3.2f, GardenDemoPose.Open, Vector3.zero, 0f, "Open blades around the stem"),
            Step(8.5f, GardenDemoPose.Curled, Vector3.zero, 1f, "Close to cut the stem"),
            Step(10.8f, GardenDemoPose.Curled, Vector3.zero, 1f, "The tomato is separated")
        };
        var pick = catalogue.Find("harvest_stem_cut");
        pick.demo = GardenControlDemo.PickTomato;
        pick.controllerAnimation = catalogue.controllerGrip;
        pick.controllerTarget = AnimationUtility.GetCurveBindings(catalogue.controllerGrip)[0].path;
        pick.steps = new[] {
            Step(0f, GardenDemoPose.Open, new Vector3(-0.1f, 0f, -0.02f), 0f, "Reach for the loose tomato"),
            Step(1f, GardenDemoPose.Pinch, Vector3.zero, 1f, "Pinch or hold Grip"),
            Step(2.4f, GardenDemoPose.Pinch, new Vector3(0f, 0.08f, -0.03f), 1f, "Lift the separated tomato")
        };
        EditorUtility.SetDirty(catalogue);
        AssetDatabase.SaveAssetIfDirty(catalogue);
        return new { cutSteps = cut.steps.Length, pickSteps = pick.steps.Length,
            shears = AssetDatabase.GetAssetPath(catalogue.pruningShearsVisual) };
    }

    private static GronnyAudioCatalogue.Step Step(float at, GardenDemoPose pose, Vector3 position, float press, string label) =>
        new() { at = at, pose = pose, position = position, press = press, label = label };
}
