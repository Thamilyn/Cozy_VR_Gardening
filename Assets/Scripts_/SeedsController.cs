using System;
using System.Collections.Generic;
using UnityEngine;

public enum SeedCrop { Tomato, Radish, Lettuce }
// Keep the original numeric values so existing scene and JSON data remain readable.
public enum GrowthStage { Seed = 0, Sprout = 1, Mature = 2, YoungPlant = 3, Flowering = 4, GreenFruit = 5 }

/// <summary>Owns planted seed references, crop requirements and stage transitions.</summary>
[DisallowMultipleComponent]
public sealed class SeedsController : MonoBehaviour
{
    [Serializable]
    public sealed class CropRequirements
    {
        public SeedCrop crop;
        [Min(0f)] public float daysToSprout = 1f;
        [Min(0f)] public float daysToMature = 3f;
        [Tooltip("Only used for radish and lettuce. Tomato growth follows its age, regardless of water.")]
        [Min(0f)] public float millilitresToSprout = 50f;
        [Min(0f)] public float millilitresToMature = 200f;
    }

    [Serializable]
    public sealed class PlantedSeed
    {
        public SeedItem seed;
        public GardenPot pot;
        public SeedCrop crop;
        public float elapsedGameDays;
        public GrowthStage stage;
        public float waterMillilitres;
    }

    [SerializeField] private CalendarSystem calendar;
    [SerializeField] private GardenPot[] pots;
    [SerializeField] private SeedItem[] seedPrefabs;
    [SerializeField] private CropRequirements[] requirements =
    {
        new CropRequirements { crop = SeedCrop.Radish, daysToSprout = 1f, daysToMature = 3f, millilitresToSprout = 40f, millilitresToMature = 180f },
        new CropRequirements { crop = SeedCrop.Lettuce, daysToSprout = 1f, daysToMature = 3f, millilitresToSprout = 50f, millilitresToMature = 220f }
    };
    private readonly List<PlantedSeed> plantedSeeds = new();

    public IReadOnlyList<PlantedSeed> PlantedSeeds => plantedSeeds;
    public CalendarSystem Calendar => calendar;

    private void Start()
    {
        if (calendar == null) calendar = FindFirstObjectByType<CalendarSystem>();
        if (calendar == null) { Debug.LogError("SeedsController needs CalendarSystem.", this); return; }
        if (pots == null || pots.Length == 0) pots = FindObjectsByType<GardenPot>(FindObjectsSortMode.None);
        calendar.Changed += RefreshPlants;

        foreach (GardenPot pot in pots)
        {
            if (pot == null) continue;
            pot.SetSoilWaterMillilitres(0f);
            PlantWaterReceiver receiver = pot.GetComponent<PlantWaterReceiver>();
            if (receiver == null) receiver = pot.gameObject.AddComponent<PlantWaterReceiver>();
            GardenPot target = pot;
            receiver.WaterReceived += litres => ReceiveWater(target, litres);
        }

        foreach (CalendarSystem.PlantSave saved in calendar.Plants)
        {
            GardenPot pot = Array.Find(pots, p => p != null && p.PotId == saved.potId);
            if (pot == null || FindByPot(pot) != null) continue;
            if (!Enum.TryParse(saved.crop, out SeedCrop crop)) continue;
            SeedItem seed = FindSceneSeed(saved.seedId);
            if (seed == null)
            {
                SeedItem prefab = Array.Find(seedPrefabs, p => p != null && p.Crop == crop);
                if (prefab == null) continue;
                seed = Instantiate(prefab, pot.PlantingPoint, Quaternion.identity);
                seed.AssignId(saved.seedId);
            }
            Attach(seed, pot, saved, crop);
        }
        RefreshPlants();
    }

    private void OnDestroy()
    {
        if (calendar != null) calendar.Changed -= RefreshPlants;
    }

