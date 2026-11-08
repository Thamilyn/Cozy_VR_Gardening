using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GardenPastelSceneSetup
{
    [MenuItem("Garden/Apply pastel cel environment")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply outside Play mode.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Garden_Moves") throw new InvalidOperationException("Open Garden_Moves first.");
        GardenPastelGraphBuilder.Build();
        Shader[] variants = { Load("PastelLeaves"), Load("PastelLayered"), Load("PastelProps"), Load("PastelFlowers") };
        Shader skyShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Materials/GardenPastelSky.shader");
        foreach (Shader shader in variants.Concat(new[] { skyShader }))
            if (shader == null || !shader.isSupported || ShaderUtil.GetShaderMessages(shader).Any(m => m.severity.ToString() == "Error"))
                throw new InvalidOperationException("A pastel shader failed validation.");
        GardenDistantGround ground = UnityEngine.Object.FindFirstObjectByType<GardenDistantGround>();
        SerializedObject serialized = new(ground);
        serialized.FindProperty("flowerLightingShader").objectReferenceValue = variants[3];
        SerializedProperty nature = serialized.FindProperty("pastelNatureShaders");
        nature.arraySize = 3;
        for (int i = 0; i < 3; i++) nature.GetArrayElementAtIndex(i).objectReferenceValue = variants[i];
        serialized.ApplyModifiedProperties();
        GardenPhaseTimeCycle cycle = UnityEngine.Object.FindFirstObjectByType<GardenPhaseTimeCycle>();
        SerializedObject cycleSettings = new(cycle);
        Material source = (Material)cycleSettings.FindProperty("skybox").objectReferenceValue;
        if (source == null) throw new InvalidOperationException("Missing cycle sky material.");
        const string skyPath = "Assets/Materials/GardenPastel/PastelSky.mat";
        Material sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if (sky == null)
        {
            sky = new Material(source) { name = "Pastel Sky" };
            AssetDatabase.CreateAsset(sky, skyPath);
        }
        sky.shader = skyShader;
        EditorUtility.SetDirty(sky);
        cycleSettings.FindProperty("skybox").objectReferenceValue = sky;
        cycleSettings.ApplyModifiedProperties();
        RenderSettings.skybox = sky;
        ground.Rebuild();
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory("Docs/Validation");
        File.WriteAllText("Docs/Validation/PastelSetup.txt", "Pastel nature, terrain, props and sky assigned to Garden_Moves.\nNo unused assets deleted.\n");
    }

    private static Shader Load(string name) => AssetDatabase.LoadAssetAtPath<Shader>("Assets/Materials/GardenPastel/" + name + ".shadergraph");
}
