using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Game-day clock and durable planting/watering journal for Garden_Moves.</summary>
[DisallowMultipleComponent]
public sealed class CalendarSystem : MonoBehaviour
{
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

    [Serializable]
    private sealed class SaveData
    {
        public float elapsedGameDays;
        public List<JournalEntry> journal = new();
        public List<PlantSave> plants = new();
    }

    [SerializeField, Min(1f)] private float secondsPerGameDay = 120f;
    [SerializeField] private float elapsedGameDays;
    [SerializeField] private List<JournalEntry> journal = new();
    [SerializeField] private List<PlantSave> plants = new();

    private float _saveTimer;
    private bool _dirty;
    private string SavePath => Path.Combine(Application.persistentDataPath, "Garden_Moves_calendar.json");

    public float ElapsedGameDays => elapsedGameDays;
    public int CurrentDay => Mathf.FloorToInt(elapsedGameDays) + 1;
    public IReadOnlyList<JournalEntry> Journal => journal;
    public IReadOnlyList<PlantSave> Plants => plants;

    private void Awake() => Load();

    private void Update()
    {
        elapsedGameDays += Time.deltaTime / secondsPerGameDay;
        _dirty = true;
        _saveTimer += Time.unscaledDeltaTime;
        if (_saveTimer >= 5f) Save();
    }

    /// <summary>Inspector button or UI hook for testing without waiting in real time.</summary>
    [ContextMenu("Advance one game day")]
    public void AdvanceOneDay() => AdvanceDays(1f);

    public void AdvanceDays(float days)
    {
        if (days <= 0f) return;
        elapsedGameDays += days;
        _dirty = true;
        Save();
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
        _dirty = true;
        Save();
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
        _dirty = true;
    }

    public void MarkChanged() => _dirty = true;

    public void Save()
    {
        if (!_dirty) return;
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(new SaveData
            {
                elapsedGameDays = elapsedGameDays, journal = journal, plants = plants
            }, true));
            _dirty = false;
            _saveTimer = 0f;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not save garden calendar: {exception.Message}", this);
        }
    }

    private void Load()
    {
        if (!File.Exists(SavePath)) return;
        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data == null) return;
            elapsedGameDays = Mathf.Max(0f, data.elapsedGameDays);
            journal = data.journal ?? new List<JournalEntry>();
            plants = data.plants ?? new List<PlantSave>();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not load garden calendar: {exception.Message}", this);
        }
    }

    private void OnApplicationPause(bool paused) { if (paused) Save(); }
    private void OnApplicationQuit() => Save();
    private void OnDisable() => Save();
}
