using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Visual-only shovel and soil, driven by the tutorial clock rather than gameplay.</summary>
internal sealed class GardenSoilDemonstration : IDisposable
{
    private static readonly Vector3 LocalScoopPoint = new(0f, 0.019f, 0.195f);
    private readonly int pourStep;
    private readonly Transform visuals;
    private readonly Transform shovel;
    private readonly Transform scoop;
    private readonly GameObject load;
    private readonly GameObject seed;
    private readonly GameObject cover;
    private readonly Transform[] grains = new Transform[6];
    private readonly Vector3 landingPoint;
    private readonly Material soilMaterial;
    private readonly Material seedMaterial;

    public Vector3 GripPoint => shovel.TransformPoint(new Vector3(0f, 0.019f, 0f));

    public GardenSoilDemonstration(GardenControlDemo kind, Transform visuals, Transform motion,
        GronnyAudioCatalogue catalogue, Material symbolMaterial)
    {
        this.visuals = visuals;
        pourStep = kind == GardenControlDemo.SoilScoop ? 3 : 1;
        soilMaterial = new Material(symbolMaterial);
        soilMaterial.SetColor("_BaseColor", new Color(0.38f, 0.21f, 0.09f));
        seedMaterial = new Material(symbolMaterial);
        seedMaterial.SetColor("_BaseColor", new Color(0.88f, 0.67f, 0.3f));

        if (kind == GardenControlDemo.SoilScoop)
            PlaceProp(catalogue.soilSourceVisual, "Example soil supply", visuals,
                new Vector3(-0.14f, -0.08f, 0.04f), 0.14f);
        Transform pot = PlaceProp(catalogue.soilPotVisual, "Example planted pot", visuals,
            new Vector3(0.14f, -0.10f, 0.04f), 0.14f);
        Bounds potBounds = ReadBounds(pot, visuals);
        landingPoint = new Vector3(potBounds.center.x, potBounds.max.y - 0.012f, potBounds.center.z);
        seed = Symbol("Example uncovered seed", visuals, landingPoint + Vector3.up * 0.008f,
            new Vector3(0.015f, 0.009f, 0.012f), seedMaterial);
        cover = Symbol("Example soil cover", visuals, landingPoint + Vector3.up * 0.007f,
            new Vector3(0.07f, 0.014f, 0.07f), soilMaterial);

        // Copy only the existing blade's render data. Its gardening Awake and Meta
        // grabbables must never run on the example.
        shovel = CopyVisuals(catalogue.shovelVisual.transform, motion);
        shovel.name = "Example shovel";
        shovel.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));
        Bounds bladeBounds = ReadBounds(shovel, motion);
        float longest = Mathf.Max(bladeBounds.size.x, bladeBounds.size.y, bladeBounds.size.z);
        if (longest > 0.000001f) shovel.localScale *= 0.27f / longest;
        var scoopObject = new GameObject("Example blade scoop point");
        scoop = scoopObject.transform;
        scoop.SetParent(shovel, false);
        scoop.localPosition = LocalScoopPoint;
        // Keep the blade over the authored pot positions while the hand stays on
        // the handle. Rolling around this long axis tips the face without digging.
        shovel.localPosition += new Vector3(0.15f, 0f, 0.04f) - motion.InverseTransformPoint(scoop.position);
        load = Symbol("Example carried soil", scoop, new Vector3(0f, 0.013f, 0f),
            new Vector3(0.06f, 0.025f, 0.07f) / Mathf.Max(0.0001f, shovel.localScale.x), soilMaterial);
        for (int i = 0; i < grains.Length; i++)
            grains[i] = Symbol("Example falling soil " + i, visuals, Vector3.zero,
                Vector3.one * 0.009f, soilMaterial).transform;
        Tick(0, 0f, 0.35f);
    }

    public void Tick(int step, float elapsed, float blendSeconds)
    {
        // Scoop: approach, short stroke, carry, tilt, finish. Pour starts loaded.
        bool collected = pourStep == 1 || step >= 1;
        float poured = step > pourStep ? 1f : step == pourStep
            ? Mathf.Clamp01((elapsed - blendSeconds) / 0.7f) : 0f;
        load.SetActive(collected && poured < 1f);
        seed.SetActive(poured < 1f);
        cover.SetActive(poured >= 1f);
        Vector3 start = visuals.InverseTransformPoint(scoop.position);
        for (int i = 0; i < grains.Length; i++)
        {
            // Stagger six reusable grains; no particles, physics or new per-frame objects.
            float fall = poured * 1.8f - i * 0.13f;
            grains[i].gameObject.SetActive(collected && poured > 0f && poured < 1f && fall >= 0f && fall <= 1f);
            Vector3 spread = new((i % 3 - 1) * 0.012f, 0f, (i / 3 - 0.5f) * 0.016f);
            grains[i].localPosition = Vector3.Lerp(start, landingPoint + spread, Mathf.Clamp01(fall * fall));
        }
    }

    private Transform PlaceProp(GameObject prefab, string name, Transform parent, Vector3 position, float size)
    {
        Transform prop = CopyVisuals(prefab.transform, parent);
        prop.name = name;
        prop.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        Bounds bounds = ReadBounds(prop, parent);
        float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (longest > 0.000001f) prop.localScale *= size / longest;
        bounds = ReadBounds(prop, parent);
        prop.localPosition += position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        return prop;
    }

    private Transform CopyVisuals(Transform source, Transform parent)
    {
        var copy = new GameObject("Example " + source.name);
        copy.layer = 2;
        Transform result = copy.transform;
        result.SetParent(parent, false);
        result.SetLocalPositionAndRotation(source.localPosition, source.localRotation);
        result.localScale = source.localScale;
        MeshFilter mesh = source.GetComponent<MeshFilter>();
        MeshRenderer renderer = source.GetComponent<MeshRenderer>();
        if (mesh != null && renderer != null)
        {
            copy.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            var visual = copy.AddComponent<MeshRenderer>();
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                // The raw pot asset has a red placeholder. Its normal gardening
                // component is deliberately absent here, so tint only copied soil.
                string materialName = materials[i] != null ? materials[i].name : string.Empty;
                if (source.GetComponent<GardenSoil>() != null ||
                    materialName.Equals("Soil", StringComparison.OrdinalIgnoreCase) ||
                    materialName.StartsWith("Soil.", StringComparison.OrdinalIgnoreCase))
                    materials[i] = soilMaterial;
            }
            visual.sharedMaterials = materials;
            visual.enabled = renderer.enabled;
            visual.shadowCastingMode = ShadowCastingMode.Off;
            visual.receiveShadows = false;
        }
        foreach (Transform child in source)
            if (child.gameObject.activeSelf) CopyVisuals(child, result);
        return result;
    }

    private static Bounds ReadBounds(Transform root, Transform space)
    {
        Bounds bounds = default;
        bool found = false;
        foreach (MeshFilter mesh in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mesh.sharedMesh == null) continue;
            if (!mesh.GetComponent<MeshRenderer>().enabled) continue;
            Bounds local = mesh.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                point = space.InverseTransformPoint(mesh.transform.TransformPoint(point));
                if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                else bounds.Encapsulate(point);
            }
        }
        return bounds;
    }

    private static GameObject Symbol(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject symbol = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        symbol.name = name;
        symbol.layer = 2;
        symbol.transform.SetParent(parent, false);
        symbol.transform.localPosition = position;
        symbol.transform.localScale = scale;
        Collider collider = symbol.GetComponent<Collider>();
        collider.enabled = false;
        Object.Destroy(collider);
        var renderer = symbol.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return symbol;
    }

    public void Dispose()
    {
        Object.Destroy(soilMaterial);
        Object.Destroy(seedMaterial);
    }
}
