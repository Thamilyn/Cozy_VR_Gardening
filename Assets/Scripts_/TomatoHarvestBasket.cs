using UnityEngine;

/// <summary>Collects ripe tomatoes inside the tray's editable scene volume.</summary>
[DisallowMultipleComponent]
public sealed class TomatoHarvestBasket : MonoBehaviour
{
    [SerializeField] private BoxCollider collectionVolume;
    [SerializeField] private GardenerMixamoGreeting gardener;

    public bool Contains(Vector3 worldPoint)
    {
        if (collectionVolume == null || !collectionVolume.enabled || !collectionVolume.gameObject.activeInHierarchy)
            return false;
        Vector3 point = collectionVolume.transform.InverseTransformPoint(worldPoint) - collectionVolume.center;
        Vector3 halfSize = collectionVolume.size * 0.5f;
        return Mathf.Abs(point.x) < halfSize.x && Mathf.Abs(point.z) < halfSize.z &&
               Mathf.Abs(point.y) <= halfSize.y;
    }

    public bool TryGetRestingPoint(out Vector3 worldPoint)
    {
        worldPoint = default;
        if (collectionVolume == null) return false;

        // The root can be offset from the box. Cast through the actual interior,
        // using only the box's solid colliders so tables and triggers cannot win.
        Physics.SyncTransforms();
        Collider[] surfaces = GetComponentsInChildren<Collider>();
        Vector3 origin = collectionVolume.transform.TransformPoint(collectionVolume.center);
        float top = collectionVolume.bounds.max.y;
        foreach (Collider surface in surfaces)
            if (IsRestingSurface(surface)) top = Mathf.Max(top, surface.bounds.max.y);
        origin.y = top + TomatoFruit.DiameterMetres;

        Ray ray = new(origin, Vector3.down);
        float nearest = float.PositiveInfinity;
        foreach (Collider surface in surfaces)
        {
            if (!IsRestingSurface(surface) ||
                !surface.Raycast(ray, out RaycastHit hit, Mathf.Infinity) || hit.distance >= nearest) continue;
            nearest = hit.distance;
            worldPoint = hit.point;
        }
        return !float.IsPositiveInfinity(nearest);
    }

    private static bool IsRestingSurface(Collider surface) =>
        surface.enabled && !surface.isTrigger && surface.GetComponentInParent<TomatoFruit>() == null;

    public void Collect(TomatoFruit fruit, SeedItem source)
    {
        if (GardenMenuController.GardenSuspended) return;
        if (fruit == null || source == null || source.Crop != SeedCrop.Tomato || !Contains(fruit.transform.position)) return;
        SeedsController controller = FindFirstObjectByType<SeedsController>();
        if (controller == null || controller.Calendar == null) return;
        CalendarSystem.PlantSave plant = controller.Calendar.FindPlant(source.SeedId);
        if (plant == null || plant.harvested || plant.stage != GrowthStage.Mature.ToString()) return;
        fruit.StoreIn(transform);
        controller.Calendar.RecordHarvest(plant);
        if (gardener == null) gardener = FindFirstObjectByType<GardenerMixamoGreeting>();
        if (gardener != null) gardener.CelebrateHarvest();
    }
}
