using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One manual, global clock with session-only planting and watering history.</summary>
[DisallowMultipleComponent]
public sealed class CalendarSystem : MonoBehaviour
{
    [Serializable]
    public sealed class Milestone
    {
        [Min(0)] public int day;
        public string displayName;
        public Sprite icon;
        public Color color = Color.white;
    }

    [Serializable]
    public sealed class JournalEntry
    {
        public int day;
        public string action;
        public string seedId;
        public string crop;
        public string potId;
        public float waterMillilitres;
    }

    [Serializable]
    public sealed class PlantSave
    {
        public string seedId;
        public string crop;
        public string potId;
        public float waterMillilitres;
        public float plantedAtGameDay;
        public float elapsedGameDays;
        public string stage;
    }

    [SerializeField] private Milestone[] milestones =
    {
        new Milestone { day = 0, displayName = "Seed", color = new Color(0.75f, 0.55f, 0.35f) },
        new Milestone { day = 7, displayName = "Sprout / germination", color = new Color(0.55f, 0.75f, 0.38f) },
        new Milestone { day = 45, displayName = "Young plant and growth", color = new Color(0.32f, 0.7f, 0.36f) },
        new Milestone { day = 60, displayName = "Flowering", color = new Color(0.95f, 0.8f, 0.38f) },
        new Milestone { day = 80, displayName = "Green tomatoes", color = new Color(0.42f, 0.72f, 0.3f) },
        new Milestone { day = 110, displayName = "Ripe tomatoes and harvest", color = new Color(0.9f, 0.34f, 0.25f) }
    };
    // Runtime state is intentionally not serialized into the scene or a JSON file.
    private float elapsedGameDays;
    private readonly List<JournalEntry> journal = new();
    private readonly List<PlantSave> plants = new();

    public event Action Changed;
    public float ElapsedGameDays => elapsedGameDays;
    public int CurrentDay => Mathf.FloorToInt(elapsedGameDays);
    public IReadOnlyList<Milestone> Milestones => milestones;
    public int CurrentPhaseIndex => PhaseIndexAt(elapsedGameDays);
    public bool IsCycleComplete => milestones != null && milestones.Length > 0 && CurrentDay >= milestones[milestones.Length - 1].day;
    public IReadOnlyList<JournalEntry> Journal => journal;
    public IReadOnlyList<PlantSave> Plants => plants;

    public int PhaseIndexAt(float days)
    {
        if (milestones == null || milestones.Length == 0) return 0;
        int index = 0;
        for (int i = 1; i < milestones.Length; i++)
            if (days >= milestones[i].day) index = i;
        return index;
    }

    /// <summary>Called by the VR button. Repeated presses at the last milestone do nothing.</summary>
    public void AdvancePhase()
    {
        if (milestones == null) return;
        foreach (Milestone milestone in milestones)
        {
            if (milestone.day <= elapsedGameDays) continue;
            elapsedGameDays = milestone.day;
            Changed?.Invoke();
            return;
        }
    }

    public PlantSave FindPlant(string seedId) => plants.Find(p => p.seedId == seedId);

    public void RecordPlanting(PlantSave plant)
    {
        if (plant == null || FindPlant(plant.seedId) != null) return;
        plants.Add(plant);
        journal.Add(new JournalEntry
        {
            day = CurrentDay, action = "Planted", seedId = plant.seedId,
            crop = plant.crop, potId = plant.potId
        });
        Changed?.Invoke();
    }

    public void RecordWater(PlantSave plant, float millilitres)
    {
        if (plant == null || millilitres <= 0f) return;
        plant.waterMillilitres += millilitres;
        JournalEntry entry = journal.FindLast(e =>
            e.day == CurrentDay && e.action == "Watered" && e.seedId == plant.seedId);
        if (entry == null)
        {
            entry = new JournalEntry
            {
                day = CurrentDay, action = "Watered", seedId = plant.seedId,
                crop = plant.crop, potId = plant.potId
            };
            journal.Add(entry);
        }
        entry.waterMillilitres += millilitres;
        Changed?.Invoke();
    }

    private void OnValidate()
    {
        if (milestones == null || milestones.Length == 0) return;
        milestones[0].day = 0;
        for (int i = 1; i < milestones.Length; i++)
            milestones[i].day = Mathf.Max(milestones[i - 1].day + 1, milestones[i].day);
    }
}
