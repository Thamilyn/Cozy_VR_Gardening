using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Oculus.Interaction;

/// <summary>Editable world-space calendar presentation. The UI Button is driven by Meta's PointableCanvas.</summary>
[DisallowMultipleComponent]
public sealed class CalendarPanel : MonoBehaviour
{
    [Serializable]
    public sealed class MilestoneView
    {
        public Text dayText;
        public Text nameText;
        public Image icon;
        public Image background;
    }

    [SerializeField] private CalendarSystem calendar;
    [SerializeField] private Text currentDayText;
    [SerializeField] private Text currentPhaseText;
    [SerializeField] private Text approximateDatesText;
    [SerializeField] private Text waterSummaryText;
    [SerializeField] private Text buttonText;
    [SerializeField] private Button advanceButton;
    [SerializeField] private Image buttonBackground;
    [SerializeField] private MilestoneView[] milestoneViews = new MilestoneView[6];
    [Header("Editable labels and styles")]
    [SerializeField, Tooltip("Keep off until milestone icons have their own space beside the phase names.")]
    private bool showMilestoneIcons;
    [SerializeField] private string dayPrefix = "Day ";
    [SerializeField] private string approximateDatesLabel = "Approximate dates · tomato growth cycle";
    [SerializeField] private string advanceLabel = "Advance phase";
    [SerializeField] private string completeLabel = "Cycle complete";
    [SerializeField] private string waterPrefix = "Total water: ";
    [SerializeField] private Color futureColor = new(0.19f, 0.24f, 0.22f, 1f);
    [SerializeField] private Color completedColor = new(0.29f, 0.47f, 0.32f, 1f);
    [SerializeField] private Color buttonEnabledColor = new(0.21f, 0.51f, 0.3f, 1f);
    [SerializeField] private Color buttonDisabledColor = new(0.35f, 0.37f, 0.35f, 1f);
    private Button restartButton;
    private Button refillButton;
    private bool restarting;
    public event Action ButtonSelected;
    public event Action<GardenTutorialAction> ActionCompleted;

    private void Awake()
    {
        if (calendar == null) calendar = FindFirstObjectByType<CalendarSystem>();
        if (advanceButton != null) advanceButton.onClick.AddListener(AdvancePhase);
        if (gameObject.scene.name == "Garden_Moves" && advanceButton != null) AddPrototypeControls();
        GardenRoundedPanel.ApplyToHierarchy(transform);
    }

    private void OnEnable()
    {
        if (calendar != null) calendar.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (calendar != null) calendar.Changed -= Refresh;
    }

    private void OnDestroy()
    {
        if (advanceButton != null) advanceButton.onClick.RemoveListener(AdvancePhase);
    }

    public void AdvancePhase()
    {
        if (calendar == null || calendar.IsAdvancing || !GronnyTutorial.Allows(GardenTutorialAction.Advance)) return;
        int previousDay = calendar.CurrentDay;
        calendar.AdvancePhase();
        if (calendar.IsAdvancing || calendar.CurrentDay != previousDay) CompleteAction(GardenTutorialAction.Advance);
    }

    private void AddPrototypeControls()
    {
        restartButton = MakeButton("Restart", "Restart", 190f);
        restartButton.onClick.AddListener(RestartPrototype);
        refillButton = MakeButton("Refill", "Refill water", 0f);
        refillButton.onClick.AddListener(RefillCans);
        RectTransform rect = advanceButton.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(-190f, -315f);
        rect.sizeDelta = new Vector2(170f, 65f);
        ResizeButtonLabel(advanceButton);
    }

