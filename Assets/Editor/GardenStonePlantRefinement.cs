using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GardenStonePlantRefinement
{
    [MenuItem("Garden/Refine pastel stones and small plant")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Garden_Moves" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Garden_Moves outside Play mode.");
        Shader shader = Shader.Find("Cozy Garden/Styled Props");
        if (shader == null || !shader.isSupported) throw new InvalidOperationException("Pastel prop shader unavailable.");
        const string folder = "Assets/Materials/GardenPastel";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Materials", "GardenPastel");
        Material stone = AssetDatabase.LoadAssetAtPath<Material>(folder + "/PastelStone.mat");
        if (stone == null)
        {
            stone = new Material(shader) { name = "Pastel Stone", enableInstancing = true };
            stone.SetColor("_BaseColor", new Color(0.34f, 0.39f, 0.43f));
            stone.SetColor("_StoneMossColor", new Color(0.36f, 0.46f, 0.29f));
            stone.SetFloat("_StoneMossStrength", 0.55f);
            stone.SetFloat("_BumpScale", 0f);
            AssetDatabase.CreateAsset(stone, folder + "/PastelStone.mat");
        }
        GardenDistantGround ground = UnityEngine.Object.FindFirstObjectByType<GardenDistantGround>();
        SerializedObject settings = new(ground);
        settings.FindProperty("pastelRockMaterial").objectReferenceValue = stone;
        settings.ApplyModifiedProperties();
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.transform.parent == null || renderer.transform.parent.name != "Plant_Pot" ||
                (renderer.name != "Sprout" && renderer.name != "Basket_S")) continue;
            string materialPath = folder + "/" + renderer.name + "Pastel.mat";
            Material plant = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (plant == null)
            {
                Material source = renderer.sharedMaterial;
                if (source == null) continue;
                // Preserve the model's colour atlas while removing PBR highlights.
                string map = source.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                plant = new Material(shader) { name = renderer.name + " Pastel", enableInstancing = true };
                plant.SetTexture("_BaseMap", source.GetTexture(map));
                plant.SetTextureScale("_BaseMap", source.GetTextureScale(map));
                plant.SetTextureOffset("_BaseMap", source.GetTextureOffset(map));
                plant.SetColor("_BaseColor", source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white);
                plant.SetFloat("_BumpScale", 0f);
                plant.SetFloat("_Cull", 0f);
                AssetDatabase.CreateAsset(plant, materialPath);
            }
            Material[] shared = renderer.sharedMaterials;
            for (int i = 0; i < shared.Length; i++) shared[i] = plant;
            Undo.RecordObject(renderer, "Unify small plant with pastel environment");
            renderer.sharedMaterials = shared;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        ground.Rebuild();
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
    }
}
