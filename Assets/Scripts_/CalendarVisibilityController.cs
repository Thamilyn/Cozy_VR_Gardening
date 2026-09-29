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


    public bool IsVisible { get; private set; }

    private void Awake()
    {
        // A world-space, pointable Canvas must not inherit a screen-space Canvas scale.
        if (transform.parent != null) transform.SetParent(null, false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        SetVisible(startVisible);
    }

    private void Update()
    {
       
        // RawButton.Y is specifically the physical Y button on the left Meta Quest controller.
        // PointableCanvas handles hand-ray pinches on the UI. Do not also toggle the panel
        // when the user is trying to click Advance phase or Close.
            // (pinchStarted && (!IsVisible || !IsPointingAtCalendar())))
        if (OVRInput.GetDown(OVRInput.RawButton.Y, OVRInput.Controller.LTouch) || leftHand.IsReleased())
            Toggle();
    }


   


    public void Toggle() => SetVisible(!IsVisible);
    public void Open() => SetVisible(true);
    public void Close() => SetVisible(false);

    public void SetVisible(bool visible)
    {
        IsVisible = visible;
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
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }
}
