using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Sizes imported tomato models while preserving their stalks and original materials.</summary>
public sealed class TomatoFruitMesh : MonoBehaviour
{
    public const string HarvestModelName = "RootswithTomatoes.036";
    private Mesh ownedMesh;

    public static void NormalizeFruitModels(GameObject visual)
    {
        foreach (MeshFilter filter in visual.GetComponentsInChildren<MeshFilter>(true))
        {
            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || !filter.name.StartsWith("RootswithTomatoes", StringComparison.Ordinal) ||
                !TryGetFruitBounds(filter, renderer, out Bounds bounds)) continue;
            Vector3 size = Vector3.Scale(bounds.size, filter.transform.lossyScale);
            float diameter = Mathf.Max(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            if (diameter > TomatoFruit.DiameterMetres)
            {
                Vector3 attachment = GetAttachmentPoint(filter, renderer, bounds);
                Vector3 worldAttachment = filter.transform.TransformPoint(attachment);
                filter.transform.localScale *= TomatoFruit.DiameterMetres / diameter;
                filter.transform.position += worldAttachment - filter.transform.TransformPoint(attachment);
            }
        }
    }

    public static Transform PrepareHarvestModel(GameObject visual)
    {
        NormalizeFruitModels(visual);
        foreach (MeshFilter filter in visual.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.name != HarvestModelName) continue;
            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || !TryGetFruitBounds(filter, renderer, out Bounds bounds)) continue;
            // Put the physics origin at the fruit's center, keeping the imported model
            // intact underneath it. Basket delivery now measures the fruit, not its stalk.
            GameObject root = new("Harvestable tomato (" + HarvestModelName + ")");
            root.transform.position = filter.transform.TransformPoint(bounds.center);
            root.transform.SetParent(visual.transform, true);
            filter.transform.SetParent(root.transform, true);
            return root.transform;
        }
        Debug.LogError("Missing ripe tomato model " + HarvestModelName, visual);
        return null;
    }

    public static bool IsFruitMaterial(Material material)
    {
        return material != null && material.name.IndexOf("Tomato", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static Vector3 GetAttachmentPoint(MeshFilter filter, MeshRenderer renderer, Bounds fruitBounds)
    {
        return TryGetAttachmentPoint(filter, renderer, fruitBounds, out Vector3 point)
            ? point : fruitBounds.center;
    }

    public static bool TryGetAttachmentPoint(MeshFilter filter, MeshRenderer renderer, Bounds fruitBounds,
        out Vector3 point)
    {
        Mesh mesh = filter.sharedMesh;
        point = fruitBounds.center;
        // Never use the fruit's centre as a cutting target when stalk geometry is unavailable.
        if (!mesh.isReadable) return false;
        Vector3[] vertices = mesh.vertices;
        Material[] materials = renderer.sharedMaterials;
        var stalkIndices = new HashSet<int>();
        for (int i = 0; i < mesh.subMeshCount && i < materials.Length; i++)
        {
            if (materials[i] == null || IsFruitMaterial(materials[i])) continue;
            foreach (int index in mesh.GetTriangles(i)) stalkIndices.Add(index);
        }
        if (stalkIndices.Count == 0) return false;

        Vector3 stalkCentre = Vector3.zero;
        foreach (int index in stalkIndices) stalkCentre += vertices[index];
        stalkCentre /= stalkIndices.Count;
        Vector3 axis = stalkCentre - fruitBounds.center;
        if (axis.sqrMagnitude < 0.00000001f) return false;
        axis.Normalize();

        // The calyx leaves extend farther sideways than the short stalk. Radial
        // distance therefore targets a leaf tip. Project along the fruit-to-stalk
        // axis instead, then centre the terminal ring rather than picking one edge.
        float farthest = float.NegativeInfinity;
        foreach (int index in stalkIndices)
            farthest = Mathf.Max(farthest, Vector3.Dot(vertices[index] - fruitBounds.center, axis));
        float tipDepth = fruitBounds.size.magnitude * 0.02f;
        if (farthest <= tipDepth) return false;
        Vector3 tip = Vector3.zero;
        int tipCount = 0;
        foreach (int index in stalkIndices)
        {
            if (Vector3.Dot(vertices[index] - fruitBounds.center, axis) < farthest - tipDepth) continue;
            tip += vertices[index];
            tipCount++;
        }
        point = tip / tipCount;
        return true;
    }

    public static bool TryGetFruitBounds(MeshFilter filter, MeshRenderer renderer, out Bounds bounds)
    {
        bounds = default;
        if (filter == null || filter.sharedMesh == null || renderer == null) return false;
        Mesh mesh = filter.sharedMesh;
        Material[] materials = renderer.sharedMaterials;
        bool found = false;
        for (int i = 0; i < mesh.subMeshCount && i < materials.Length; i++)
        {
            if (!IsFruitMaterial(materials[i])) continue;
            Bounds part = mesh.GetSubMesh(i).bounds;
            if (!found) bounds = part;
            else bounds.Encapsulate(part);
            found = true;
        }
        return found;
    }

    public static Mesh CreateOutlineMesh(GameObject shell, MeshFilter filter, MeshRenderer renderer)
    {
        Mesh source = filter.sharedMesh;
        if (source.subMeshCount == 1) return source;
        if (!source.isReadable)
        {
            Debug.LogError("Enable Read/Write on the ripe tomato model for its outline.", filter);
            return null;
        }
        List<int> triangles = new();
        Material[] materials = renderer.sharedMaterials;
        for (int i = 0; i < source.subMeshCount && i < materials.Length; i++)
            if (IsFruitMaterial(materials[i])) triangles.AddRange(source.GetTriangles(i));
        Mesh outline = Instantiate(source);
        outline.name = HarvestModelName + " fruit outline";
        outline.subMeshCount = 1;
        outline.SetTriangles(triangles, 0);
        shell.AddComponent<TomatoFruitMesh>().ownedMesh = outline;
        return outline;
    }

    private void OnDestroy()
    {
        if (ownedMesh == null) return;
        if (Application.isPlaying) Destroy(ownedMesh);
        else DestroyImmediate(ownedMesh);
    }
}
