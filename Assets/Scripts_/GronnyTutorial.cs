using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Locomotion;
using UnityEngine;

public enum GardenTutorialAction { Plant, Pour, OpenCalendar, Advance, Refill, RefillWithController, Recover, Restart, CloseCalendar }

/// <summary>Unlocks the first tomato instructions after the player completes each action.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class GronnyTutorial : MonoBehaviour
{
    [SerializeField] private GronnyAudioCatalogue catalogue;
    [SerializeField, TextArea] private string seedPlantingInstruction = "Pick me and plant me\non the plant pot";
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
    private readonly HashSet<int> heldObjects = new();
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
    private enum GuideStep { Move, Grab, Plant, Water, Calendar, CalendarSelect, Free }
    private static GronnyTutorial active;
    private readonly HashSet<Behaviour> blockedInteractions = new();
    private float nextInteractionRefresh;
    private Transform playerOrigin;
    private Vector3 movementStart;
    private bool movementRequested;
    private bool movementPracticeStarted;
    private WateringCan refillCan;
    private Transform viewer;
    private TextMesh seedPlantingCaption;
    private bool grabInstructionFinished;
    private int announcedWaterAttempt;

    private GuideStep Step
    {
        get
        {
            if (!Learned("move")) return GuideStep.Move;
            if (!Learned("grab")) return GuideStep.Grab;
            if (State == null) return GuideStep.Plant;
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
                action == GardenTutorialAction.Restart || action == GardenTutorialAction.Recover;
        if (active == null || !active.Running) return true;
        GuideStep step = active.Step;
        if (step == GuideStep.Free) return true;
        return action switch
        {
            GardenTutorialAction.Plant => step == GuideStep.Plant,
            GardenTutorialAction.Pour => step == GuideStep.Water,
            GardenTutorialAction.OpenCalendar => step == GuideStep.Calendar || step == GuideStep.CalendarSelect,
            GardenTutorialAction.Advance => step == GuideStep.CalendarSelect && active.Ready && !active.CanEmpty,
            GardenTutorialAction.Refill => step == GuideStep.CalendarSelect && active.CanEmpty,
            _ => false
        };
    }

    // SeedItem detects release before its planting attempt, including grabs shorter than the polling interval.
    public static void RecordSeedRelease(SeedItem seed)
    {
        if (GardenMenuController.GardenSuspended) return;
        if (active != null && active.Running && active.Step == GuideStep.Grab &&
            seed != null && seed.Crop == SeedCrop.Tomato) active.Learn("grab");
    }

    public bool TryGetInstruction(out string text, out string detail)
    {
        text = detail = null;
        if (!Running || Step == GuideStep.Free) return false;
        bool hands = mode == GardenInputMode.Hands;
        (text, detail) = Step switch
        {
            GuideStep.Move => ("Step 1/6: Practice moving.", mode == GardenInputMode.Unknown
                ? "Use your tracked hands or Touch controllers to begin."
                : hands ? "Curl your index finger, tap your thumb, then swipe to step." : "Move with the left joystick."),
            GuideStep.Grab => ("Step 2/6: Pick up and release the seed labelled Tomato.",
                hands ? "Pinch to grab. Open your fingers to release." : "Hold Grip to grab. Release Grip to let go."),
            GuideStep.Plant => ("Step 3/6: Plant the seed labelled Tomato.", "Drop it onto the soil in an empty small pot."),
            GuideStep.Water => (Excess ? "Step 4/6: Stop watering and wait for drainage."
                : !NeedsWater ? "Step 4/6: Hold the watering can upright."
                : "Step 4/6: Water the soil near the roots.", "Reach the water target, then stop pouring."),
            GuideStep.Calendar => ("Step 5/6: Open the calendar.",
                hands ? "Pinch with your left hand away from objects." : "Press Y on the left controller."),
            _ => (CanEmpty ? "Step 6/6: Select Refill water." : "Step 6/6: Select Advance phase.",
                hands ? "Point at the button and pinch." : "Aim at the button and press Trigger.")
        };
        return true;
    }

    private SeedsController.PlantedSeed Plant => seeds != null ? seeds.PrototypeTomato : null;
    private CalendarSystem.PlantSave State => seeds != null ? seeds.GetState(Plant) : null;
    private bool Running => initialized && isActiveAndEnabled && !completed &&
        seeds != null && seeds.isActiveAndEnabled && calendar != null;
    private bool Caring => Running && State != null && State.tomatoPhase < 5 && !calendar.IsAdvancing &&
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
        seedItems = FindObjectsByType<SeedItem>(FindObjectsSortMode.None);
        locomotors = FindObjectsByType<FirstPersonLocomotor>(FindObjectsSortMode.None);
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (visibility == null || panel == null || rig == null || locomotors.Length == 0)
        {
            Debug.LogError("Gronny's progressive tutorial needs the Meta camera rig, locomotion and calendar controls.", this);
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
        playerOrigin = rig.transform;
        viewer = rig.centerEyeAnchor;
        movementStart = playerOrigin.position;
        if (GardenMenuController.StartsAtPlanting)
        {
            learned.Add("move");
            learned.Add("grab");
            grabInstructionFinished = true;
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
        voice.AudioFinished += OnAudioFinished;
        seeds.PlantingRejected += OnPlantingRejected;
        calendar.AdvanceBlocked += OnAdvanceBlocked;
        if (visibility != null)
        {
            visibility.VisibilityChanged += OnCalendarVisible;
        }
        if (panel != null) panel.ActionCompleted += OnCalendarAction;
        foreach (FirstPersonLocomotor locomotor in locomotors)
            if (locomotor != null) locomotor.WhenLocomotionEventHandled += OnMoved;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (voice != null) voice.AudioFinished -= OnAudioFinished;
        if (seeds != null) seeds.PlantingRejected -= OnPlantingRejected;
        if (calendar != null) calendar.AdvanceBlocked -= OnAdvanceBlocked;
        if (visibility != null)
        {
            visibility.VisibilityChanged -= OnCalendarVisible;
        }
        if (panel != null) panel.ActionCompleted -= OnCalendarAction;
        foreach (FirstPersonLocomotor locomotor in locomotors)
            if (locomotor != null) locomotor.WhenLocomotionEventHandled -= OnMoved;
        subscribed = false;
    }

    private void Update()
    {
        if (GardenMenuController.GardenSuspended)
        {
            lastActionAt = Time.unscaledTime;
            heldObjects.Clear();
            movementRequested = false;
            if (playerOrigin != null) movementStart = playerOrigin.position;
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
        ObserveGrabs();
        if (Step == GuideStep.Move && movementPracticeStarted && movementRequested && playerOrigin != null &&
            Vector3.ProjectOnPlane(playerOrigin.position - movementStart, Vector3.up).sqrMagnitude >= 0.01f)
            Learn("move");
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
        if (State.harvested)
        {
            voice.FinishCurrent();
            completed = true;
            ReleaseInteractionLocks();
            Unsubscribe();
            // The closing line is the only request allowed after tutorial completion.
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

    private void LateUpdate()
    {
        SeedItem target = null;
        if (!GardenMenuController.GardenSuspended && Running && grabInstructionFinished && State == null && !calendar.IsAdvancing)
        {
            foreach (SeedItem seed in seedItems)
            {
                if (seed == null || seed.Crop != SeedCrop.Tomato || seed.IsPlanted ||
                    !seed.isActiveAndEnabled || seed.gameObject.scene != gameObject.scene) continue;
                target = seed;
                break;
            }
        }
        if (target == null || viewer == null)
        {
            if (seedPlantingCaption != null) seedPlantingCaption.gameObject.SetActive(false);
            return;
        }
        if (seedPlantingCaption == null)
        {
            // Keep world scale independent of the seed and the guide's scaled canvas.
            GameObject caption = new("Tomato seed planting instruction");
            caption.layer = 2;
            seedPlantingCaption = caption.AddComponent<TextMesh>();
            seedPlantingCaption.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            MeshRenderer renderer = caption.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = seedPlantingCaption.font.material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            seedPlantingCaption.anchor = TextAnchor.MiddleCenter;
            seedPlantingCaption.alignment = TextAlignment.Center;
            seedPlantingCaption.characterSize = 0.006f;
            seedPlantingCaption.fontSize = 48;
            seedPlantingCaption.color = Color.white;
        }
        seedPlantingCaption.text = seedPlantingInstruction;
        seedPlantingCaption.transform.position = target.transform.position + Vector3.up * 0.16f;
        Vector3 direction = seedPlantingCaption.transform.position - viewer.position;
        if (direction.sqrMagnitude > 0.0001f)
            seedPlantingCaption.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        seedPlantingCaption.gameObject.SetActive(true);
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
            Say("water_practice_intro", "water-practice", () => Caring && phase == 0 && NeedsWater);
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

    private void OnAudioFinished(string id)
    {
        if (id == "controls_grab_hands" || id == "controls_grab_controllers")
            grabInstructionFinished = true;
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
        movementRequested = false;
        movementPracticeStarted = false;
        if (playerOrigin != null) movementStart = playerOrigin.position;
        QueueControls();
    }

    private bool Learned(string control) => learned.Contains(control);
    private void Learn(string control)
    {
        if (GardenMenuController.GardenSuspended) return;
        if (!Running || !learned.Add(control)) return;
        lastActionAt = Time.unscaledTime;
        if (control == "move")
            Say("movement_success", "movement-success", () => Running && State == null);
        RefreshInteractionLocks();
        QueueControls();
    }

    private void QueueControls()
    {
        if (!Running || mode == GardenInputMode.Unknown || calendar.IsAdvancing) return;
        GuideStep step = Step;
        if (step == GuideStep.Move) Control("move", () => Step == GuideStep.Move);
        else if (step == GuideStep.Grab) Control("grab", () => Step == GuideStep.Grab);
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
        System.Func<bool> demoRelevant = catalogue.FindTutorialCue(id)?.demo switch
        {
            GardenControlDemo.Water => () => ShowWateringDemonstration,
            GardenControlDemo.Upright => () => System.Array.Exists(cans, can => can != null && can.IsPouring),
            _ => null
        };
        voice.Enqueue(id, key, () => (GardenWaterSupply.Active == null || !GardenWaterSupply.Active.Exhausted) && relevant(), () =>
        {
            // Movement before its own lesson must not skip the demonstration.
            if ((id == "controls_move_hands" || id == "controls_move_controllers") && Step == GuideStep.Move)
            {
                movementPracticeStarted = true;
                movementRequested = false;
                if (playerOrigin != null) movementStart = playerOrigin.position;
            }
            if (catalogue.FindTutorialCue(id).assisted) calendar.RecordTutorialHelp(id);
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
        if (Step == GuideStep.Move || Step == GuideStep.Grab || Step == GuideStep.Calendar ||
            Step == GuideStep.CalendarSelect) return;
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
        if (Step == GuideStep.Move || Step == GuideStep.Grab) return;
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

    private void OnMoved(LocomotionEvent evt, Pose delta)
    {
        if (!GardenMenuController.GardenSuspended && Running && movementPracticeStarted && Step == GuideStep.Move &&
            (evt.Translation == LocomotionEvent.TranslationType.Relative ||
             evt.Translation == LocomotionEvent.TranslationType.Velocity) &&
            Vector3.ProjectOnPlane(delta.position, Vector3.up).sqrMagnitude > 0.00000001f)
            movementRequested = true;
    }

    private void ObserveGrabs()
    {
        if (Step != GuideStep.Grab) return;
        foreach (SeedItem seed in seedItems)
        {
            if (seed == null || seed.Crop != SeedCrop.Tomato) continue;
            Grabbable grabbable = seed.GetComponent<Grabbable>();
            ObserveObject(seed.GetInstanceID(), grabbable != null && grabbable.SelectingPointsCount > 0);
        }
        foreach (WateringCan can in cans)
            if (can != null) ObserveObject(can.GetInstanceID(), can.IsHeld);
    }

    private void ObserveObject(int id, bool held)
    {
        if (held) heldObjects.Add(id);
        else if (heldObjects.Remove(id) && Step == GuideStep.Grab) Learn("grab");
    }

    private void RefreshInteractionLocks()
    {
        bool free = !Running || Step == GuideStep.Free;
        if (free) { ReleaseInteractionLocks(); return; }
        GuideStep step = Step;
        foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (behaviour.gameObject.scene != gameObject.scene) continue;
            bool allowed;
            if (behaviour is GrabInteractable || behaviour is HandGrabInteractable)
            {
                Grabbable grab = behaviour.GetComponentInParent<Grabbable>();
                SeedItem seed = grab != null ? grab.GetComponent<SeedItem>() : null;
                WateringCan can = grab != null ? grab.GetComponent<WateringCan>() : null;
                if (seed != null && seed.IsPlanted) { blockedInteractions.Remove(behaviour); continue; }
                allowed = (grab != null && grab.SelectingPointsCount > 0) ||
                    (step != GuideStep.Move && seed != null && seed.Crop == SeedCrop.Tomato) ||
                    (Learned("grab") && State != null && can != null);
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
        if (seedPlantingCaption != null) seedPlantingCaption.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (seedPlantingCaption != null) Destroy(seedPlantingCaption.gameObject);
    }
}
