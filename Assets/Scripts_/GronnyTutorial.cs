using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Locomotion;
using UnityEngine;

/// <summary>Observes the existing tomato activity; never changes cultivation or player input.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class GronnyTutorial : MonoBehaviour
{
    [SerializeField] private GronnyAudioCatalogue catalogue;
    private SeedsController seeds;
    private CalendarSystem calendar;
    private CalendarVisibilityController visibility;
    private CalendarPanel panel;
    private FirstPersonLocomotor[] locomotors;
    private WateringCan[] cans;
    private SeedItem[] seedItems;
    private GronnyVoicePlayer voice;
    private GardenControlDemonstration demonstration;
    private readonly HashSet<string> learned = new();
    private readonly HashSet<string> heldObjects = new();
    private GardenInputMode mode;
    private GardenInputMode candidateMode;
    private float modeChangedAt;
    private float lastActionAt;
    private float lastWater;
    private float nextRefresh;
    private int phase = -2;
    private bool wasExcess;
    private bool completed;
    private bool initialized;
    private bool subscribed;

    private SeedsController.PlantedSeed Plant => seeds != null ? seeds.PrototypeTomato : null;
    private CalendarSystem.PlantSave State => seeds != null ? seeds.GetState(Plant) : null;
    private bool Running => initialized && isActiveAndEnabled && !completed &&
        seeds != null && seeds.isActiveAndEnabled && calendar != null;
    private bool Caring => Running && State != null && State.tomatoPhase < 5 && !calendar.IsAdvancing;
    private bool Excess => Caring && State.retainedWaterMillilitres > seeds.ExcessLimit(Plant);
    private bool NeedsWater => Caring && seeds.TargetMillilitres(Plant) > 0f &&
        State.retainedWaterMillilitres < seeds.TargetMillilitres(Plant);
    private bool Ready => Caring && !NeedsWater && !Excess &&
        seeds.TargetMillilitres(Plant) > 0f && Time.unscaledTime - State.lastWaterAt >= 0.5f;
    private bool CanEmpty => cans != null && System.Array.Exists(cans,
        can => can != null && can.isActiveAndEnabled && can.IsHeld && can.CurrentWaterLitres <= 0f);

    private void Start()
    {
        if (catalogue == null) catalogue = Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial");
        seeds = FindFirstObjectByType<SeedsController>();
        calendar = seeds != null ? seeds.Calendar : null;
        if (catalogue == null || calendar == null)
        {
            Debug.LogError("Gronny tutorial needs its audio catalogue and the existing tomato calendar.", this);
            enabled = false;
            return;
        }
        visibility = FindFirstObjectByType<CalendarVisibilityController>();
        panel = FindFirstObjectByType<CalendarPanel>(FindObjectsInactive.Include);
        cans = FindObjectsByType<WateringCan>(FindObjectsSortMode.None);
        seedItems = FindObjectsByType<SeedItem>(FindObjectsSortMode.None);
        locomotors = FindObjectsByType<FirstPersonLocomotor>(FindObjectsSortMode.None);
        demonstration = gameObject.AddComponent<GardenControlDemonstration>();
        demonstration.Configure(catalogue);
        voice = gameObject.AddComponent<GronnyVoicePlayer>();
        voice.Configure(catalogue, demonstration);
        initialized = true;
        lastActionAt = Time.unscaledTime;
        Subscribe();
        Say("01_welcome", "welcome", () => Running);
        Say("02_tomato_intro", "intro", () => Running && State == null);
        RefreshPhase();
    }

    private void OnEnable()
    {
        if (!initialized) return;
        voice.enabled = true;
        Subscribe();
        lastActionAt = Time.unscaledTime;
        QueueControls();
    }

    private void Subscribe()
    {
        if (subscribed || completed) return;
        seeds.PlantingRejected += OnPlantingRejected;
        calendar.AdvanceBlocked += OnAdvanceBlocked;
        if (visibility != null)
        {
            visibility.VisibilityChanged += OnCalendarVisible;
            visibility.CloseSelected += OnButtonSelected;
        }
        if (panel != null) panel.ButtonSelected += OnButtonSelected;
        foreach (FirstPersonLocomotor locomotor in locomotors)
            if (locomotor != null) locomotor.WhenLocomotionEventHandled += OnMoved;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (seeds != null) seeds.PlantingRejected -= OnPlantingRejected;
        if (calendar != null) calendar.AdvanceBlocked -= OnAdvanceBlocked;
        if (visibility != null)
        {
            visibility.VisibilityChanged -= OnCalendarVisible;
            visibility.CloseSelected -= OnButtonSelected;
        }
        if (panel != null) panel.ButtonSelected -= OnButtonSelected;
        foreach (FirstPersonLocomotor locomotor in locomotors)
            if (locomotor != null) locomotor.WhenLocomotionEventHandled -= OnMoved;
        subscribed = false;
    }

    private void Update()
    {
        if (!Running)
        {
            if (initialized && !completed) voice.Clear();
            return;
        }
        UpdateMode();
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.15f;
        ObserveGrabs();
        RefreshPhase();
        if (State == null) { IdleHelp(); return; }
        if (State.harvested)
        {
            voice.Clear();
            completed = true;
            Unsubscribe();
            // The closing line is the only request allowed after tutorial completion.
            voice.Enqueue("13_complete", "complete", () => isActiveAndEnabled && State?.harvested == true);
            return;
        }
        if (calendar.IsAdvancing) { voice.Clear(); return; }
        if (State.waterMillilitres != lastWater)
        {
            lastWater = State.waterMillilitres;
            lastActionAt = Time.unscaledTime;
        }
        bool excess = Excess;
        if (excess && !wasExcess) Help("help_excess_water", () => Excess);
        if (!excess && wasExcess && State.phaseWaterMillilitres > seeds.ExcessLimit(Plant))
            Help("help_drained", () => Caring && !Excess &&
                State.phaseWaterMillilitres > seeds.ExcessLimit(Plant));
        wasExcess = excess;
        if (phase == 0 && Caring && !NeedsWater && !Excess)
            Say("06_enough_water", "enough", () => Caring && phase == 0 && !NeedsWater && !Excess);
        if (phase == 0 && Ready)
            Say("07_first_advance", "advance", () => Ready && phase == 0);
        if (CanEmpty && NeedsWater) Help("help_refill", () => CanEmpty && NeedsWater);
        QueueControls();
        IdleHelp();
    }

    private void RefreshPhase()
    {
        int currentPhase = State != null ? State.tomatoPhase : -1;
        if (phase == currentPhase) return;
        bool first = phase == -2;
        phase = currentPhase;
        wasExcess = false;
        lastWater = State != null ? State.waterMillilitres : 0f;
        lastActionAt = Time.unscaledTime;
        if (!first) voice.Clear();
        int captured = phase;
        if (phase < 0)
            Say("03_plant_seed", "plant", () => Running && State == null);
        else if (phase == 0)
        {
            Say("04_first_watering", "watering", () => Caring && phase == 0 && NeedsWater);
            Say("05_water_target", "target", () => Caring && phase == 0 && NeedsWater);
        }
        else
        {
            string id = phase switch
            {
                1 => "08_sprout", 2 => "09_young_plant", 3 => "10_flowering",
                4 => "11_green_fruit", _ => "12_harvest"
            };
            Say(id, "phase-" + phase, () => Running && State != null &&
                State.tomatoPhase == captured && !calendar.IsAdvancing);
        }
        QueueControls();
    }

    private void UpdateMode()
    {
        OVRInput.Controller active = OVRInput.GetActiveController();
        GardenInputMode detected = (active & OVRInput.Controller.Hands) != 0 ? GardenInputMode.Hands :
            (active & OVRInput.Controller.Touch) != 0 ? GardenInputMode.Controllers : GardenInputMode.Unknown;
        if (detected != candidateMode) { candidateMode = detected; modeChangedAt = Time.unscaledTime; }
        if (mode == detected || Time.unscaledTime - modeChangedAt < 0.35f) return;
        mode = detected;
        voice.CancelControls();
        demonstration.SetInputMode(mode);
        heldObjects.Clear();
        QueueControls();
    }

    private bool Learned(string control) => learned.Contains(mode + ":" + control);
    private void Learn(string control)
    {
        if (mode != GardenInputMode.Unknown) learned.Add(mode + ":" + control);
        lastActionAt = Time.unscaledTime;
    }

    private void QueueControls()
    {
        if (!Running || mode == GardenInputMode.Unknown || calendar.IsAdvancing) return;
        // Full control explanations are limited to the first care phase.
        if (phase < 0)
        {
            Control("move", () => Running && State == null);
            Control("grab", () => Running && State == null);
        }
        if (phase == 0 && (Ready || CanEmpty))
        {
            if (visibility != null && !visibility.IsVisible)
                Control("calendar", () => Running && phase == 0 && (Ready || CanEmpty) && !visibility.IsVisible);
            else if (visibility != null && visibility.IsVisible)
                Control("calendar_select", () => Running && phase == 0 && (Ready || CanEmpty) && visibility.IsVisible);
        }
    }

    private void Control(string name, System.Func<bool> relevant)
    {
        GardenInputMode requestedMode = mode;
        string id = "controls_" + name + (mode == GardenInputMode.Hands ? "_hands" : "_controllers");
        Say(id, id, () => Running && mode == requestedMode && !Learned(name) && relevant());
    }

    private void Say(string id, string key, System.Func<bool> relevant, bool once = true)
    {
        System.Func<bool> demoRelevant = catalogue.Find(id)?.demo == GardenControlDemo.Upright
            ? () => System.Array.Exists(cans, can => can != null && can.IsPouring) : null;
        voice.Enqueue(id, key, relevant, () =>
        {
            if (catalogue.Find(id).assisted) calendar.RecordTutorialHelp(id);
        }, once, demoRelevant);
    }

    private void Help(string id, System.Func<bool> relevant)
    {
        int capturedPhase = phase;
        Say(id, id + ":" + phase, () => Running && phase == capturedPhase && relevant(), false);
    }

    private void IdleHelp()
    {
        float delay = phase >= 2 ? catalogue.independentHelpSeconds : catalogue.idleHelpSeconds;
        if (Time.unscaledTime - lastActionAt < delay) return;
        lastActionAt = Time.unscaledTime;
        if (State == null) Help("help_planting", () => State == null);
        else if (NeedsWater) Help("help_low_water", () => NeedsWater);
        else if (Ready) Help("help_calendar", () => Ready);
    }

    private void OnPlantingRejected(SeedItem seed)
    {
        if (seed != null && seed.Crop == SeedCrop.Tomato)
            Help("help_planting", () => Running && State == null);
    }

    private void OnAdvanceBlocked(string reason)
    {
        if (!Running) return;
        if (State == null) Help("help_planting", () => State == null);
        else if (Excess) Help("help_excess_water", () => Excess);
        else if (NeedsWater) Help("help_low_water", () => NeedsWater);
    }

    private void OnCalendarVisible(bool visible)
    {
        if (!Running || !visible) return;
        Learn("calendar");
        QueueControls();
    }

    private void OnButtonSelected()
    {
        if (Running) Learn("calendar_select");
    }

    private void OnMoved(LocomotionEvent evt, Pose delta)
    {
        if (Running && evt.Translation != LocomotionEvent.TranslationType.None &&
            delta.position.sqrMagnitude > 0.0001f) Learn("move");
    }

    private void ObserveGrabs()
    {
        foreach (SeedItem seed in seedItems)
        {
            if (seed == null || seed.Crop != SeedCrop.Tomato) continue;
            Grabbable grabbable = seed.GetComponent<Grabbable>();
            ObserveObject(seed.GetInstanceID().ToString(), grabbable != null && grabbable.SelectingPointsCount > 0);
        }
        foreach (WateringCan can in cans)
            if (can != null) ObserveObject(can.GetInstanceID().ToString(), can.IsHeld);
    }

    private void ObserveObject(string id, bool held)
    {
        if (held) heldObjects.Add(id);
        else if (heldObjects.Remove(id)) Learn("grab");
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (voice != null) { voice.Clear(); voice.enabled = false; }
        if (demonstration != null) demonstration.Hide();
    }
}