    private Button MakeButton(string objectName, string label, float x)
    {
        Button button = Instantiate(advanceButton, advanceButton.transform.parent);
        button.name = objectName;
        // Do not copy serialized calendar callbacks to the new controls.
        button.onClick = new Button.ButtonClickedEvent();
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, -315f);
        rect.sizeDelta = new Vector2(170f, 65f);
        Text text = button.GetComponentInChildren<Text>();
        if (text != null) text.text = label;
        ResizeButtonLabel(button);
        return button;
    }

    private void ResizeButtonLabel(Button button)
    {
        Text text = button.GetComponentInChildren<Text>();
        if (text == null) return;
        text.rectTransform.sizeDelta = new Vector2(160f, 58f);
        text.fontSize = 20;
    }

    public void RefillCans()
    {
        if (!GronnyTutorial.Allows(GardenTutorialAction.Refill)) return;
        bool refilled = false;
        foreach (WateringCan can in FindObjectsByType<WateringCan>(FindObjectsSortMode.None))
        {
            float before = can.CurrentWaterLitres;
            can.Refill();
            refilled |= can.CurrentWaterLitres > before;
        }
        if (refilled) CompleteAction(GardenTutorialAction.Refill);
        FindFirstObjectByType<TomatoPrototypeGuide>()?.Say(refilled
            ? "Water transferred to the can. Refilling is not water applied to the soil."
            : "No water transferred. Check the reserve and the water already in your can.");
    }

    public void RestartPrototype()
    {
        if (restarting || !GronnyTutorial.Allows(GardenTutorialAction.Restart)) return;
        if (GardenWaterSupply.Active != null && GardenWaterSupply.Active.Exhausted)
        {
            if (GardenWaterSupply.Active.Retry())
            {
                restarting = true;
                if (restartButton != null) restartButton.interactable = false;
            }
            return;
        }
        foreach (Grabbable grab in FindObjectsByType<Grabbable>(FindObjectsSortMode.None))
        {
            if (grab.SelectingPointsCount == 0) continue;
            FindFirstObjectByType<TomatoPrototypeGuide>()?.Say("Release all objects and press Restart for the next player.");
            return;
        }
        Scene scene = gameObject.scene;
        if (!Application.CanStreamedLevelBeLoaded(scene.path))
        {
            FindFirstObjectByType<TomatoPrototypeGuide>()?.Say("Add Garden_Moves to Build Settings to enable restarting.");
            return;
        }
        restarting = true;
        CompleteAction(GardenTutorialAction.Restart);
        if (restartButton != null) restartButton.interactable = false;
        SceneManager.LoadSceneAsync(scene.path, LoadSceneMode.Single);
    }

    public void Refresh()
    {
        if (calendar == null) return;
        int phase = calendar.CurrentPhaseIndex;
        var milestones = calendar.Milestones;
        if (currentDayText != null) currentDayText.text = dayPrefix + calendar.CurrentDay;
        if (currentPhaseText != null && milestones.Count > 0)
            currentPhaseText.text = milestones[Mathf.Clamp(phase, 0, milestones.Count - 1)].displayName;
        if (approximateDatesText != null) approximateDatesText.text = approximateDatesLabel;
        bool canAdvance = !calendar.IsCycleComplete && !calendar.IsAdvancing &&
            GronnyTutorial.Allows(GardenTutorialAction.Advance);
        if (advanceButton != null) advanceButton.interactable = canAdvance;
        if (buttonText != null) buttonText.text = calendar.IsAdvancing ? "Time passing" : restartButton != null
            ? calendar.IsCycleComplete ? "Harvest" : "Advance phase"
            : calendar.IsCycleComplete ? completeLabel : advanceLabel;
        if (buttonBackground != null) buttonBackground.color =
            canAdvance ? buttonEnabledColor : buttonDisabledColor;

        for (int i = 0; i < milestoneViews.Length; i++)
        {
            MilestoneView view = milestoneViews[i];
            if (view == null) continue;
            bool exists = i < milestones.Count;
            if (view.background != null) view.background.gameObject.SetActive(exists);
            if (!exists) continue;
            CalendarSystem.Milestone milestone = milestones[i];
            if (view.dayText != null) view.dayText.text = "≈ " + milestone.day;
            if (view.nameText != null) view.nameText.text = milestone.displayName;
            if (view.background != null) view.background.color =
                i == phase ? milestone.color : i < phase ? completedColor : futureColor;
            if (view.icon != null)
            {
                view.icon.sprite = milestone.icon;
                view.icon.color = milestone.color;
                view.icon.enabled = true;
                view.icon.gameObject.SetActive(showMilestoneIcons && milestone.icon != null);
            }
        }

        if (waterSummaryText != null)
        {
            float total = 0f;
            foreach (CalendarSystem.PlantSave plant in calendar.Plants) total += plant.waterMillilitres;
            waterSummaryText.text = waterPrefix + total.ToString("0") + " ml";
        }
        if (restartButton != null) restartButton.interactable = !restarting && GronnyTutorial.Allows(GardenTutorialAction.Restart);
        if (restartButton != null)
        {
            Text label = restartButton.GetComponentInChildren<Text>();
            if (label != null) label.text = GardenWaterSupply.Active != null && GardenWaterSupply.Active.Exhausted ? "Try again" : "Restart";
        }
        if (refillButton != null) refillButton.interactable = GronnyTutorial.Allows(GardenTutorialAction.Refill);
    }

    private void CompleteAction(GardenTutorialAction action)
    {
        ActionCompleted?.Invoke(action);
        ButtonSelected?.Invoke();
        Refresh();
    }
}
