using System;
using UnityEditor;
using UnityEngine;

public static class ConfigureSoilAnimations
{
    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Configure soil demonstrations outside Play.");
        var catalogue = AssetDatabase.LoadAssetAtPath<GronnyAudioCatalogue>("Assets/Resources/GronnyAudio/Tutorial.asset");
        var scoop = catalogue.Find("soil_scoop_first");
        var pour = catalogue.Find("soil_cover_seed");
        if (scoop?.clip == null || pour?.clip == null)
            throw new InvalidOperationException("Assign the recorded soil instructions before their demonstrations.");
        catalogue.shovelVisual = Require("Assets/OurAss/GrabablePrfab/Shovel.prefab");
        catalogue.soilSourceVisual = Require("Assets/Prefabs/Furniture/SoilPot.prefab");
        catalogue.soilPotVisual = Require("Assets/MamkinEnthusiast/3D Mini Garden Props/Prefabs/PotSmall.prefab");
        var bindings = AnimationUtility.GetCurveBindings(catalogue.controllerGrip);
        if (bindings.Length == 0) throw new InvalidOperationException("Existing Meta Grip animation is missing.");
        foreach (var cue in new[] { scoop, pour })
        {
            cue.rightHand = false;
            cue.controllerAnimation = catalogue.controllerGrip;
            cue.controllerTarget = bindings[0].path;
        }
        scoop.demo = GardenControlDemo.SoilScoop;
        scoop.steps = new[]
        {
            Step(0f, GardenDemoPose.Open, new Vector3(-0.32f, 0.10f, 0f), 0f, 0f, "Pick up the shovel"),
            Step(1.3f, GardenDemoPose.Pinch, new Vector3(-0.29f, 0.10f, 0f), 0f, 1f, "Make a short pass over SoilPot"),
            Step(3.3f, GardenDemoPose.Pinch, new Vector3(-0.01f, 0.12f, 0f), 0f, 1f, "Carry the soil above the pot"),
            Step(6.4f, GardenDemoPose.Pinch, new Vector3(-0.01f, 0.12f, 0f), 65f, 1f, "Tilt and hold briefly to pour"),
            Step(8f, GardenDemoPose.Pinch, new Vector3(-0.01f, 0.12f, 0f), 65f, 1f, "The soil covers the seed")
        };
        pour.demo = GardenControlDemo.SoilPour;
        pour.steps = new[]
        {
            Step(0f, GardenDemoPose.Pinch, new Vector3(-0.01f, 0.12f, 0f), 0f, 1f, "Hold the loaded blade above the pot"),
            Step(7f, GardenDemoPose.Pinch, new Vector3(-0.01f, 0.12f, 0f), 65f, 1f, "Tilt and hold briefly to pour"),
            Step(8.4f, GardenDemoPose.Pinch, new Vector3(-0.01f, 0.12f, 0f), 65f, 1f, "The soil covers the seed")
        };
        EditorUtility.SetDirty(catalogue);
        AssetDatabase.SaveAssetIfDirty(catalogue);
        return new { scoopSteps = scoop.steps.Length, pourSteps = pour.steps.Length,
            shovel = AssetDatabase.GetAssetPath(catalogue.shovelVisual),
            source = AssetDatabase.GetAssetPath(catalogue.soilSourceVisual),
            pot = AssetDatabase.GetAssetPath(catalogue.soilPotVisual),
            scoopClip = AssetDatabase.GetAssetPath(scoop.clip), pourClip = AssetDatabase.GetAssetPath(pour.clip) };
    }

    private static GameObject Require(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path)
        ?? throw new InvalidOperationException("Existing example model is missing: " + path);

    private static GronnyAudioCatalogue.Step Step(float at, GardenDemoPose pose, Vector3 position,
        float tilt, float press, string label) => new()
        { at = at, pose = pose, position = position, rotation = new Vector3(tilt, 0f, 0f), press = press, label = label };
}
