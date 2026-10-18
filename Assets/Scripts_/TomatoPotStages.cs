using System;
using UnityEngine;

/// <summary>Replaces a small pot's appearance before planting and through its six crop stages.</summary>
[DisallowMultipleComponent]
public  class TomatoPotStages : MonoBehaviour
{
    [Tooltip("Empty pot, then the six planted tomato appearances in calendar order.")]
    [SerializeField] private GameObject[] stagePrefabs = new GameObject[7];
    [SerializeField] private MeshFilter substrateSurface;
    [SerializeField] private GardenSoil soil;
    [SerializeField] private GameObject emptyPotVisual;
    [SerializeField] private BoxCollider potCollider;

    private Renderer[] originalRenderers;
    private GameObject currentVisual;
    private int currentIndex = -1;

    public GameObject CurrentVisual => currentVisual;

    private void Awake()
    {
        TryShowVisual(0, out _);
    }
    // public enum GrowthStage { Seed = 0, Sprout = 1, Mature = 2, YoungPlant = 3, Flowering = 4, GreenFruit = 5 }

    public bool TryShowStage(GrowthStage stage, out GameObject visual)
    {
        int index = stage switch
        {
            GrowthStage.Sprout => 1,
            GrowthStage.YoungPlant => 2,
            GrowthStage.Flowering => 4,
            GrowthStage.GreenFruit => 5,
            GrowthStage.Mature => 6,
            _ => 0
        };
        return TryShowVisual(index, out visual);
    }

    private bool TryShowVisual(int index, out GameObject visual)
    {
        visual = null;
        if (stagePrefabs == null || index >= stagePrefabs.Length || stagePrefabs[index] == null ||
            substrateSurface == null || substrateSurface.sharedMesh == null)
        {
            Debug.LogError("Missing tomato pot stage or planting surface at slot " + index, this);
            return false;
        }
        if (currentIndex == index && currentVisual != null)
        {
            visual = currentVisual;
            return true;
        }
        if (originalRenderers == null)
        {
            originalRenderers = GetComponentsInChildren<Renderer>(true);
            currentVisual = emptyPotVisual;
            if (potCollider != null)
            {
                if (emptyPotVisual != null) FitPotCollider(emptyPotVisual);
                foreach (Collider collider in GetComponentsInChildren<Collider>(true))
                    if (collider != potCollider && !collider.isTrigger &&
                        collider.gameObject != substrateSurface.gameObject) collider.enabled = false;
            }
        }

        // A phase includes the whole pot. Its owner is the pot's physics root, not the seed Rigidbody.
        GameObject next = Instantiate(stagePrefabs[index], transform);
        next.name = stagePrefabs[index].name;
        next.transform.localPosition = Vector3.zero;
        next.transform.localRotation = Quaternion.identity;
        if (!TryFindSoil(next, out MeshRenderer soilRenderer, out Bounds soilBounds, out int materialIndex))
        {
            Debug.LogError("Tomato stage needs a mesh with a Soil material: " + next.name, this);
            RemoveVisual(next);
            return false;
        }

        AlignSoil(next.transform, soilRenderer.transform, soilBounds);
        if (index == 0) FitPotCollider(next);
        // The original pot retains its planting surface and physics. Stage models are visual only.
        foreach (Collider collider in next.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Renderer renderer in originalRenderers)
            if (renderer != null) renderer.enabled = false;
        if (currentVisual != null) RemoveVisual(currentVisual);
        currentVisual = next;
        currentIndex = index;
        if (soil != null) soil.SetVisualSurface(soilRenderer, materialIndex);
        visual = next;
        return true;
    }

    private static void RemoveVisual(GameObject visual)
    {
        visual.SetActive(false);
        if (Application.isPlaying) Destroy(visual);
        else DestroyImmediate(visual);
    }

    private static bool TryFindSoil(GameObject visual, out MeshRenderer renderer,
        out Bounds bounds, out int materialIndex)
    {
        foreach (MeshRenderer candidate in visual.GetComponentsInChildren<MeshRenderer>(true))
        {
            MeshFilter filter = candidate.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) continue;
            Material[] materials = candidate.sharedMaterials;
            for (int i = 0; i < materials.Length && i < filter.sharedMesh.subMeshCount; i++)
            {
                if (materials[i] == null || !materials[i].name.StartsWith("Soil", StringComparison.OrdinalIgnoreCase)) continue;
                renderer = candidate;
                bounds = filter.sharedMesh.GetSubMesh(i).bounds;
                materialIndex = i;
                return true;
            }
        }
        renderer = null;
        bounds = default;
        materialIndex = -1;
        return false;
    }

    private void AlignSoil(Transform visual, Transform source, Bounds sourceBounds)
    {
        Bounds target = InPotSpace(substrateSurface.transform, substrateSurface.sharedMesh.bounds);
        Bounds current = InPotSpace(source, sourceBounds);
        if (current.size.x > 0.000001f && current.size.z > 0.000001f)
            visual.localScale *= Mathf.Min(target.size.x / current.size.x, target.size.z / current.size.z);

        // Keep the base on the same collider while anchoring the soil, even when model pivots differ.
        current = InPotSpace(source, sourceBounds);
        if (potCollider != null && TryGetVisualBounds(visual.gameObject, out Bounds bodyBounds))
        {
            float soilAboveBase = current.max.y - bodyBounds.min.y;
            float targetBase = potCollider.center.y - potCollider.size.y * 0.5f;
            if (soilAboveBase > 0.000001f && target.max.y > targetBase)
            {
                Vector3 scale = visual.localScale;
                scale.y *= (target.max.y - targetBase) / soilAboveBase;
                visual.localScale = scale;
            }
        }

        // Imported stages have different pivots; anchor their soil to the existing watering surface.
        current = InPotSpace(source, sourceBounds);
        Vector3 offset = new(target.center.x - current.center.x,
            target.max.y - current.max.y, target.center.z - current.center.z);
        visual.position += transform.TransformVector(offset);
    }

    private void FitPotCollider(GameObject visual)
    {
        if (potCollider == null || !TryGetVisualBounds(visual, out Bounds bounds)) return;
        potCollider.center = bounds.center;
        potCollider.size = bounds.size;
    }

    private bool TryGetVisualBounds(GameObject visual, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (MeshFilter filter in visual.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null) continue;
            Bounds part = InPotSpace(filter.transform, filter.sharedMesh.bounds);
            if (!found) bounds = part;
            else bounds.Encapsulate(part);
            found = true;
        }
        return found;
    }

    private Bounds InPotSpace(Transform source, Bounds bounds)
    {
        Matrix4x4 matrix = transform.worldToLocalMatrix * source.localToWorldMatrix;
        Bounds result = new(matrix.MultiplyPoint3x4(bounds.center), Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
            result.Encapsulate(matrix.MultiplyPoint3x4(corner));
        }
        return result;
    }

    public static Transform FindRipeFruit(GameObject visual)
    {
        Transform largest = null;
        float largestVolume = 0f;
        foreach (MeshRenderer renderer in visual.GetComponentsInChildren<MeshRenderer>())
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || material.name.IndexOf("Tomato", StringComparison.OrdinalIgnoreCase) < 0) continue;
                Vector3 size = renderer.bounds.size;
                float volume = size.x * size.y * size.z;
                if (volume > largestVolume)
                {
                    largest = renderer.transform;
                    largestVolume = volume;
                }
                break;
            }
        }
        return largest;
    }
}
