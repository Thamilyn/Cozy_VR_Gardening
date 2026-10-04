using UnityEngine;
using UnityEngine.UI;

/// <summary>Short visual coaching and measured water feedback for the tomato activity.</summary>
[DisallowMultipleComponent]
public sealed class TomatoPrototypeGuide : MonoBehaviour
{
    [SerializeField] private SeedsController seeds;
    [Header("Editable scene panel")]
    [SerializeField] private Text instruction;
    [SerializeField] private Text water;
    [SerializeField] private bool facePlayer = true;
    [SerializeField] private bool followPlantedPot;
    [SerializeField] private Vector3 potPanelOffset = new(0.25f, 0.46f, 0f);
    [SerializeField, TextArea] private string welcomeInstruction = "Pick up the red tomato seed.\nDrop it onto the soil in an empty small pot.";
    [SerializeField, TextArea] private string initialWaterInformation = "One seed per pot.\nGrab: pinch or press Grip.\nCalendar: left-hand pinch away from objects, or Y.";
    [SerializeField, Min(5f)] private float idleHintSeconds = 25f;
    [Header("Water feedback colors")]
    [SerializeField] private Color excessWaterColor = new(1f, 0.55f, 0.5f, 1f);
    [SerializeField] private Color drainedExcessColor = new(1f, 0.75f, 0.4f, 1f);
    [SerializeField] private Color withinTargetColor = new(0.55f, 1f, 0.65f, 1f);
    private Color neutralWaterColor;
    private CalendarSystem calendar;
    private Transform viewer;
    private string message;
    private float messageUntil;
    private float lastActionAt;
    private float lastTotalWater;
    private int lastPhase = -1;
    private bool successShown;
    private float nextRefresh;
    private GronnyTutorial audioTutorial;

    private void Start()
    {
        if (instruction == null || water == null)
        {
            Debug.LogError("Assign both scene text labels to the tomato guide.", this);
            enabled = false;
            return;
        }
        if (seeds == null) seeds = FindFirstObjectByType<SeedsController>();
        if (seeds == null || seeds.Calendar == null) { enabled = false; return; }
        neutralWaterColor = water.color;
        calendar = seeds.Calendar;
        seeds.Guidance += Say;
        calendar.AdvanceBlocked += Say;
        if (gameObject.scene.name == "Garden_Moves")
            audioTutorial = GetComponent<GronnyTutorial>() ?? gameObject.AddComponent<GronnyTutorial>();
        GameObject eye = GameObject.Find("CenterEyeAnchor");
        viewer = eye != null ? eye.transform : Camera.main != null ? Camera.main.transform : null;
        Say(welcomeInstruction);
    }

    public void Say(string text)
    {
        message = text;
        messageUntil = Time.unscaledTime + 6f;
        lastActionAt = Time.unscaledTime;
    }

    private void LateUpdate()
    {
        SeedsController.PlantedSeed plant = seeds.PrototypeTomato;
        if (followPlantedPot && plant != null && plant.pot != null)
            transform.position = plant.pot.PlantingPoint + potPanelOffset;
        if (viewer == null && Camera.main != null) viewer = Camera.main.transform;
        if (facePlayer && viewer != null)
        {
            Vector3 direction = transform.position - viewer.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(direction);
        }
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.25f;
        Refresh(plant);
    }

    private void Refresh(SeedsController.PlantedSeed plant)
    {
        if (calendar.IsAdvancing)
        {
            water.color = neutralWaterColor;
            instruction.text = "Watch your tomato grow.";
            water.text = "Time is passing.\nWatering is paused.";
            return;
        }
        if (plant == null)
        {
            water.color = neutralWaterColor;
            instruction.text = message;
            water.text = initialWaterInformation;
            return;
        }
        CalendarSystem.PlantSave saved = seeds.GetState(plant);
        if (saved == null) return;
        if (saved.waterMillilitres != lastTotalWater || saved.tomatoPhase != lastPhase)
        {
            // A planting or blocked-advance hint must not ask for more water after watering succeeds.
            if (saved.waterMillilitres != lastTotalWater) messageUntil = 0f;
            lastActionAt = Time.unscaledTime;
            lastTotalWater = saved.waterMillilitres;
            lastPhase = saved.tomatoPhase;
        }
        if (saved.harvested && !successShown)
        {
            successShown = true;
            Say("Well done! You harvested a tomato.\nCalendar: Restart to play again.");
        }
        bool excess = saved.retainedWaterMillilitres > seeds.ExcessLimit(plant) && saved.tomatoPhase < 5;
        bool pouring = Time.unscaledTime - saved.lastWaterAt < 0.5f;
        string prompt = saved.harvested ? "Well done! You harvested a tomato.\nCalendar: Restart to play again." :
            saved.tomatoPhase >= 5 ? "Pick the largest red tomato.\nPlace it in the harvest tray." :
            excess ? "Stop watering.\nLet the extra water drain away." :
            saved.retainedWaterMillilitres >= seeds.TargetMillilitres(plant)
                ? "Enough water! Hold the can upright.\nCalendar: Advance phase."
                : saved.tomatoPhase == 4
                    ? "You're in charge.\nCheck what your plant needs."
                    : "Water the soil near the roots.\nKeep the water off the leaves.";
        if (!pouring && Time.unscaledTime < messageUntil && !excess) prompt = message;
        if (!pouring && Time.unscaledTime - lastActionAt > idleHintSeconds && !saved.harvested)
            prompt += "\nCalendar: left-hand pinch away from objects, or Y.";
        instruction.text = prompt;
        float target = seeds.TargetMillilitres(plant);
        string status = WaterFeedbackStatus(plant, saved, target, excess);
        water.text = status + "\nPour: " + saved.currentPourMillilitres.ToString("0.0") + " ml | Stage: " + saved.phaseWaterMillilitres.ToString("0.0") + " ml\n" +
            (saved.tomatoPhase < 5 ? "Target: " + target.ToString("0.0") + " ml | Limit: " + seeds.ExcessLimit(plant).ToString("0.0") + " ml\n" : "") +
            "Total: " + saved.waterMillilitres.ToString("0.0") + " ml";
    }

    private string WaterFeedbackStatus(SeedsController.PlantedSeed plant, CalendarSystem.PlantSave saved,
        float target, bool excess)
    {
        water.color = neutralWaterColor;
        if (saved.harvested) return "HARVEST COMPLETE";
        if (saved.tomatoPhase >= 5) return "READY TO HARVEST";
        if (target <= 0f) return seeds.WaterStatus(plant);
        if (excess)
        {
            water.color = excessWaterColor;
            return "TOO MUCH WATER";
        }
        // Keep the excess visible after drainage; recovery must not erase the watering mistake.
        if (saved.phaseWaterMillilitres > seeds.ExcessLimit(plant))
        {
            water.color = drainedExcessColor;
            return "EXCESS WATER DRAINED";
        }
        if (saved.retainedWaterMillilitres < target) return "WATER BELOW TARGET";
        water.color = withinTargetColor;
        return "WATER IN RANGE";
    }

    private void OnDestroy()
    {
        if (seeds != null) seeds.Guidance -= Say;
        if (calendar != null) calendar.AdvanceBlocked -= Say;
    }

    private void OnEnable()
    {
        if (audioTutorial != null) audioTutorial.enabled = true;
    }

    private void OnDisable()
    {
        if (audioTutorial != null) audioTutorial.enabled = false;
    }
}