    private void RefreshPlants()
    {
        if (calendar == null) return;
        foreach (PlantedSeed plant in plantedSeeds)
        {
            if (plant.seed == null || plant.pot == null) continue;
            CalendarSystem.PlantSave saved = calendar.FindPlant(plant.seed.SeedId);
            if (saved == null) continue;
            float days = Mathf.Max(0f, calendar.ElapsedGameDays - saved.plantedAtGameDay);
            GrowthStage stage = CalculateStage(plant.crop, days, saved.waterMillilitres);
            plant.elapsedGameDays = days;
            plant.waterMillilitres = saved.waterMillilitres;
            saved.elapsedGameDays = days;
            if (plant.stage == stage) continue;
            plant.stage = stage;
            plant.seed.ShowStage(stage);
            saved.stage = stage.ToString();
        }
    }

    public bool TryPlant(SeedItem seed)
    {
        if (seed == null || seed.IsPlanted || calendar == null) return false;
        if (calendar.FindPlant(seed.SeedId) != null) return false;
        foreach (GardenPot pot in pots)
        {
            if (pot == null || !pot.ContainsReleasePoint(seed.transform.position) || FindByPot(pot) != null) continue;
            CalendarSystem.PlantSave saved = new()
            {
                seedId = seed.SeedId, crop = seed.Crop.ToString(), potId = pot.PotId,
                plantedAtGameDay = calendar.ElapsedGameDays, stage = GrowthStage.Seed.ToString()
            };
            calendar.RecordPlanting(saved);
            Attach(seed, pot, saved, seed.Crop);
            return true;
        }
        return false;
    }

    public void ReceiveWater(GardenPot pot, float amountLitres)
    {
        if (pot == null || amountLitres <= 0f || calendar == null) return;
        PlantedSeed plant = FindByPot(pot);
        if (plant == null || plant.seed == null) return;
        CalendarSystem.PlantSave saved = calendar.FindPlant(plant.seed.SeedId);
        if (saved == null) return;
        calendar.RecordWater(saved, amountLitres * 1000f);
        pot.SetSoilWaterMillilitres(saved.waterMillilitres);
        RefreshPlants();
    }

    private void Attach(SeedItem seed, GardenPot pot, CalendarSystem.PlantSave saved, SeedCrop crop)
    {
        seed.PlantIn(pot);
        PlantedSeed plant = new()
        {
            seed = seed, pot = pot, crop = crop, waterMillilitres = saved.waterMillilitres,
            elapsedGameDays = Mathf.Max(0f, calendar.ElapsedGameDays - saved.plantedAtGameDay)
        };
        plant.stage = CalculateStage(crop, plant.elapsedGameDays, plant.waterMillilitres);
        saved.elapsedGameDays = plant.elapsedGameDays;
        seed.ShowStage(plant.stage);
        plantedSeeds.Add(plant);
        pot.SetSoilWaterMillilitres(saved.waterMillilitres);
        if (saved.stage != plant.stage.ToString())
            saved.stage = plant.stage.ToString();
    }

    private GrowthStage CalculateStage(SeedCrop crop, float days, float millilitres)
    {
        if (crop == SeedCrop.Tomato)
        {
            int phase = calendar.PhaseIndexAt(days);
            return phase switch
            {
                0 => GrowthStage.Seed,
                1 => GrowthStage.Sprout,
                2 => GrowthStage.YoungPlant,
                3 => GrowthStage.Flowering,
                4 => GrowthStage.GreenFruit,
                _ => GrowthStage.Mature
            };
        }
        // Radish and lettuce deliberately retain their age AND water gates.
        CropRequirements rule = Array.Find(requirements, r => r != null && r.crop == crop);
        if (rule == null) return GrowthStage.Seed;
        if (days >= rule.daysToMature && millilitres >= rule.millilitresToMature) return GrowthStage.Mature;
        if (days >= rule.daysToSprout && millilitres >= rule.millilitresToSprout) return GrowthStage.Sprout;
        return GrowthStage.Seed;
    }

    private PlantedSeed FindByPot(GardenPot pot) => plantedSeeds.Find(p => p.pot == pot);

    private static SeedItem FindSceneSeed(string id)
    {
        foreach (SeedItem seed in FindObjectsByType<SeedItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (seed.SeedId == id) return seed;
        return null;
    }
}
