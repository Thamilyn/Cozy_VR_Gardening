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
        Mesh mesh = filter.sharedMesh;
        Vector3 point = fruitBounds.center;
        float farthest = 0f;
        if (!mesh.isReadable) return point;
        Vector3[] vertices = mesh.vertices;
        Material[] materials = renderer.sharedMaterials;
        // RootswithTomatoes includes a calyx and a short stalk. Its farthest stalk
        // vertex marks the connection to the plant, independently of the imported pivot.
        for (int i = 0; i < mesh.subMeshCount && i < materials.Length; i++)
        {
            if (IsFruitMaterial(materials[i])) continue;
            foreach (int index in mesh.GetTriangles(i))
            {
                float distance = (vertices[index] - fruitBounds.center).sqrMagnitude;
                if (distance <= farthest) continue;
                farthest = distance;
                point = vertices[index];
            }
        }
        return point;
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
