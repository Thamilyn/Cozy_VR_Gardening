using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Non-interactive harvest examples, driven only by the tutorial animation clock.</summary>
internal sealed class GardenHarvestDemonstration : IDisposable
{
    private readonly GardenControlDemo kind;
    private readonly Transform tomato;
    private readonly GameObject stem;
    private readonly Material fruitMaterial;
    private readonly Material stemMaterial;
    private readonly Vector3 restingFruit = new(0.09f, 0.02f, 0f);
    private Transform bottomBlade;
    private Transform topBlade;
    private Quaternion bottomOpen;
    private Quaternion topOpen;
    private float bottomSign;
    private float topSign;

    public GardenHarvestDemonstration(GardenControlDemo kind, Transform visuals, Transform motion,
        GameObject shearsPrefab, Material symbolMaterial)
    {
        this.kind = kind;
        fruitMaterial = new Material(symbolMaterial);
        fruitMaterial.SetColor("_BaseColor", new Color(0.9f, 0.13f, 0.08f));
        stemMaterial = new Material(symbolMaterial);
        stemMaterial.SetColor("_BaseColor", new Color(0.18f, 0.55f, 0.2f));
        tomato = MakeSymbol(PrimitiveType.Sphere, "Example ripe fruit", visuals, restingFruit,
            Vector3.one * TomatoFruit.DiameterMetres, fruitMaterial).transform;
        if (kind != GardenControlDemo.CutStem) return;
        Vector3 cuttingPoint = restingFruit + Vector3.up * 0.052f;
        stem = MakeSymbol(PrimitiveType.Cylinder, "Example stem", visuals,
            cuttingPoint - Vector3.up * 0.006f, new Vector3(0.005f, 0.014f, 0.005f), stemMaterial);
        if (shearsPrefab == null)
        {
            Debug.LogWarning("Assign the existing Pruner visual to Gronny's harvest demonstration.");
            return;
        }
        Transform shears = CopyVisuals(shearsPrefab.transform, motion);
        shears.localPosition = Vector3.zero;
        shears.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        Bounds bounds = ReadBounds(shears, motion);
        float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (longest > 0.000001f) shears.localScale *= 0.21f / longest;
        if (bottomBlade == null || topBlade == null) return;
        bottomOpen = bottomBlade.localRotation;
        topOpen = topBlade.localRotation;
        Bounds bottom = bottomBlade.GetComponent<MeshFilter>().sharedMesh.bounds;
        Bounds top = topBlade.GetComponent<MeshFilter>().sharedMesh.bounds;
        bottomSign = Mathf.Sign(bottom.center.x);
        topSign = Mathf.Sign(top.center.x);
        Transform model = bottomBlade.parent;
        float reach = Mathf.Min(
            model.InverseTransformPoint(bottomBlade.TransformPoint(Vector3.forward * bottom.max.z)).z,
            model.InverseTransformPoint(topBlade.TransformPoint(Vector3.forward * top.max.z)).z);
        Vector3 edgeCentre = new Vector3(-bottomSign * 0.125f, 0f, 0.575f) * reach;
        shears.localPosition += cuttingPoint - motion.InverseTransformPoint(model.TransformPoint(edgeCentre));
    }

    public void Tick(float press, Vector3 grip)
    {
        press = Mathf.Clamp01(press);
        if (kind == GardenControlDemo.PickTomato)
        {
            tomato.localPosition = Vector3.Lerp(restingFruit, grip, press);
            return;
        }
        if (bottomBlade != null)
            bottomBlade.localRotation = bottomOpen * Quaternion.AngleAxis(bottomSign * 22f * press, Vector3.up);
        if (topBlade != null)
            topBlade.localRotation = topOpen * Quaternion.AngleAxis(topSign * 22f * press, Vector3.up);
        float severed = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 1f, press));
        if (stem != null) stem.SetActive(severed < 0.95f);
        tomato.localPosition = restingFruit + Vector3.down * (0.025f * severed);
    }

    private Transform CopyVisuals(Transform source, Transform parent)
    {
        // Copy render data, never Instantiate the interactive prefab: Awake on its
        // gardening components would otherwise create new grab/cutting interactions.
        var copy = new GameObject("Example " + source.name);
        copy.layer = 2;
        Transform result = copy.transform;
        result.SetParent(parent, false);
        result.SetLocalPositionAndRotation(source.localPosition, source.localRotation);
        result.localScale = source.localScale;
        MeshFilter sourceMesh = source.GetComponent<MeshFilter>();
        MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
        if (sourceMesh != null && sourceRenderer != null)
        {
            copy.AddComponent<MeshFilter>().sharedMesh = sourceMesh.sharedMesh;
            MeshRenderer renderer = copy.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = sourceRenderer.sharedMaterials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (source.name == "BottomPart") bottomBlade = result;
            if (source.name == "TopPart") topBlade = result;
        }
        foreach (Transform child in source) CopyVisuals(child, result);
        return result;
    }

    private static Bounds ReadBounds(Transform root, Transform space)
    {
        Bounds bounds = default;
        bool found = false;
        foreach (MeshFilter mesh in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mesh.sharedMesh == null) continue;
            Bounds local = mesh.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                point = space.InverseTransformPoint(mesh.transform.TransformPoint(point));
                if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                else bounds.Encapsulate(point);
            }
        }
        return bounds;
    }

    private static GameObject MakeSymbol(PrimitiveType shape, string name, Transform parent,
        Vector3 position, Vector3 scale, Material material)
    {
        GameObject symbol = GameObject.CreatePrimitive(shape);
        symbol.name = name;
        symbol.layer = 2;
        symbol.transform.SetParent(parent, false);
        symbol.transform.localPosition = position;
        symbol.transform.localScale = scale;
        Collider collider = symbol.GetComponent<Collider>();
        collider.enabled = false;
        Object.Destroy(collider);
        Renderer renderer = symbol.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return symbol;
    }

    public void Dispose()
    {
        Object.Destroy(fruitMaterial);
        Object.Destroy(stemMaterial);
    }
}
