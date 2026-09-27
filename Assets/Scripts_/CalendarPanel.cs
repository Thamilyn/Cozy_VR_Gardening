using System;
using UnityEngine;
using UnityEngine.UI;

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

    private void Awake()
    {
        if (calendar == null) calendar = FindFirstObjectByType<CalendarSystem>();
        if (advanceButton != null) advanceButton.onClick.AddListener(AdvancePhase);
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
        if (calendar != null) calendar.AdvancePhase();
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
        if (advanceButton != null) advanceButton.interactable = !calendar.IsCycleComplete;
        if (buttonText != null) buttonText.text = calendar.IsCycleComplete ? completeLabel : advanceLabel;
        if (buttonBackground != null) buttonBackground.color =
            calendar.IsCycleComplete ? buttonDisabledColor : buttonEnabledColor;

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
    }
}
