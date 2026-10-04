using System;
using System.Collections.Generic;
using UnityEngine;

public enum SeedCrop { Tomato, Radish, Lettuce }
// Preserve numeric values used by the existing scene assets.
public enum GrowthStage { Seed = 0, Sprout = 1, Mature = 2, YoungPlant = 3, Flowering = 4, GreenFruit = 5 }

/// <summary>Owns planting, phase water requirements and crop stage transitions.</summary>
[DisallowMultipleComponent]
public sealed class SeedsController : MonoBehaviour
{
    [Serializable]
    public sealed class CropRequirements
    {
        public SeedCrop crop;
        [Min(0f)] public float daysToSprout = 1f;
        [Min(0f)] public float daysToMature = 3f;
        [Tooltip("Legacy cumulative requirements for radish and lettuce.")]
        [Min(0f)] public float millilitresToSprout = 50f;
        [Min(0f)] public float millilitresToMature = 200f;
    }

    [Serializable]
    public sealed class TomatoWaterPhase
    {
        public string name;
        [Min(0.01f)] public float demandMultiplier = 1f;
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
    [Header("Tomato: educational prototype water model")]
    [SerializeField] private bool singleTomatoPrototype;
    [SerializeField, Min(0.01f)] private float referenceMillimetresPerWeek = 25f;
    [Tooltip("Each calendar jump represents one care day, not all the skipped biological days.")]
    [SerializeField, Min(0.01f)] private float representativeCareDays = 1f;
    [SerializeField, Min(1f)] private float excessMultiplier = 1.5f;
    [Tooltip("Accelerated drainage after pouring stops. Applied water remains in the journal.")]
    [SerializeField, Min(1f)] private float drainageWaitSeconds = 5f;
    [SerializeField] private TomatoWaterPhase[] tomatoWaterPhases =
    {
        new TomatoWaterPhase { name = "Seed", demandMultiplier = 1f },
        new TomatoWaterPhase { name = "Sprout", demandMultiplier = 1.2f },
        new TomatoWaterPhase { name = "Growth", demandMultiplier = 1.5f },
        new TomatoWaterPhase { name = "Flowering", demandMultiplier = 1.7f },
        new TomatoWaterPhase { name = "Green fruit", demandMultiplier = 1.5f }
    };
    private readonly List<PlantedSeed> plantedSeeds = new();
    private readonly Dictionary<PlantWaterReceiver, Action<float>> receivers = new();

    public event Action<string> Guidance;
    public event Action<SeedItem> PlantingRejected;
    public IReadOnlyList<PlantedSeed> PlantedSeeds => plantedSeeds;
    public CalendarSystem Calendar => calendar;
    public PlantedSeed PrototypeTomato => plantedSeeds.Find(p => p.crop == SeedCrop.Tomato && p.seed != null);
    public bool HarvestComplete => PrototypeTomato != null && GetState(PrototypeTomato)?.harvested == true;

    private void Start()
    {
        if (calendar == null) calendar = FindFirstObjectByType<CalendarSystem>();
        if (calendar == null) { Debug.LogError("SeedsController needs CalendarSystem.", this); return; }
        if (pots == null || pots.Length == 0) pots = FindObjectsByType<GardenPot>(FindObjectsSortMode.None);
        calendar.Changed += RefreshPlants;
        calendar.PhaseAdvanced += AdvancePlants;
        calendar.AdvanceBlockReason = GetAdvanceBlockReason;
        foreach (GardenPot pot in pots)
        {
            if (pot == null) continue;
            pot.SetSoilWaterMillilitres(0f);
            PlantWaterReceiver receiver = pot.GetComponent<PlantWaterReceiver>();
            if (receiver == null) receiver = pot.gameObject.AddComponent<PlantWaterReceiver>();
            GardenPot target = pot;
            Action<float> handler = litres => ReceiveWater(target, litres);
            receiver.WaterReceived += handler;
            receivers.Add(receiver, handler);
        }
        RefreshPlants();
    }

    private void Update()
    {
        if (calendar == null) return;
        foreach (PlantedSeed plant in plantedSeeds)
        {
            if (plant.crop != SeedCrop.Tomato || plant.seed == null) continue;
            CalendarSystem.PlantSave saved = GetState(plant);
            if (saved == null || saved.tomatoPhase >= 5 || saved.retainedWaterMillilitres <= ExcessLimit(plant) ||
                Time.unscaledTime - saved.lastWaterAt < drainageWaitSeconds) continue;
            // Drain only surplus in this teaching model; never erase the water actually applied.
            saved.retainedWaterMillilitres = TargetMillilitres(plant);
            plant.pot.SetSoilWaterMillilitres(100f);
            calendar.NotifyChanged();
            Guidance?.Invoke("The extra water has drained away.\nNo more water is needed.");
        }
    }

