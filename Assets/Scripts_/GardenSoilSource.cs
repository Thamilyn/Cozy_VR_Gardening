using System;
using UnityEngine;

/// <summary>The reusable soil supply in the existing SoilPot asset.</summary>
[DisallowMultipleComponent]
public sealed class GardenSoilSource : MonoBehaviour
{
    [SerializeField] private MeshFilter soilSurface;
    [SerializeField] private int soilMaterialIndex = -1;
    private Bounds surfaceBounds;

    public Material SoilMaterial { get; private set; }

    private void Awake()
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
                if (soilMaterialIndex < 0 &&
                    !materials[i].name.StartsWith("Soil", StringComparison.OrdinalIgnoreCase)) continue;
                soilSurface = candidate;
                soilMaterialIndex = i;
                SoilMaterial = materials[i];
                Bounds bounds = candidate.sharedMesh.GetSubMesh(i).bounds;
                Matrix4x4 matrix = transform.worldToLocalMatrix * candidate.transform.localToWorldMatrix;
                surfaceBounds = new Bounds(matrix.MultiplyPoint3x4(bounds.center), Vector3.zero);
                for (int corner = 0; corner < 8; corner++)
                    surfaceBounds.Encapsulate(matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((corner & 1) == 0 ? -1f : 1f,
                            (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f))));
                GardenObjectLabel.Create(gameObject, "SoilPot",
                    transform.TransformVector(Vector3.up * surfaceBounds.max.y) + Vector3.up * 0.1f);
                return;
            }
        }
        Debug.LogError("SoilPot needs a mesh with a Soil material for scooping.", this);
        enabled = false;
    }

    public bool ContainsScoopPoint(Vector3 worldPoint)
    {
        if (!isActiveAndEnabled || SoilMaterial == null || Vector3.Dot(transform.up, Vector3.up) < 0.5f) return false;
        Vector3 point = transform.InverseTransformPoint(worldPoint);
        float x = (point.x - surfaceBounds.center.x) / Mathf.Max(0.0001f, surfaceBounds.extents.x * 0.85f);
        float z = (point.z - surfaceBounds.center.z) / Mathf.Max(0.0001f, surfaceBounds.extents.z * 0.85f);
        Vector3 top = transform.TransformPoint(new Vector3(point.x, surfaceBounds.max.y, point.z));
        float height = Vector3.Dot(worldPoint - top, transform.up);
        return x * x + z * z <= 1f && height >= -0.08f && height <= 0.06f;
    }
}
