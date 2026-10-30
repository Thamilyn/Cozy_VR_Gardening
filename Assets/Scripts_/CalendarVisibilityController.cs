using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

/// <summary>Keeps the Canvas root listening for Y and left-hand pinches while the panel is hidden.</summary>
[DisallowMultipleComponent]
public sealed class CalendarVisibilityController : MonoBehaviour
{
    [Header("Canvas and panel")]
    [SerializeField] private GameObject panelContent;
    [SerializeField] private Collider rayCollider;
    [SerializeField] private CalendarPanel calendarPanel;
    [SerializeField] private bool startVisible;

    [Header("Placement when opened")]
    [SerializeField, Tooltip("Assign CenterEyeAnchor in the scene. Uses the Main Camera if left empty.")]
    private Transform viewTransform;
    [SerializeField, Min(0.25f)] private float distanceFromView = 1.3f;
    [SerializeField, Tooltip("Table and prop visuals to avoid even without colliders. Uses TableSet if left empty.")]
    private Transform tableContents;

    [Header("Hand gestures")]
    [Tooltip("Left tracked hand. Pinch away from the calendar to toggle it; point at it to use its buttons.")]
    public OVRHand leftHand;
    [SerializeField, Min(0.01f)] private float pinchReleaseDuration = 0.1f;

    private HandGrabInteractor[] handGrabInteractors;
    private RayInteractor[] handRayInteractors;
    private float nextRaySearch;
    private bool pinchArmed;
    private float releaseStartedAt = float.NaN;
    private float nextInteractorSearch;
    private GardenPresentationPlacement placement;

    public bool IsVisible { get; private set; }
    public event System.Action<bool> VisibilityChanged;

    private void Awake()
    {
        // Meta ray selection uses Collider.Raycast directly, so the target can be a trigger.
        // A solid panel can become the locomotor's ground and lift the player as it moves.
        if (rayCollider != null) rayCollider.isTrigger = true;
        // A world-space, pointable Canvas must not inherit a screen-space Canvas scale.
        if (transform.parent != null) transform.SetParent(null, false);
        SetVisible(startVisible);
        if (panelContent != null)
        {
            if (tableContents == null)
            {
                GameObject table = GameObject.Find("TableSet");
                if (table != null) tableContents = table.transform;
            }
            placement = gameObject.AddComponent<GardenPresentationPlacement>();
            placement.Configure(panelContent.transform, viewTransform, rayCollider,
                checkFullSightline: true, visualObstacleRoot: tableContents,
                ignorePlayerAndGrabbables: true);
        }
    }

    private void LateUpdate()
    {
        if (GardenMenuController.GardenSuspended)
        {
            pinchArmed = false;
            releaseStartedAt = float.NaN;
            return;
        }
        // Read grab state after the Meta interactors have processed this frame.
        bool validHand = leftHand != null && leftHand.IsDataValid && leftHand.IsTracked &&
            leftHand.HandConfidence == OVRHand.TrackingConfidence.High &&
            leftHand.GetFingerConfidence(OVRHand.HandFinger.Index) == OVRHand.TrackingConfidence.High &&
            !leftHand.IsSystemGestureInProgress;
        bool busy = IsHandInteractionBusy();
        bool pointingAtUI = IsPointingAtPanel();
        bool pinching = validHand && leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        float strength = validHand ? leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) : 0f;
        // Disarm while using the UI, so moving off a button mid-pinch cannot close the panel.
        bool pinchStarted = UpdatePinchState(validHand && !busy && !pointingAtUI,
            pinching, strength, Time.unscaledTime);

        // UI ray pinches go to the calendar buttons; object grabs consume the gesture.
        if (OVRInput.GetDown(OVRInput.RawButton.Y, OVRInput.Controller.LTouch) ||
            pinchStarted)
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

    private bool IsPointingAtPanel()
    {
        bool calendarTarget = IsVisible && rayCollider != null && rayCollider.enabled;
        if (Time.unscaledTime >= nextRaySearch)
        {
            handRayInteractors = FindObjectsByType<RayInteractor>(FindObjectsSortMode.None);
            nextRaySearch = Time.unscaledTime + 1f;
        }

        bool hasLeftRay = false;
        if (handRayInteractors != null)
        {
            foreach (RayInteractor interactor in handRayInteractors)
            {
                if (interactor == null || !interactor.isActiveAndEnabled ||
                    interactor.State == InteractorState.Disabled) continue;
                HandRef hand = interactor.GetComponentInParent<HandRef>();
                if (hand == null || hand.Hand == null || hand.Handedness != Handedness.Left) continue;
                hasLeftRay = true;
                // Use the same processed ray as PointableCanvas, including Meta's pose adjustments.
                if (GardenMenuController.RayHitsMenu(interactor.Ray, interactor.MaxRayLength) ||
                    (calendarTarget && rayCollider.Raycast(interactor.Ray, out _, interactor.MaxRayLength))) return true;
            }
        }

        if (hasLeftRay || leftHand == null || !leftHand.IsPointerPoseValid) return false;
        Transform pointer = leftHand.PointerPose;
        if (pointer == null) return false;
        Ray pointerRay = new(pointer.position, pointer.forward);
        return GardenMenuController.RayHitsMenu(pointerRay, 10f) ||
            (calendarTarget && rayCollider.Raycast(pointerRay, out _, 10f));
    }
    public void Toggle() => SetVisible(!IsVisible);
    public void Open() => SetVisible(true);
    public void Close() => SetVisible(false);

    public void SetVisible(bool visible)
    {
        if (!GronnyTutorial.Allows(visible ? GardenTutorialAction.OpenCalendar : GardenTutorialAction.CloseCalendar)) return;
        ApplyVisible(visible);
    }

    // Menu suspension must hide this panel even when the tutorial forbids its Close action.
    public void HideForMenu() => ApplyVisible(false);

    private void ApplyVisible(bool visible)
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
        if (placement != null)
        {
            placement.ResetReferencePose();
            placement.ResolveNow();
        }
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

        // Keep the panel upright at eye height instead of copying the headset's pitch and roll.
        Vector3 forward = Vector3.ProjectOnPlane(viewer.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        // Unity world-space UI faces along its local -Z axis.
        transform.SetPositionAndRotation(
            viewer.position + forward * distanceFromView,
            Quaternion.LookRotation(forward, Vector3.up));

        Canvas worldCanvas = GetComponent<Canvas>();
        if (worldCanvas != null)
            worldCanvas.worldCamera = viewer.GetComponent<Camera>() ?? Camera.main;
    }

}