    private void OnDestroy()
    {
        if (calendar != null)
        {
            calendar.Changed -= RefreshPlants;
            calendar.PhaseAdvanced -= AdvancePlants;
            calendar.AdvanceBlockReason = null;
        }
        foreach (var entry in receivers)
            if (entry.Key != null) entry.Key.WaterReceived -= entry.Value;
    }

    public CalendarSystem.PlantSave GetState(PlantedSeed plant) =>
        plant != null && plant.seed != null && calendar != null ? calendar.FindPlant(plant.seed.SeedId) : null;

    public float TargetMillilitres(PlantedSeed plant)
    {
        CalendarSystem.PlantSave saved = GetState(plant);
        if (saved == null || plant.pot == null || saved.tomatoPhase >= 5 ||
            tomatoWaterPhases == null || saved.tomatoPhase >= tomatoWaterPhases.Length) return 0f;
        TomatoWaterPhase phase = tomatoWaterPhases[saved.tomatoPhase];
        if (phase == null) return 0f;
        return referenceMillimetresPerWeek / 7f * representativeCareDays *
            phase.demandMultiplier * plant.pot.SubstrateAreaSquareMetres * 1000f;
    }

    public float ExcessLimit(PlantedSeed plant) => TargetMillilitres(plant) * Mathf.Max(1f, excessMultiplier);

    public string WaterStatus(PlantedSeed plant)
    {
        CalendarSystem.PlantSave saved = GetState(plant);
        if (saved == null) return "Not planted";
        if (saved.tomatoPhase >= 5) return "Ripe: harvest now; no more watering is needed in this prototype";
        float target = TargetMillilitres(plant);
        if (target <= 0f) return "Check the soil area and water targets in the Inspector";
        if (saved.retainedWaterMillilitres > ExcessLimit(plant)) return "Too much water: stop and wait for drainage";
        if (saved.retainedWaterMillilitres < target) return "Not enough water: water the soil a little more";
        return saved.phaseWaterMillilitres > ExcessLimit(plant)
            ? "Excess water has drained: stop watering; you can advance"
            : "Within target: stop watering; you can advance";
    }

    public string GetAdvanceBlockReason()
    {
        if (calendar == null) return "Calendar unavailable.";
        bool hasTomato = false;
        foreach (PlantedSeed plant in plantedSeeds)
        {
            if (plant.crop != SeedCrop.Tomato || plant.seed == null) continue;
            hasTomato = true;
            CalendarSystem.PlantSave saved = GetState(plant);
            if (saved == null || saved.tomatoPhase >= 5) continue;
            float target = TargetMillilitres(plant);
            if (target <= 0f) return "Check the soil area and the five water targets.";
            if (saved.retainedWaterMillilitres > ExcessLimit(plant))
                return "Too much water in " + plant.pot.PotId + ". Stop and wait for drainage.";
            if (saved.retainedWaterMillilitres < target)
                return "Still needs " + (target - saved.retainedWaterMillilitres).ToString("0.0") +
                    " ml in " + plant.pot.PotId + ". Water the soil before advancing.";
            if (Time.unscaledTime - saved.lastWaterAt < 0.5f)
                return "Hold the can upright.\nWait until pouring stops.";
        }
        if (!hasTomato) return "Drop the tomato seed onto the soil in an empty small pot.";
        if (calendar.IsCycleComplete) return "Pick the red tomato and place it in the tray.";
        return null;
    }

    private void AdvancePlants()
    {
        foreach (PlantedSeed plant in plantedSeeds)
        {
            if (plant.crop != SeedCrop.Tomato || plant.seed == null) continue;
            CalendarSystem.PlantSave saved = GetState(plant);
            if (saved == null || saved.tomatoPhase >= 5) continue;
            saved.tomatoPhase++;
            saved.phaseWaterMillilitres = 0f;
            saved.retainedWaterMillilitres = 0f;
            saved.currentPourMillilitres = 0f;
            saved.lastWaterAt = float.NegativeInfinity;
            plant.pot.SetSoilWaterMillilitres(saved.tomatoPhase >= 5 ? 100f : 0f);
        }
        RefreshPlants();
        Guidance?.Invoke(PrototypeTomato?.stage == GrowthStage.Mature
            ? "Your tomato is ripe!\nPlace the largest red tomato in the harvest tray."
            : "Your tomato has grown!\nCheck its new water target.");
    }

