using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Applies matte meadow shading to opaque garden props, preserving textures and runtime tint IDs.</summary>
public static class GardenArtDirectionSetup
{
    private const string ShaderName = "Cozy Garden/Styled Props";

    [MenuItem("Garden/Apply unified meadow shading")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Garden_Moves") throw new InvalidOperationException("Open Garden_Moves first.");
        Shader shader = Shader.Find(ShaderName);
        if (shader == null || !shader.isSupported) throw new InvalidOperationException("Meadow prop shader is unavailable.");
        HashSet<Material> materials = new();
        List<Renderer> styledRenderers = new();
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is SkinnedMeshRenderer || renderer.name == "Floor" ||
                renderer.GetComponentInParent<OVRCameraRig>(true) != null ||
                renderer.GetComponentInParent<Canvas>(true) != null) continue;
            styledRenderers.Add(renderer);
            foreach (Material material in renderer.sharedMaterials)
                if (CanStyle(material)) materials.Add(material);
        }

        Directory.CreateDirectory("Docs/GardenStyleBackups");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/GardenStyle"))
            AssetDatabase.CreateFolder("Assets/Materials", "GardenStyle");
        List<string> changed = new();
        Dictionary<Material, Material> replacements = new();
        foreach (Material source in materials)
        {
            Material material = source;
            string path = AssetDatabase.GetAssetPath(source);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
            {
                string backup = "Docs/GardenStyleBackups/" + guid + ".mat";
                if (!File.Exists(backup)) File.Copy(path, backup);
            }
            else
            {
                // Embedded FBX materials are read-only: create a scene override instead of changing the model.
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string _, out long localId);
                string safeName = System.Text.RegularExpressions.Regex.Replace(source.name, "[^a-zA-Z0-9_-]", "_");
                path = "Assets/Materials/GardenStyle/" + safeName + "_" + guid + "_" + localId + ".mat";
                material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(source) { name = source.name + " Meadow" };
                    AssetDatabase.CreateAsset(material, path);
                }
                replacements.Add(source, material);
            }
            Texture albedo = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.mainTexture;
            Color tint = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
            Vector2 scale = material.mainTextureScale;
            Vector2 offset = material.mainTextureOffset;
            bool cutout = material.IsKeywordEnabled("_ALPHATEST_ON");
            bool emission = material.IsKeywordEnabled("_EMISSION");
            Texture normal = material.HasProperty("_BumpMap") ? material.GetTexture("_BumpMap") : null;
            float normalStrength = material.HasProperty("_BumpScale") ? material.GetFloat("_BumpScale") : 1f;
            Undo.RecordObject(material, "Unify garden prop shading");
            material.shader = shader;
            material.shaderKeywords = Array.Empty<string>();
            material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", tint);
            material.SetTextureScale("_BaseMap", scale);
            material.SetTextureOffset("_BaseMap", offset);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", Mathf.Clamp(normalStrength * 0.45f, 0f, 0.55f));
            material.SetFloat("_AlphaClip", cutout ? 1f : 0f);
            if (cutout) material.EnableKeyword("_ALPHATEST_ON");
            if (normal != null) material.EnableKeyword("_NORMALMAP");
            if (emission) material.EnableKeyword("_EMISSION");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            changed.Add(path);
        }
        foreach (Renderer renderer in styledRenderers)
        {
            Material[] shared = renderer.sharedMaterials;
            bool replaced = false;
            for (int i = 0; i < shared.Length; i++)
                if (shared[i] != null && replacements.TryGetValue(shared[i], out Material replacement))
                {
                    shared[i] = replacement;
                    replaced = true;
                }
            if (!replaced) continue;
            Undo.RecordObject(renderer, "Unify embedded garden materials");
            renderer.sharedMaterials = shared;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        foreach (Renderer renderer in styledRenderers)
        {
            // The atlas is also used by tools and pots: only decorative grass receives a wind variant.
            if (!renderer.name.StartsWith("Grass", StringComparison.OrdinalIgnoreCase)) continue;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) continue;
            Material[] shared = renderer.sharedMaterials;
            for (int i = 0; i < shared.Length; i++)
            {
                Material source = shared[i];
                if (source == null || source.shader != shader || source.IsKeywordEnabled("_GARDEN_WIND")) continue;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string materialGuid, out long _);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(filter.sharedMesh, out string meshGuid, out long meshId);
                string path = "Assets/Materials/GardenStyle/GrassWind_" + materialGuid + "_" + meshGuid + "_" + meshId + ".mat";
                Material wind = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (wind == null)
                {
                    wind = new Material(source) { name = source.name + " Grass Wind" };
                    wind.SetFloat("_WindEnabled", 1f);
                    wind.SetFloat("_WindStrength", 0.02f);
                    wind.SetFloat("_WindBase", filter.sharedMesh.bounds.min.y);
                    wind.SetFloat("_WindHeight", filter.sharedMesh.bounds.size.y);
                    wind.EnableKeyword("_GARDEN_WIND");
                    AssetDatabase.CreateAsset(wind, path);
                }
                shared[i] = wind;
                changed.Add(path);
            }
            Undo.RecordObject(renderer, "Animate decorative garden grass");
            renderer.sharedMaterials = shared;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        AssetDatabase.SaveAssets();
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (GardenDistantGround ground in root.GetComponentsInChildren<GardenDistantGround>(true)) ground.Rebuild();
        EditorSceneManager.SaveScene(scene);
        string reportPath = "Docs/Validation/ArtStyleMaterials.txt";
        string[] previous = File.Exists(reportPath) ? File.ReadAllLines(reportPath) : Array.Empty<string>();
        File.WriteAllText(reportPath, string.Join("\n", previous.Concat(changed).Distinct()));
        Debug.Log("Applied meadow shading to " + changed.Count + " opaque prop materials.");
    }

    private static bool CanStyle(Material material)
    {
        if (material == null || material.shader == null || material.renderQueue >= 3000) return false;
        string path = AssetDatabase.GetAssetPath(material);
        if (!path.StartsWith("Assets/", StringComparison.Ordinal)) return false;
        if (path.StartsWith("Assets/3D set of stylized nature", StringComparison.Ordinal) ||
            path.StartsWith("Assets/Oculus/", StringComparison.Ordinal) ||
            path.StartsWith("Assets/InteractionSDK/", StringComparison.Ordinal)) return false;
        if (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f) return false;
        if (material.HasProperty("_Mode") && material.GetFloat("_Mode") > 1f) return false;
        return material.shader.name is "Universal Render Pipeline/Lit" or "Universal Render Pipeline/Simple Lit" or "Standard" or "Unlit/BasicPBR";
    }
}
