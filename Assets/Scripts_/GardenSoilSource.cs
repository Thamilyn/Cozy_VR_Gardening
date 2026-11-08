using System;
using UnityEngine;

/// <summary>The reusable soil supply in the existing SoilPot asset.</summary>
[DisallowMultipleComponent]
public sealed class GardenSoilSource : MonoBehaviour
{
    [SerializeField] private MeshFilter soilSurface;
    [SerializeField] private int soilMaterialIndex = -1;
    [SerializeField] private bool showNameLabel = true;
    [SerializeField, Min(0.02f), Tooltip("Reachable space above the soil; the blade need not penetrate the container.")]
    private float scoopHeightAboveSoil = 0.14f;
    [SerializeField, Min(0.01f)] private float scoopDepthBelowSoil = 0.08f;
    private Bounds surfaceBounds;
    private GardenObjectLabel nameLabel;

    public Material SoilMaterial { get; private set; }

    private void Awake()
    {
        if (!TryResolveSurface())
        {
            Debug.LogError("SoilPot needs a mesh with a Soil material for scooping.", this);
            enabled = false;
            return;
        }
        if (showNameLabel)
            nameLabel = GardenObjectLabel.Create(gameObject, "Soil Pot",
                transform.TransformVector(Vector3.up * surfaceBounds.max.y) + Vector3.up * 0.1f);
    }

    internal void RecordScoop()
    {
        if (nameLabel != null) nameLabel.enabled = false;
    }

    private bool TryResolveSurface()
    {
        foreach (MeshFilter candidate in GetComponentsInChildren<MeshFilter>(true))
        {
            if (soilSurface != null && candidate != soilSurface) continue;
            if (candidate.sharedMesh == null || !candidate.TryGetComponent(out MeshRenderer renderer)) continue;
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length && i < candidate.sharedMesh.subMeshCount; i++)
            {
                if (materials[i] == null) continue;
                if (soilMaterialIndex >= 0 && i != soilMaterialIndex) continue;
                if (soilMaterialIndex < 0 && !IsSoilMaterial(materials[i].name)) continue;
                soilSurface = candidate;
                SoilMaterial = materials[i];
                Bounds bounds = candidate.sharedMesh.GetSubMesh(i).bounds;
                Matrix4x4 matrix = transform.worldToLocalMatrix * candidate.transform.localToWorldMatrix;
                surfaceBounds = new Bounds(matrix.MultiplyPoint3x4(bounds.center), Vector3.zero);
                for (int corner = 0; corner < 8; corner++)
                    surfaceBounds.Encapsulate(matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((corner & 1) == 0 ? -1f : 1f,
                            (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f))));
                return true;
            }
        }
        return false;
    }

    private static bool IsSoilMaterial(string materialName)
    {
        // The imported mesh has both "SoilPot" (the container) and "Soil.001".
        // A plain StartsWith("Soil") selects the container's material and bounds.
        return materialName.Equals("Soil", StringComparison.OrdinalIgnoreCase) ||
            materialName.StartsWith("Soil.", StringComparison.OrdinalIgnoreCase) ||
            materialName.StartsWith("Soil_", StringComparison.OrdinalIgnoreCase) ||
            materialName.StartsWith("Soil ", StringComparison.OrdinalIgnoreCase);
    }

    public bool ContainsScoopPoint(Vector3 worldPoint, float bladeRadius = 0f)
    {
        return CanSupplySoil && ScoopBounds(bladeRadius).Contains(transform.InverseTransformPoint(worldPoint));
    }

    public bool IntersectsScoopPath(Vector3 from, Vector3 to, float bladeRadius)
    {
        if (!CanSupplySoil) return false;
        Bounds bounds = ScoopBounds(bladeRadius);
        Vector3 start = transform.InverseTransformPoint(from);
        Vector3 end = transform.InverseTransformPoint(to);
        if (bounds.Contains(start) || bounds.Contains(end)) return true;
        Vector3 delta = end - start;
        float distance = delta.magnitude;
        // Test the swept blade as well, so a short, quick scoop is not missed between frames.
        return distance > 0f && bounds.IntersectRay(new Ray(start, delta / distance), out float hit) && hit <= distance;
    }

    private bool CanSupplySoil => isActiveAndEnabled && SoilMaterial != null &&
        Vector3.Dot(transform.up, Vector3.up) >= 0.5f;

    private Bounds ScoopBounds(float bladeRadius)
    {
        float xScale = Mathf.Max(0.0001f, transform.TransformVector(Vector3.right).magnitude);
        float yScale = Mathf.Max(0.0001f, transform.TransformVector(Vector3.up).magnitude);
        float zScale = Mathf.Max(0.0001f, transform.TransformVector(Vector3.forward).magnitude);
        float radius = Mathf.Max(0f, bladeRadius);
        Vector3 min = new(surfaceBounds.center.x - surfaceBounds.extents.x * 0.95f - radius / xScale,
            surfaceBounds.max.y - scoopDepthBelowSoil / yScale,
            surfaceBounds.center.z - surfaceBounds.extents.z * 0.95f - radius / zScale);
        Vector3 max = new(surfaceBounds.center.x + surfaceBounds.extents.x * 0.95f + radius / xScale,
            surfaceBounds.max.y + scoopHeightAboveSoil / yScale,
            surfaceBounds.center.z + surfaceBounds.extents.z * 0.95f + radius / zScale);
        Bounds bounds = new();
        bounds.SetMinMax(min, max);
        return bounds;
    }
}
