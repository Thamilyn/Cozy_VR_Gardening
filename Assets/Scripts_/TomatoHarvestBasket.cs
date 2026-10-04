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

    public void Collect(TomatoFruit fruit, SeedItem source)
    {
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
