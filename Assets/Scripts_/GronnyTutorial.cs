using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Locomotion;
using UnityEngine;

// Keep the remaining action values stable after removing manual can recovery (6).
public enum GardenTutorialAction { Plant, Pour, OpenCalendar, Advance, Refill, RefillWithController, Restart = 7, CloseCalendar, FillSoil }

/// <summary>Unlocks the first tomato instructions after the player completes each action.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class GronnyTutorial : MonoBehaviour
{
    [SerializeField] private GronnyAudioCatalogue catalogue;
    private SeedsController seeds;
    private CalendarSystem calendar;
    private CalendarVisibilityController visibility;
    private CalendarPanel panel;
    private WateringCan[] cans;
    private GronnyVoicePlayer voice;
    private GardenControlDemonstration demonstration;
    private readonly HashSet<string> learned = new();
    private readonly HashSet<string> missingAudio = new();
    private GardenShovel[] shovels;
    private string soilInstructionId;
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
    private enum GuideStep { Move, Soil, Grab, Plant, Water, Calendar, CalendarSelect, CutStem, PickTomato, Free }
    private static GronnyTutorial active;
    private readonly HashSet<Behaviour> blockedInteractions = new();
    private float nextInteractionRefresh;
    private WateringCan refillCan;
    private int announcedWaterAttempt;
    private string backgroundInstructionId = "04_first_watering";

    private GuideStep Step
    {
        get
        {
            if (!Learned("move")) return GuideStep.Move;
            if (!Learned("grab")) return GuideStep.Grab;
            if (State == null) return GuideStep.Plant;
            if (!State.soilCovered) return GuideStep.Soil;
            if (Fruit != null && Fruit.IsRipe && !Fruit.IsPickedUp)
                return Fruit.IsStemCut ? GuideStep.PickTomato : GuideStep.CutStem;
            if (State.tomatoPhase > 0) return GuideStep.Free;
            bool open = visibility != null && visibility.IsVisible;
            if (CanEmpty) return open && Learned("calendar") ? GuideStep.CalendarSelect : GuideStep.Calendar;
            if (!Ready) return GuideStep.Water;
            if (Learned("calendar") && Learned("calendar_select")) return GuideStep.Free;
            return open && Learned("calendar") ? GuideStep.CalendarSelect : GuideStep.Calendar;
        }
    }

    public static bool Allows(GardenTutorialAction action)
    {
        if (GardenMenuController.GardenSuspended) return false;
        if (GardenWaterSupply.Active != null && GardenWaterSupply.Active.Exhausted)
            return action == GardenTutorialAction.OpenCalendar || action == GardenTutorialAction.CloseCalendar ||
                action == GardenTutorialAction.Restart;
        if (active == null || !active.Running) return true;
        GuideStep step = active.Step;
        if (step == GuideStep.Free || step == GuideStep.CutStem || step == GuideStep.PickTomato) return true;
        return action switch
        {
            GardenTutorialAction.Plant => step == GuideStep.Plant,
            GardenTutorialAction.FillSoil => step == GuideStep.Soil,
            GardenTutorialAction.Pour => step == GuideStep.Water,
            GardenTutorialAction.OpenCalendar => step == GuideStep.Calendar || step == GuideStep.CalendarSelect,
            GardenTutorialAction.Advance => step == GuideStep.CalendarSelect && active.Ready && !active.CanEmpty,
            GardenTutorialAction.Refill => step == GuideStep.CalendarSelect && active.CanEmpty,
            _ => false
        };
    }

    // Advance while the seed is held, so planting guidance precedes its release.
    public static void RecordSeedGrab(SeedItem seed)
    {
        if (GardenMenuController.GardenSuspended) return;
        if (active != null && active.Running && active.Step == GuideStep.Grab &&
            seed != null && !seed.IsPlanted && seed.Crop == SeedCrop.Tomato) active.Learn("grab");
    }

    public bool TryGetInstruction(out string text, out string detail)
    {
        text = detail = null;
        if (!Running || Step == GuideStep.Free) return false;
        bool hands = mode == GardenInputMode.Hands;
        (text, detail) = Step switch
        {
            GuideStep.Move => ("Step 1/7: Walk to the gardening table.",
                "Look for Tomato seed. Gronny will explain how to pick it up."),
            GuideStep.Soil => ("Step 4/7: Cover the seed with soil.",
                HasLoadedShovel
                    ? "Hold the loaded blade above the pot. Gently tilt it and hold briefly to pour."
                    : "Grab Shovel (pinch/Grip). Make a short pass over the soil in SoilPot.\nThen hold the loaded blade above the pot, gently tilt it and hold briefly to pour."),
            GuideStep.Grab => ("Step 2/7: Pick up Tomato seed.",
                hands ? "Pinch to grab. Keep holding the seed." : "Hold Grip to grab. Keep holding the seed."),
            GuideStep.Plant => ("Step 3/7: Plant Tomato seed.", "Release the seed over the centre of the small pot."),
            GuideStep.Water => (Excess ? "Step 5/7: Stop watering."
                : !NeedsWater ? "Step 5/7: Hold the can upright."
                : "Step 5/7: Water the soil.", Excess ? "Let the extra water drain."
                : !NeedsWater ? "Your plant has enough water." : "Stop pouring at the water target."),
            GuideStep.Calendar => ("Step 6/7: Open the calendar.",
                hands ? "Pinch with your left hand away from objects." : "Press Y on the left controller."),
            GuideStep.CutStem => ("Your tomato is ripe! Cut the stem with Pruner.",
                hands ? "Hold Pruner in your palm. Open the blades around the stem, then close your hand."
                    : "Hold Pruner with Grip. Release Trigger to open the blades around the stem, then squeeze Trigger."),
            GuideStep.PickTomato => ("Nicely done! Now pick up your tomato.",
                hands ? "Pinch to pick up the separated tomato." : "Hold Grip to pick up the separated tomato."),
            _ => (CanEmpty ? "Step 7/7: Select Refill water." : "Step 7/7: Select Advance phase.",
                hands ? "Point at the button and pinch." : "Aim at the button and press Trigger.")
        };
        return true;
    }

    public bool TryGetSpokenInstruction(out string text, out string detail)
    {
        text = detail = null;
        if (!initialized || !isActiveAndEnabled || GardenMenuController.GardenSuspended || voice == null) return false;
        if (Step == GuideStep.Soil) return false;
        // Water feedback must respond immediately even while an earlier sentence finishes.
        if (Caring && (Excess || !NeedsWater || CanEmpty)) return false;
        return voice.TryGetInstruction(out text, out detail);
    }

    private SeedsController.PlantedSeed Plant => seeds != null ? seeds.PrototypeTomato : null;
    private TomatoFruit Fruit => Plant?.seed != null ? Plant.seed.HarvestFruit : null;
    private CalendarSystem.PlantSave State => seeds != null ? seeds.GetState(Plant) : null;
    private bool Running => initialized && isActiveAndEnabled && !completed &&
        seeds != null && seeds.isActiveAndEnabled && calendar != null;
    private bool HasLoadedShovel => shovels != null && System.Array.Exists(shovels,
        shovel => shovel != null && shovel.isActiveAndEnabled && shovel.HasSoil);
    private bool Caring => Running && State != null && State.soilCovered && State.tomatoPhase < 5 && !calendar.IsAdvancing &&
        (GardenWaterSupply.Active == null || !GardenWaterSupply.Active.Exhausted);
    private bool Excess => Caring && State.retainedWaterMillilitres > seeds.ExcessLimit(Plant);
    private bool NeedsWater => Caring && seeds.TargetMillilitres(Plant) > 0f &&
        State.retainedWaterMillilitres < seeds.TargetMillilitres(Plant);
    private bool Ready => Caring && !NeedsWater && !Excess &&
        seeds.TargetMillilitres(Plant) > 0f && Time.unscaledTime - State.lastWaterAt >= 0.5f &&
        !System.Array.Exists(cans, can => can != null && can.IsHeld && can.IsTiltedToPour);
    private bool CanEmpty => cans != null && System.Array.Exists(cans,
        can => can != null && can.isActiveAndEnabled && (can.IsHeld || can == refillCan) && can.CurrentWaterLitres <= 0f);
    private bool ShowWateringDemonstration => Caring && phase == 0 && Step == GuideStep.Water &&
        NeedsWater && !Excess && !CanEmpty;
    private bool ShowCutDemonstration => Running && Fruit != null && Fruit.IsRipe && !Fruit.IsStemCut;
    private bool ShowPickDemonstration => Running && Fruit != null && Fruit.IsRipe && Fruit.IsStemCut && !Fruit.IsPickedUp;

    private IEnumerator Start()
    {
        // The welcome and progressive locks start only after Play and a released UI selection.
        while (!GardenMenuController.HasStarted || GardenMenuController.GardenSuspended) yield return null;
        if (catalogue == null) catalogue = Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial");
        seeds = FindFirstObjectByType<SeedsController>();
        calendar = seeds != null ? seeds.Calendar : null;
        if (catalogue == null || calendar == null)
        {
            Debug.LogError("Gronny tutorial needs its audio catalogue and the existing tomato calendar.", this);
            enabled = false;
            yield break;
        }
        visibility = FindFirstObjectByType<CalendarVisibilityController>();
        if (visibility != null) visibility.Close();
        panel = FindFirstObjectByType<CalendarPanel>(FindObjectsInactive.Include);
        cans = FindObjectsByType<WateringCan>(FindObjectsSortMode.None);
        shovels = FindObjectsByType<GardenShovel>(FindObjectsSortMode.None);
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (visibility == null || panel == null || rig == null)
        {
            Debug.LogError("Gronny's progressive tutorial needs the Meta camera rig and calendar controls.", this);
            enabled = false;
            yield break;
        }
        demonstration = gameObject.AddComponent<GardenControlDemonstration>();
        Transform gardeningAnchor = GetComponent<TomatoPrototypeGuide>()?.GardeningDemonstrationAnchor;
        demonstration.Configure(catalogue, gardeningAnchor);
        voice = gameObject.AddComponent<GronnyVoicePlayer>();
        voice.Configure(catalogue, demonstration);
        voice.SetBackgroundDemonstration("04_first_watering", () => ShowWateringDemonstration);
        initialized = true;
        active = this;
        if (GardenMenuController.StartsAtPlanting)
        {
            learned.Add("move");
            learned.Add("grab");
        }
        RefreshInteractionLocks();
        lastActionAt = Time.unscaledTime;
        Subscribe();
        if (!GardenMenuController.StartsAtPlanting)
            Say("01_welcome", "welcome-start", () => Running);
        RefreshPhase();
    }

    private void OnEnable()
    {
        if (!initialized) return;
        voice.enabled = true;
        active = this;
        RefreshInteractionLocks();
        Subscribe();
        lastActionAt = Time.unscaledTime;
        QueueControls();
    }

    private void Subscribe()
    {
        if (subscribed || completed) return;
        seeds.PlantingRejected += OnPlantingRejected;
        seeds.SeedCovered += OnSeedCovered;
        calendar.AdvanceBlocked += OnAdvanceBlocked;
        if (visibility != null)
        {
            visibility.VisibilityChanged += OnCalendarVisible;
        }
        if (panel != null) panel.ActionCompleted += OnCalendarAction;
        voice.AudioFinished += OnInstructionFinished;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (seeds != null) seeds.PlantingRejected -= OnPlantingRejected;
        if (seeds != null) seeds.SeedCovered -= OnSeedCovered;
        if (calendar != null) calendar.AdvanceBlocked -= OnAdvanceBlocked;
        if (visibility != null)
        {
            visibility.VisibilityChanged -= OnCalendarVisible;
        }
        if (panel != null) panel.ActionCompleted -= OnCalendarAction;
        if (voice != null) voice.AudioFinished -= OnInstructionFinished;
        subscribed = false;
    }

    private void Update()
    {
        if (GardenMenuController.GardenSuspended)
        {
            lastActionAt = Time.unscaledTime;
            return;
        }
        if (!Running)
        {
            if (initialized && !completed) voice.Clear();
            if (blockedInteractions.Count > 0) ReleaseInteractionLocks();
            return;
        }
        UpdateMode();
        foreach (WateringCan can in cans)
            if (can != null && can.IsHeld && can.CurrentWaterLitres <= 0f) refillCan = can;
        if (Step == GuideStep.Water && System.Array.Exists(cans, can => can != null && can.IsHeld)) Learn("watering_grab");
        if (Time.unscaledTime >= nextInteractionRefresh)
        {
            nextInteractionRefresh = Time.unscaledTime + 0.5f;
            RefreshInteractionLocks();
        }
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.15f;
        RefreshPhase();
        if (State == null) { QueueControls(); IdleHelp(); return; }
        GardenWaterSupply supply = GardenWaterSupply.Active;
        if (supply != null && supply.Exhausted)
        {
            // The visible game-over panel owns these announcements even if this guide is disabled.
            voice.Clear();
            return;
        }
        if (supply != null && !supply.IsPractice && announcedWaterAttempt != supply.Attempt)
        {
            announcedWaterAttempt = supply.Attempt;
            voice.FinishCurrent();
            int attempt = supply.Attempt;
            // Name the visible sprout before explaining the newly limited water supply.
            if (phase == 1) Say("08_sprout", "phase-1", () => Running && phase == 1);
            foreach (string id in new[] { "water_reserve_intro", "water_reserve_rules" })
                Say(id, id + "-" + attempt, () => Running && supply.Attempt == attempt && State.tomatoPhase < 5);
        }
        UpdateGardeningDemonstration();
        QueueHarvest();
        if (State.harvested)
        {
            // Preserve cut/pick acknowledgements even if the player delivers immediately.
            // Stale care requests already carry relevance predicates and expire in the voice queue.
            completed = true;
            ReleaseInteractionLocks();
            Unsubscribe();
            // Queue the session closing after the cut/pick acknowledgements.
            voice.Enqueue("13_complete", "complete", () => isActiveAndEnabled && State?.harvested == true);
            if (supply != null && supply.HasNoMeasuredWaste)
                voice.Enqueue("water_no_waste_complete", "no-waste", () => isActiveAndEnabled && State?.harvested == true && supply.HasNoMeasuredWaste);
            return;
        }
        if (calendar.IsAdvancing) { voice.FinishCurrent(); return; }
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
        if (CanEmpty && NeedsWater)
        {
            Help("help_refill", () => CanEmpty && NeedsWater);
            if (supply != null && !supply.IsPractice)
                Say("water_reserve_explanation", "reserve-refill-explanation", () => Caring && CanEmpty && NeedsWater);
        }
        if (supply != null)
        {
            if (supply.IsMissingSoil) Help("help_missed_pot", () => Caring && supply.IsMissingSoil);
            if (!NeedsWater && !Excess && System.Array.Exists(cans, can => can != null && can.IsPouring))
                Help("help_stop_watering", () => Caring && !NeedsWater && !Excess && System.Array.Exists(cans, can => can != null && can.IsPouring));
            if (!supply.IsPractice)
            {
                int attempt = supply.Attempt;
                Say("help_reserve_low", "low-reserve-" + attempt, () => Caring && supply.Attempt == attempt &&
                    supply.ReserveMillilitres > 0.001f && supply.ReserveMillilitres <= supply.InitialReserveMillilitres * 0.25f);
                Say("help_reserve_empty", "empty-reserve-" + attempt, () => Caring && supply.Attempt == attempt &&
                    supply.ReserveMillilitres <= 0.001f && supply.WaterInCansMillilitres > 0.001f);
                int readyPhase = phase;
                if (Ready)
                {
                    Say("water_stopped_success", "stopped-" + phase, () => Ready && phase == readyPhase);
                    Say("water_ready_next_phase", "ready-" + phase, () => Ready && phase == readyPhase);
                }
            }
        }
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
        if (!first) voice.FinishCurrent();
        int captured = phase;
        if (phase < 0)
            Say("03_plant_seed", "plant", () => Running && Step == GuideStep.Plant);
        else if (phase == 0)
        {
            soilInstructionId = null;
        }
        else if (phase < 5)
        {
            string id = phase switch
            {
                1 => "08_sprout", 2 => "09_young_plant", 3 => "10_flowering",
                _ => "11_green_fruit"
            };
            Say(id, "phase-" + phase, () => Running && State != null &&
                State.tomatoPhase == captured && !calendar.IsAdvancing);
        }
        QueueControls();
    }

    private void UpdateGardeningDemonstration()
    {
        string background = Step == GuideStep.Soil ? (HasLoadedShovel ? "soil_cover_seed" : "soil_scoop_first") :
            ShowCutDemonstration ? "harvest_cut_stem" :
            ShowPickDemonstration ? "harvest_stem_cut" : "04_first_watering";
        if (background != backgroundInstructionId)
        {
            backgroundInstructionId = background;
            System.Func<bool> relevant = background switch
            {
                "soil_scoop_first" => () => Running && Step == GuideStep.Soil && !HasLoadedShovel,
                "soil_cover_seed" => () => Running && Step == GuideStep.Soil && HasLoadedShovel,
                "harvest_cut_stem" => () => ShowCutDemonstration,
                "harvest_stem_cut" => () => ShowPickDemonstration,
                _ => () => ShowWateringDemonstration
            };
            voice.SetBackgroundDemonstration(background, relevant);
        }
    }

    private void QueueHarvest()
    {
        TomatoFruit fruit = Fruit;
        if (fruit == null) return;
        if (fruit.IsRipe && !fruit.IsStemCut)
            Say("harvest_cut_stem", "harvest-ripe", () => Running && fruit == Fruit &&
                fruit.IsRipe && !fruit.IsStemCut);
        // Keep acknowledgements in order even when cut, pickup and tray release occur
        // between tutorial updates. These flags survive recovery and tutorial completion.
        if (fruit.IsStemCut)
            Say("harvest_stem_cut", "harvest-cut", () => isActiveAndEnabled && fruit == Fruit && fruit.IsStemCut);
        if (fruit.IsPickedUp)
            Say("harvest_picked", "harvest-picked", () => isActiveAndEnabled && fruit == Fruit && fruit.IsPickedUp);
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
        QueueControls();
    }

    private bool Learned(string control) => learned.Contains(control);
    private void Learn(string control)
    {
        if (GardenMenuController.GardenSuspended) return;
        if (!Running || !learned.Add(control)) return;
        lastActionAt = Time.unscaledTime;
        RefreshInteractionLocks();
        QueueControls();
    }

    private void QueueControls()
    {
        if (!Running || calendar.IsAdvancing) return;
        GuideStep step = Step;
        // Spoken walking guidance is shared by both input modes.
        if (step == GuideStep.Move)
        {
            Say("roomscale_walk", "roomscale-walk", () => Running && Step == GuideStep.Move,
                false, true);
            return;
        }
        if (step == GuideStep.Soil)
        {
            // Select one complete instruction rather than queueing two versions of the same task.
            soilInstructionId ??= HasLoadedShovel ? "soil_cover_seed" : "soil_scoop_first";
            Say(soilInstructionId, "cover-seed", () => Running && Step == GuideStep.Soil);
            return;
        }
        if (step == GuideStep.Water && phase == 0)
            Say("water_practice_intro", "water-practice", () => Caring && phase == 0 && NeedsWater);
        if (mode == GardenInputMode.Unknown) return;
        if (step == GuideStep.Grab) Control("grab", () => Step == GuideStep.Grab);
        else if (step == GuideStep.Plant)
        {
            Say("03_plant_seed", "plant", () => Running && Step == GuideStep.Plant);
        }
        else if (step == GuideStep.Water && phase == 0)
        {
            if (!Learned("watering_grab")) Control("watering", () => Step == GuideStep.Water && !Learned("watering_grab"));
            else
            {
                Say("04_first_watering", "watering", () => Caring && phase == 0 && NeedsWater);
                Say("05_water_target", "target", () => Caring && phase == 0 && NeedsWater);
            }
        }
        else if (step == GuideStep.Calendar)
        {
            Control("calendar", () => Step == GuideStep.Calendar);
        }
        else if (step == GuideStep.CalendarSelect)
            Control(CanEmpty ? "refill_select" : "calendar_select", () => Step == GuideStep.CalendarSelect);
    }

    private void Control(string name, System.Func<bool> relevant)
    {
        GardenInputMode requestedMode = mode;
        string id = "controls_" + name + (mode == GardenInputMode.Hands ? "_hands" : "_controllers");
        Say(id, id, () => Running && mode == requestedMode && relevant(), false, true);
    }

    private void Say(string id, string key, System.Func<bool> relevant, bool once = true, bool waitForAction = false)
    {
        GronnyAudioCatalogue.Cue cue = catalogue.FindTutorialCue(id);
        if (cue == null || cue.clip == null)
        {
            if (missingAudio.Add(id)) Debug.LogWarning("Gronny recording is not assigned: " + id, this);
            return;
        }
        System.Func<bool> demoRelevant = cue.demo switch
        {
            GardenControlDemo.Water => () => ShowWateringDemonstration,
            GardenControlDemo.Upright => () => System.Array.Exists(cans, can => can != null && can.IsPouring),
            GardenControlDemo.CutStem => () => ShowCutDemonstration,
            GardenControlDemo.PickTomato => () => ShowPickDemonstration,
            GardenControlDemo.SoilScoop => () => Running && Step == GuideStep.Soil && !HasLoadedShovel,
            GardenControlDemo.SoilPour => () => Running && Step == GuideStep.Soil && HasLoadedShovel,
            _ => null
        };
        voice.Enqueue(id, key, () => (GardenWaterSupply.Active == null || !GardenWaterSupply.Active.Exhausted) && relevant(), () =>
        {
            if (cue.assisted) calendar.RecordTutorialHelp(id);
        }, once, demoRelevant, waitForAction);
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
        if (Step == GuideStep.Move || Step == GuideStep.Soil || Step == GuideStep.Grab || Step == GuideStep.Calendar ||
            Step == GuideStep.CalendarSelect) return;
        if (State == null) Help("help_planting", () => State == null);
        else if (NeedsWater) Help("help_low_water", () => NeedsWater);
        else if (Ready) Help("help_calendar", () => Ready);
    }

    private void OnPlantingRejected(SeedItem seed)
    {
        if (seed != null && seed.Crop == SeedCrop.Tomato)
            Help("help_planting", () => Running && Step == GuideStep.Plant);
    }

    private void OnSeedCovered(SeedsController.PlantedSeed plant)
    {
        if (!Running || plant != Plant) return;
        lastActionAt = Time.unscaledTime;
        // Discard stale queued planting help, while letting any spoken sentence finish.
        voice.FinishCurrent();
        Say("soil_seed_covered", "soil-covered", () => Running && State != null && State.soilCovered);
        RefreshInteractionLocks();
        QueueControls();
    }

    private void OnAdvanceBlocked(string reason)
    {
        if (!Running) return;
        if (Step == GuideStep.Move || Step == GuideStep.Soil || Step == GuideStep.Grab) return;
        if (State == null) Help("help_planting", () => State == null);
        else if (Excess) Help("help_excess_water", () => Excess);
        else if (NeedsWater) Help("help_low_water", () => NeedsWater);
    }

    private void OnCalendarVisible(bool visible)
    {
        if (!Running || !visible || (Step != GuideStep.Calendar && Step != GuideStep.CalendarSelect)) return;
        Learn("calendar");
        QueueControls();
    }

    private void OnCalendarAction(GardenTutorialAction action)
    {
        if (Running && (action == GardenTutorialAction.Advance || action == GardenTutorialAction.Refill))
            Learn("calendar_select");
    }

    private void OnInstructionFinished(string id)
    {
        // This is spoken guidance, not a measured walking challenge. Meta already
        // tracks real movement; continue to grabbing when Gronny finishes speaking.
        if (id == "roomscale_walk" && Running && Step == GuideStep.Move &&
            !GardenMenuController.GardenSuspended) Learn("move");
    }

    private void RefreshInteractionLocks()
    {
        bool free = !Running || Step == GuideStep.Free || Step == GuideStep.CutStem || Step == GuideStep.PickTomato;
        if (free) { ReleaseInteractionLocks(); return; }
        GuideStep step = Step;
        foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (behaviour.gameObject.scene != gameObject.scene) continue;
            bool allowed;
            if (behaviour is GrabInteractable || behaviour is HandGrabInteractable)
            {
                // Fruit owns its attachment lock. Tutorial cleanup must never enable
                // a ripe tomato's Meta interactables before the stalk is cut.
                if (behaviour.GetComponentInParent<TomatoFruit>() != null) continue;
                Grabbable grab = behaviour.GetComponentInParent<Grabbable>();
                SeedItem seed = grab != null ? grab.GetComponent<SeedItem>() : null;
                WateringCan can = grab != null ? grab.GetComponent<WateringCan>() : null;
                GardenShovel shovel = grab != null ? grab.GetComponent<GardenShovel>() : null;
                if (seed != null && seed.IsPlanted) { blockedInteractions.Remove(behaviour); continue; }
                allowed = (grab != null && grab.SelectingPointsCount > 0) ||
                    (grab != null && grab.GetComponent<GardenPruner>() != null) ||
                    (step == GuideStep.Soil && shovel != null) ||
                    (step != GuideStep.Move && seed != null && seed.Crop == SeedCrop.Tomato) ||
                    (Learned("grab") && State != null && State.soilCovered && can != null);
            }
            else if (behaviour is LocomotionAxisTurnerInteractor || behaviour is LocomotionTurnerInteractor ||
                behaviour is TeleportInteractor) allowed = false;
            else continue;
            if (allowed)
            {
                if (blockedInteractions.Remove(behaviour)) behaviour.enabled = true;
            }
            else if (behaviour.enabled)
            {
                blockedInteractions.Add(behaviour);
                behaviour.enabled = false;
            }
        }
        if (panel != null) panel.Refresh();
    }

    private void ReleaseInteractionLocks()
    {
        foreach (Behaviour behaviour in blockedInteractions)
        {
            if (behaviour == null) continue;
            SeedItem seed = behaviour.GetComponentInParent<SeedItem>();
            if (seed == null || !seed.IsPlanted) behaviour.enabled = true;
        }
        blockedInteractions.Clear();
        if (panel != null) panel.Refresh();
    }

    private void OnDisable()
    {
        if (active == this) active = null;
        ReleaseInteractionLocks();
        Unsubscribe();
        if (voice != null) { voice.Clear(); voice.enabled = false; }
        if (demonstration != null) demonstration.Hide();
    }
}