    private void RefreshPlants()
    {
        if (calendar == null) return;
        foreach (PlantedSeed plant in plantedSeeds)
        {
            if (plant.seed == null || plant.pot == null) continue;
            CalendarSystem.PlantSave saved = GetState(plant);
            if (saved == null) continue;
            plant.elapsedGameDays = Mathf.Max(0f, calendar.ElapsedGameDays - saved.plantedAtGameDay);
            plant.waterMillilitres = saved.waterMillilitres;
            saved.elapsedGameDays = plant.elapsedGameDays;
            GrowthStage stage = plant.crop == SeedCrop.Tomato ? TomatoStage(saved.tomatoPhase) :
                CalculateLegacyStage(plant.crop, plant.elapsedGameDays, plant.waterMillilitres);
            saved.stage = stage.ToString();
            if (plant.stage == stage) continue;
            plant.stage = stage;
            plant.seed.ShowStage(stage);
        }
    }

    public bool TryPlant(SeedItem seed)
    {
        if (seed == null || seed.IsPlanted || calendar == null) return false;
        if (singleTomatoPrototype && (seed.Crop != SeedCrop.Tomato || PrototypeTomato != null))
        {
            Guidance?.Invoke("This activity uses one tomato seed.");
            return false;
        }
        if (calendar.IsCycleComplete)
        {
            Guidance?.Invoke("Press Restart on the calendar to start a new crop.");
            return false;
        }
        if (calendar.FindPlant(seed.SeedId) != null) return false;
        foreach (GardenPot pot in pots)
        {
            if (pot == null || !pot.ContainsReleasePoint(seed.transform.position) || FindByPot(pot) != null ||
                (seed.Crop == SeedCrop.Tomato && (!pot.AcceptsTomato || !pot.ContainsSoilPoint(seed.transform.position)))) continue;
            CalendarSystem.PlantSave saved = new()
            {
                seedId = seed.SeedId, crop = seed.Crop.ToString(), potId = pot.PotId,
                plantedAtGameDay = calendar.ElapsedGameDays, stage = GrowthStage.Seed.ToString()
            };
            seed.PlantIn(pot);
            plantedSeeds.Add(new PlantedSeed { seed = seed, pot = pot, crop = seed.Crop, stage = GrowthStage.Seed });
            calendar.RecordPlanting(saved);
            seed.ShowStage(GrowthStage.Seed);
            Guidance?.Invoke("Seed planted!\nTilt the watering can over the soil.");
            return true;
        }
        Guidance?.Invoke("Drop the seed onto the center of an empty small pot.");
        PlantingRejected?.Invoke(seed);
        return false;
    }

    public void ReceiveWater(GardenPot pot, float amountLitres)
    {
        if (pot == null || amountLitres <= 0f || float.IsNaN(amountLitres) || float.IsInfinity(amountLitres) || calendar == null) return;
        if (calendar.IsAdvancing) return;
        PlantedSeed plant = FindByPot(pot);
        CalendarSystem.PlantSave saved = GetState(plant);
        if (saved == null) return;
        float millilitres = amountLitres * 1000f;
        if (Time.unscaledTime - saved.lastWaterAt > 0.5f) saved.currentPourMillilitres = 0f;
        saved.currentPourMillilitres += millilitres;
        saved.phaseWaterMillilitres += millilitres;
        saved.retainedWaterMillilitres += millilitres;
        saved.lastWaterAt = Time.unscaledTime;
        if (plant.crop == SeedCrop.Tomato)
        {
            float target = TargetMillilitres(plant);
            // Soil wetness uses 100 as the normalized target; excess uses the actual phase limit.
            bool waterlogged = target > 0f && saved.retainedWaterMillilitres > ExcessLimit(plant);
            pot.SetSoilWaterFeedback(target > 0f ? saved.retainedWaterMillilitres / target * 100f : 100f,
                waterlogged);
        }
        calendar.RecordWater(saved, millilitres);
        if (plant.crop != SeedCrop.Tomato) pot.SetSoilWaterMillilitres(saved.waterMillilitres);
    }

    private GrowthStage CalculateLegacyStage(SeedCrop crop, float days, float millilitres)
    {
        CropRequirements rule = Array.Find(requirements, r => r != null && r.crop == crop);
        if (rule == null) return GrowthStage.Seed;
        if (days >= rule.daysToMature && millilitres >= rule.millilitresToMature) return GrowthStage.Mature;
        if (days >= rule.daysToSprout && millilitres >= rule.millilitresToSprout) return GrowthStage.Sprout;
        return GrowthStage.Seed;
    }

    private static GrowthStage TomatoStage(int phase) => phase switch
    {
        1 => GrowthStage.Sprout, 2 => GrowthStage.YoungPlant, 3 => GrowthStage.Flowering,
        4 => GrowthStage.GreenFruit, 5 => GrowthStage.Mature, _ => GrowthStage.Seed
    };

    private PlantedSeed FindByPot(GardenPot pot) => plantedSeeds.Find(p => p.pot == pot);
}
