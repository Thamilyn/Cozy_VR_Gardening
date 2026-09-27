using UnityEngine;

/// <summary>A scene pot with one planting position. The point follows a movable pot.</summary>
[DisallowMultipleComponent]
public sealed class GardenPot : MonoBehaviour
{
    [SerializeField] private string potId;
    [SerializeField] private Vector3 localPlantingPoint;
    [SerializeField, Min(0.01f)] private float plantingRadius = 0.16f;
    [SerializeField, Min(0.01f)] private float plantingHeight = 0.25f;

    public string PotId => potId;
    public Vector3 PlantingPoint => transform.TransformPoint(localPlantingPoint);
    public Vector3 LocalPlantingPoint => localPlantingPoint;

    public void SetSoilWaterMillilitres(float millilitres)
    {
        GardenSoil soil = GetComponentInChildren<GardenSoil>(true);
        if (soil != null) soil.SetReceivedMillilitres(millilitres);
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
