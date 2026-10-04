using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Keeps the Canvas root listening for Y and left-hand pinches while the panel is hidden.</summary>
[DisallowMultipleComponent]
public sealed class CalendarVisibilityController : MonoBehaviour
{
    [Header("Canvas and panel")]
    [SerializeField] private GameObject panelContent;
    [SerializeField] private Collider rayCollider;
    [SerializeField] private CalendarPanel calendarPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private bool startVisible;

    [Header("Placement when opened")]
    [SerializeField, Tooltip("Assign CenterEyeAnchor in the scene. Uses the Main Camera if left empty.")]
    private Transform viewTransform;
    [SerializeField, Min(0.25f)] private float distanceFromView = 1.3f;

    [Header("Hand gestures")]
    [Tooltip("Left tracked hand. Pinch away from the calendar to toggle it; point at it to use its buttons.")]
    public OVRHand leftHand;
    [SerializeField, Min(0.01f)] private float pinchReleaseDuration = 0.1f;

    private HandGrabInteractor[] handGrabInteractors;
    private bool pinchArmed;
    private float releaseStartedAt = float.NaN;
    private float nextInteractorSearch;

    public bool IsVisible { get; private set; }
    public event System.Action<bool> VisibilityChanged;
    public event System.Action CloseSelected;

    private void Awake()
    {
        // A world-space, pointable Canvas must not inherit a screen-space Canvas scale.
        if (transform.parent != null) transform.SetParent(null, false);
        if (closeButton != null) closeButton.onClick.AddListener(SelectClose);
        SetVisible(startVisible);
    }

    private void LateUpdate()
    {
        // Read grab state after the Meta interactors have processed this frame.
        bool validHand = leftHand != null && leftHand.IsDataValid && leftHand.IsTracked &&
            leftHand.HandConfidence == OVRHand.TrackingConfidence.High &&
            leftHand.GetFingerConfidence(OVRHand.HandFinger.Index) == OVRHand.TrackingConfidence.High &&
            !leftHand.IsSystemGestureInProgress;
        bool busy = IsHandInteractionBusy();
        bool pinching = validHand && leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        float strength = validHand ? leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) : 0f;
        bool pinchStarted = UpdatePinchState(validHand && !busy, pinching, strength, Time.unscaledTime);

        // UI ray pinches go to the calendar buttons; object grabs consume the gesture.
        if (OVRInput.GetDown(OVRInput.RawButton.Y, OVRInput.Controller.LTouch) ||
            (pinchStarted && (!IsVisible || !IsPointingAtCalendar())))
            Toggle();
    }

    private bool IsHandInteractionBusy()
    {
        if ((handGrabInteractors == null || handGrabInteractors.Length == 0) &&
            Time.unscaledTime >= nextInteractorSearch)
        {
            handGrabInteractors = FindObjectsByType<HandGrabInteractor>(FindObjectsSortMode.None);
            nextInteractorSearch = Time.unscaledTime + 1f;
        }

        if (handGrabInteractors == null) return false;
        foreach (HandGrabInteractor interactor in handGrabInteractors)
        {
            if (interactor == null || !interactor.isActiveAndEnabled) continue;
            if (interactor.HasSelectedInteractable ||
                (interactor.Hand != null && interactor.Hand.Handedness == Handedness.Left &&
                 interactor.HasInteractable)) return true;
        }
        return false;
    }

    private bool UpdatePinchState(bool valid, bool pinching, float strength, float now)
    {
        if (!valid)
        {
            pinchArmed = false;
            releaseStartedAt = float.NaN;
            return false;
        }

        if (pinching || strength > 0.3f)
        {
            releaseStartedAt = float.NaN;
            if (!pinching || !pinchArmed) return false;
            pinchArmed = false;
            return true;
        }

        if (float.IsNaN(releaseStartedAt)) releaseStartedAt = now;
        if (now - releaseStartedAt >= pinchReleaseDuration) pinchArmed = true;
        return false;
    }

    private bool IsPointingAtCalendar()
    {
        if (leftHand == null || !leftHand.IsPointerPoseValid || rayCollider == null || !rayCollider.enabled)
            return false;
        Transform pointer = leftHand.PointerPose;
        return pointer != null && rayCollider.Raycast(new Ray(pointer.position, pointer.forward),
            out _, 10f);
    }
    public void Toggle() => SetVisible(!IsVisible);
    public void Open() => SetVisible(true);
    public void Close() => SetVisible(false);

    private void SelectClose()
    {
        CloseSelected?.Invoke();
        Close();
    }

    public void SetVisible(bool visible)
    {
        bool changed = IsVisible != visible;
        IsVisible = visible;
        if (changed) VisibilityChanged?.Invoke(visible);
        if (!visible)
        {
            if (rayCollider != null) rayCollider.enabled = false;
            if (panelContent != null) panelContent.SetActive(false);
            return;
        }

        PlaceInFrontOfView();
        if (panelContent != null) panelContent.SetActive(true);
        if (rayCollider != null) rayCollider.enabled = true;
        if (calendarPanel != null) calendarPanel.Refresh();
    }

    private void PlaceInFrontOfView()
    {
        Transform viewer = viewTransform;
        if (viewer == null && Camera.main != null) viewer = Camera.main.transform;
        if (viewer == null)
        {
            Debug.LogWarning("GardenCalendarVR needs the CenterEyeAnchor or a Main Camera to appear in front of the player.", this);
            return;
        }

        // Unity world-space UI faces along its local -Z axis.
        transform.SetPositionAndRotation(
          viewer.position + viewer.forward * distanceFromView,
         Quaternion.LookRotation(viewer.forward, viewer.up));

        Canvas worldCanvas = GetComponent<Canvas>();
        if (worldCanvas != null)
            worldCanvas.worldCamera = viewer.GetComponent<Camera>() ?? Camera.main;
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(SelectClose);
    }
}
