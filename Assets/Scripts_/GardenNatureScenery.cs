using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Uses the imported nature prefabs without modifying their meshes or materials on disk.</summary>
internal sealed class GardenNatureScenery
{
    private readonly Transform root;
    private readonly Dictionary<Material, Material> materials;
    private readonly Shader flowerLightingShader;
    private readonly Shader[] pastelNatureShaders;
    private readonly System.Random random = new(1927);

    private GardenNatureScenery(Transform root, Dictionary<Material, Material> materials, Shader flowerLightingShader, Shader[] pastelNatureShaders)
    {
        this.root = root;
        this.materials = materials;
        this.flowerLightingShader = flowerLightingShader;
        this.pastelNatureShaders = pastelNatureShaders;
    }

    public static void Create(Transform root, GameObject landmark, GameObject[] trees,
        GameObject[] shrubs, GameObject[] flowers, GameObject[] rocks,
        GameObject fence, Dictionary<Material, Material> materials, Shader flowerLightingShader, Shader[] pastelNatureShaders, Material pastelRockMaterial)
    {
        GardenNatureScenery scene = new(root, materials, flowerLightingShader, pastelNatureShaders);
        scene.Place(landmark, new Vector3(-2f, 0f, 18f), 11f, 0.012f);
        for (int i = 0; i < 20; i++)
        {
            float angle = i * Mathf.PI / 10f + scene.Range(-0.09f, 0.09f);
            float radius = scene.Range(16.5f, 20f);
            scene.Place(Pick(trees, i), new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius),
                scene.Range(5.8f, 8f), 0.035f);
        }
        scene.Scatter(shrubs, 12, 7f, 18f, 0.8f, 1.6f, 0.045f);
        scene.Scatter(rocks, 6, 7f, 17f, 0.45f, 0.9f, 0.045f, pastelRockMaterial);
        scene.Scatter(flowers, 96, 5f, 14f, 0.28f, 0.48f, 0.008f);
        for (int i = 0; i < 6; i++)
        {
            int group = i / 2;
            float t = 0.22f + group * 0.27f + (i % 2) * 0.06f;
            GardenLandscapeMesh.SamplePath(t, out Vector3 centre, out Vector3 tangent, out float halfWidth);
            Vector3 side = new(tangent.z, 0f, -tangent.x);
            Vector3 position = centre + side * (halfWidth + 0.45f) * (group == 1 ? 1f : -1f);
            float yaw = Mathf.Atan2(-tangent.z, tangent.x) * Mathf.Rad2Deg;
            scene.Place(fence, position, 0.60f, 0.035f, yaw);
        }
    }

    private void Scatter(GameObject[] prefabs, int count, float inner, float outer,
        float minHeight, float maxHeight, float cullHeight, Material overrideMaterial = null)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = Range(0f, Mathf.PI * 2f);
            float radius = Mathf.Sqrt(Range(inner * inner, outer * outer));
            Vector3 point = new(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            // Keep an open corridor around Gronny, the table and the player's starting position.
            if (Mathf.Abs(point.x) < 4f && point.z > -8f && point.z < 4f)
                point.x = point.x < 0f ? -5f : 5f;
            if (point.z >= 4f && point.z <= 18f && Mathf.Abs(point.x - GardenLandscapeMesh.PathCentre(point.z)) < 1.3f)
                point.x += 2.6f;
            Place(Pick(prefabs, i), point, Range(minHeight, maxHeight), cullHeight, overrideMaterial: overrideMaterial);
        }
    }

    private void Place(GameObject prefab, Vector3 point, float targetHeight, float cullHeight, float? yaw = null, Material overrideMaterial = null)
    {
        if (prefab == null) return;
        GameObject instance = Object.Instantiate(prefab, root);
        instance.name = "Nature - " + prefab.name;
        instance.hideFlags = HideFlags.HideAndDontSave;
        instance.transform.localPosition = point;
        instance.transform.localRotation = Quaternion.Euler(0f, yaw ?? Range(0f, 360f), 0f);
        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        // Some pack variants are flat flower heads: never expand those into metre-wide carpets.
        float heightScale = targetHeight / Mathf.Max(0.01f, bounds.size.y);
        float widthScale = targetHeight * 2.5f / Mathf.Max(0.01f, Mathf.Max(bounds.size.x, bounds.size.z));
        instance.transform.localScale *= Mathf.Min(heightScale, widthScale);
        bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        instance.transform.position += Vector3.up * (root.position.y - bounds.min.y);

        foreach (Renderer renderer in renderers)
        {
            // Vertex wind moves only the drawing; keep a little culling clearance around the mesh.
            Bounds localBounds = renderer.localBounds;
            localBounds.Expand(0.12f / Mathf.Max(0.001f, instance.transform.lossyScale.x));
            renderer.localBounds = localBounds;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            Material[] shared = renderer.sharedMaterials;
            for (int i = 0; i < shared.Length; i++) shared[i] = overrideMaterial != null ? overrideMaterial : PrepareMaterial(shared[i]);
            renderer.sharedMaterials = shared;
        }
        if (instance.GetComponentInChildren<LODGroup>() == null)
        {
            LODGroup lod = instance.AddComponent<LODGroup>();
            lod.fadeMode = LODFadeMode.None;
            lod.SetLODs(new[] { new LOD(cullHeight, renderers) });
            lod.RecalculateBounds();
        }
    }

    private Material PrepareMaterial(Material source)
    {
        if (source == null) return null;
        if (materials.TryGetValue(source, out Material prepared)) return prepared;
        prepared = new Material(source)
        {
            name = source.name + " (Garden scenery)",
            hideFlags = HideFlags.HideAndDontSave,
            enableInstancing = true
        };
        GardenNatureStyle.Apply(prepared, flowerLightingShader, pastelNatureShaders);
        materials.Add(source, prepared);
        return prepared;
    }

    private float Range(float low, float high) => Mathf.Lerp(low, high, (float)random.NextDouble());
    private static GameObject Pick(GameObject[] prefabs, int index) =>
        prefabs == null || prefabs.Length == 0 ? null : prefabs[index % prefabs.Length];
}
