using UnityEngine;

/// <summary>A scene pot with one planting position. The point follows a movable pot.</summary>
[DisallowMultipleComponent]
public sealed class GardenPot : MonoBehaviour
{
    [SerializeField] private string potId;
    [SerializeField] private Vector3 localPlantingPoint;
    [SerializeField, Min(0.01f)] private float plantingRadius = 0.16f;
    [SerializeField, Min(0.01f)] private float plantingHeight = 0.25f;
    [SerializeField] private bool acceptsTomato = true;
    [Tooltip("Actual top of the soil. Resolved from GardenSoil when not assigned.")]
    [SerializeField] private MeshFilter substrateSurface;
    [Tooltip("Optional measured area in square metres; zero uses the soil mesh and world scale.")]
    [SerializeField, Min(0f)] private float substrateAreaOverride;

    public string PotId => potId;
    public Vector3 PlantingPoint => transform.TransformPoint(localPlantingPoint);
    public Vector3 LocalPlantingPoint => localPlantingPoint;
    public bool AcceptsTomato => acceptsTomato;
    public float SubstrateAreaSquareMetres
    {
        get
        {
            ResolveSurface();
            if (substrateAreaOverride > 0f) return substrateAreaOverride;
            if (substrateSurface == null || substrateSurface.sharedMesh == null) return 0f;
            Vector3 size = substrateSurface.sharedMesh.bounds.size;
            Vector3 across = substrateSurface.transform.TransformVector(Vector3.right * size.x);
            Vector3 along = substrateSurface.transform.TransformVector(Vector3.forward * size.z);
            return Vector3.Cross(across, along).magnitude;
        }
    }

    private void ResolveSurface()
    {
        if (substrateSurface != null) return;
        GardenSoil soil = GetComponentInChildren<GardenSoil>(true);
        if (soil != null) substrateSurface = soil.GetComponent<MeshFilter>();
    }

    public bool ContainsSoilPoint(Vector3 worldPoint)
    {
        ResolveSurface();
        if (substrateSurface == null || substrateSurface.sharedMesh == null) return false;
        if (Vector3.Dot(substrateSurface.transform.up, Vector3.up) < 0.5f) return false;
        Bounds bounds = substrateSurface.sharedMesh.bounds;
        Vector3 point = substrateSurface.transform.InverseTransformPoint(worldPoint);
        // Ignore the planting trigger outside the visible soil footprint.
        return point.x >= bounds.min.x && point.x <= bounds.max.x &&
               point.z >= bounds.min.z && point.z <= bounds.max.z;
    }

    public bool CanReceiveWaterAt(Vector3 worldPoint)
    {
        if (!ContainsSoilPoint(worldPoint)) return false;
        Vector3 surfaceCentre = substrateSurface.transform.TransformPoint(substrateSurface.sharedMesh.bounds.center);
        float height = Vector3.Dot(worldPoint - surfaceCentre, substrateSurface.transform.up);
        // The scene's planting trigger sits just above the soil; pot sides/bottom do not count.
        return height >= -0.015f && height <= 0.08f;
    }

    public void SetSoilWaterMillilitres(float millilitres)
    {
        GardenSoil soil = GetComponentInChildren<GardenSoil>(true);
        if (soil != null) soil.SetReceivedMillilitres(millilitres);
    }

    public void SetSoilWaterFeedback(float millilitres, bool waterlogged)
    {
        GardenSoil soil = GetComponentInChildren<GardenSoil>(true);
        if (soil != null) soil.SetWaterFeedback(millilitres, waterlogged);
    }

    public bool ContainsReleasePoint(Vector3 point)
    {
        Vector3 delta = point - PlantingPoint;
        Vector3 up = transform.up;
        float vertical = Vector3.Dot(delta, up);
        Vector3 horizontal = delta - up * vertical;
        return horizontal.magnitude <= plantingRadius &&
               vertical >= -plantingHeight && vertical <= plantingHeight;
    }

    private void OnValidate()
    {
        plantingRadius = Mathf.Max(0.01f, plantingRadius);
        plantingHeight = Mathf.Max(0.01f, plantingHeight);
    }
}
