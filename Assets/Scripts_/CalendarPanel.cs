using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;

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

    private void Awake()
    {
        if (calendar == null) calendar = FindFirstObjectByType<CalendarSystem>();
        if (advanceButton != null) advanceButton.onClick.AddListener(AdvancePhase);
        if (gameObject.scene.name == "Garden_Moves" && advanceButton != null) AddPrototypeControls();
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

    private void AddPrototypeControls()
    {
        restartButton = MakeButton("Restart", "Restart", 285f);
        restartButton.onClick.AddListener(RestartPrototype);
        refillButton = MakeButton("Refill", "Refill water", -95f);
        refillButton.onClick.AddListener(RefillCans);
        Button recoverButton = MakeButton("RecoverCan", "Recall can", 95f);
        recoverButton.onClick.AddListener(RecoverCan);
        RectTransform rect = advanceButton.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(-285f, -315f);
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
        foreach (WateringCan can in FindObjectsByType<WateringCan>(FindObjectsSortMode.None)) can.Refill();
        FindFirstObjectByType<TomatoPrototypeGuide>()?.Say("Watering cans refilled. Refilling does not count as water applied to the soil.");
    }

    public void RecoverCan()
    {
        TomatoPrototypeGuide guide = FindFirstObjectByType<TomatoPrototypeGuide>();
        if (!TryGetRecoveryHand(Handedness.Right, out Vector3 position) &&
            !TryGetRecoveryHand(Handedness.Left, out position))
        {
            guide?.Say("Keep one hand free and visible to recall the watering can.");
            return;
        }

        WateringCan closest = null;
        float closestDistance = float.PositiveInfinity;
        float lastReleasedAt = float.NegativeInfinity;
        foreach (WateringCan can in FindObjectsByType<WateringCan>(FindObjectsSortMode.None))
        {
            if (can.IsHeld) continue;
            float distance = (can.transform.position - position).sqrMagnitude;
            if (can.LastReleasedAt < lastReleasedAt ||
                (can.LastReleasedAt == lastReleasedAt && distance >= closestDistance)) continue;
            closest = can;
            closestDistance = distance;
            lastReleasedAt = can.LastReleasedAt;
        }
        if (closest == null)
        {
            guide?.Say("There is no released watering can to recall.");
            return;
        }

        Vector3 forward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
        forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        closest.RecoverToHand(position + forward * 0.15f);
        guide?.Say("The watering can is beside your hand. Grab it to continue; its water is unchanged.");
    }

    private bool TryGetRecoveryHand(Handedness side, out Vector3 position)
    {
        position = default;
        HandGrabInteractor[] hands = FindObjectsByType<HandGrabInteractor>(FindObjectsSortMode.None);
        GrabInteractor[] controllers = FindObjectsByType<GrabInteractor>(FindObjectsSortMode.None);
        // Several Meta interactors can share a hand; any selected one makes it busy.
        foreach (HandGrabInteractor interactor in hands)
            if (interactor.State == InteractorState.Select && interactor.Hand != null &&
                interactor.Hand.Handedness == side) return false;
        foreach (GrabInteractor interactor in controllers)
        {
            ControllerRef controller = interactor.GetComponentInParent<ControllerRef>();
            if (interactor.State == InteractorState.Select && controller != null &&
                controller.Handedness == side) return false;
        }
        foreach (HandGrabInteractor interactor in hands)
        {
            IHand hand = interactor.Hand;
            if (!interactor.isActiveAndEnabled || interactor.State == InteractorState.Disabled ||
                interactor.State == InteractorState.Select || hand == null || hand.Handedness != side ||
                !hand.IsConnected || !hand.IsTrackedDataValid || !hand.IsHighConfidence ||
                !hand.GetRootPose(out Pose pose)) continue;
            position = pose.position;
            return true;
        }
        foreach (GrabInteractor interactor in controllers)
        {
            ControllerRef controller = interactor.GetComponentInParent<ControllerRef>();
            if (!interactor.isActiveAndEnabled || interactor.State == InteractorState.Disabled ||
                interactor.State == InteractorState.Select || controller == null || controller.Handedness != side ||
                !controller.IsConnected || !controller.IsPoseValid || !controller.TryGetPose(out Pose pose)) continue;
            position = pose.position;
            return true;
        }
        return false;
    }

    public void RestartPrototype()
    {
        if (restarting) return;
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
        if (advanceButton != null) advanceButton.interactable = !calendar.IsCycleComplete && !calendar.IsAdvancing;
        if (buttonText != null) buttonText.text = calendar.IsAdvancing ? "Time passing" : restartButton != null
            ? calendar.IsCycleComplete ? "Harvest" : "Advance phase"
            : calendar.IsCycleComplete ? completeLabel : advanceLabel;
        if (buttonBackground != null) buttonBackground.color =
            calendar.IsCycleComplete || calendar.IsAdvancing ? buttonDisabledColor : buttonEnabledColor;

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
        if (restartButton != null) restartButton.interactable = !restarting;
    }
}
