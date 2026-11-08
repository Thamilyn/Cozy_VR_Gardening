using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Styles authored crop stages as well as props instantiated from the scene.</summary>
public static class GardenCropPastelSetup
{
    private const string Folder = "Assets/Materials/GardenPastel/Crops";

    [MenuItem("Garden/Apply pastel crop and pot materials")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Garden_Moves" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Garden_Moves outside Play mode.");
        Shader shader = Shader.Find("Cozy Garden/Styled Props");
        if (shader == null || !shader.isSupported) throw new InvalidOperationException("Pastel prop shader unavailable.");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Materials/GardenPastel", "Crops");
        Dictionary<Material, Material> replacements = new();
        List<string> changedPrefabs = new();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/TomatoStages" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = Style(root, shader, replacements, false);
                if (!changed) continue;
                PrefabUtility.SaveAsPrefabAsset(root, path);
                changedPrefabs.Add(path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (GameObject root in scene.GetRootGameObjects()) Style(root, shader, replacements, true);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory("Docs/Validation");
        File.WriteAllText("Docs/Validation/CropPastelChanges.json", Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            prefabs = changedPrefabs,
            materials = replacements.Select(pair => new { source = AssetDatabase.GetAssetPath(pair.Key), name = pair.Key.name, pastel = AssetDatabase.GetAssetPath(pair.Value) }).ToArray()
        }, Newtonsoft.Json.Formatting.Indented));
    }

    private static bool Style(GameObject root, Shader shader, Dictionary<Material, Material> replacements, bool scene)
    {
        bool changed = false;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is SkinnedMeshRenderer || renderer.name == "Floor" ||
                renderer.GetComponentInParent<OVRCameraRig>(true) != null ||
                renderer.GetComponentInParent<Canvas>(true) != null) continue;
            Material[] materials = renderer.sharedMaterials;
            bool replaced = false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material source = materials[i];
                if (!CanStyle(source)) continue;
                if (!replacements.TryGetValue(source, out Material target))
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long localId);
                    string path = $"{Folder}/{guid}_{localId}.mat";
                    target = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (target == null)
                    {
                        target = CreateMaterial(source, shader);
                        AssetDatabase.CreateAsset(target, path);
                    }
                    target.name = source.name;
                    EditorUtility.SetDirty(target);
                    replacements.Add(source, target);
                }
                materials[i] = target;
                replaced = true;
            }
            if (!replaced) continue;
            if (scene) Undo.RecordObject(renderer, "Apply pastel crop and prop materials");
            renderer.sharedMaterials = materials;
            if (scene) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            changed = true;
        }
        return changed;
    }

    private static bool CanStyle(Material material)
    {
        if (material == null || material.shader == null || material.renderQueue >= 3000) return false;
        if (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f) return false;
        if (material.HasProperty("_Mode") && material.GetFloat("_Mode") > 1f) return false;
        return material.shader.name is "Universal Render Pipeline/Lit" or "Universal Render Pipeline/Simple Lit" or "Standard" or "Unlit/BasicPBR";
    }

    private static Material CreateMaterial(Material source, Shader shader)
    {
        // Soil/fruit recognition relies on the original name, not just the renderer slot.
        Material target = new(shader) { name = source.name, enableInstancing = true };
        string map = source.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
        if (source.HasProperty(map))
        {
            target.SetTexture("_BaseMap", source.GetTexture(map));
            target.SetTextureScale("_BaseMap", source.GetTextureScale(map));
            target.SetTextureOffset("_BaseMap", source.GetTextureOffset(map));
        }
        Color tint = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
        target.SetColor("_BaseColor", tint);
        // Legacy tint stays neutral; runtime wet/ripe states set both supported tint IDs.
        target.SetColor("_Color", Color.white);
        bool cutout = source.IsKeywordEnabled("_ALPHATEST_ON") || (source.HasProperty("_AlphaClip") && source.GetFloat("_AlphaClip") > 0.5f);
        target.SetFloat("_AlphaClip", cutout ? 1f : 0f);
        target.SetFloat("_Cutoff", source.HasProperty("_Cutoff") ? source.GetFloat("_Cutoff") : 0.5f);
        target.SetFloat("_Cull", source.HasProperty("_Cull") ? source.GetFloat("_Cull") : 2f);
        if (cutout) target.EnableKeyword("_ALPHATEST_ON");
        // Matte cel surfaces use mesh normals and omit specular, metallic and normal textures.
        target.SetFloat("_BumpScale", 0f);
        return target;
    }
}
