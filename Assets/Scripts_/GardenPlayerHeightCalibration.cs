using UnityEngine;

/// <summary>Sets a comfortable initial eye height once, preserving subsequent tracked head and hand movement.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(OVRCameraRig))]
public sealed class GardenPlayerHeightCalibration : MonoBehaviour
{
    [SerializeField, Range(1f, 1.8f), Tooltip("Initial eye height above the garden floor, measured when the headset is first worn.")]
    private float initialEyeHeight = 1.3f;
    private OVRCameraRig rig;
    private float trackingReadySince = -1f;

    public bool IsCalibrated { get; private set; }
    public float InitialEyeHeight => initialEyeHeight;
    public float AppliedOffset { get; private set; }

    private void Awake() => rig = GetComponent<OVRCameraRig>();

    private void OnEnable() => rig.UpdatedAnchors += OnUpdatedAnchors;

    private void OnDisable()
    {
        rig.UpdatedAnchors -= OnUpdatedAnchors;
        trackingReadySince = -1f;
    }

    private void OnUpdatedAnchors(OVRCameraRig updatedRig)
    {
        if (IsCalibrated) return;
        OVRManager manager = rig.GetComponent<OVRManager>();
        if (manager == null || !OVRManager.isHmdPresent || !manager.isUserPresent ||
            !OVRPlugin.GetNodePositionTracked(OVRPlugin.Node.EyeCenter) ||
            rig.centerEyeAnchor == null || rig.trackingSpace == null ||
            rig.centerEyeAnchor.position.y - rig.transform.position.y < 0.3f)
        {
            trackingReadySince = -1f;
            return;
        }
        // Ignore the initial untracked pose while the headset is being put on.
        if (trackingReadySince < 0f) trackingReadySince = Time.unscaledTime;
        if (Time.unscaledTime - trackingReadySince < 0.5f) return;

        float previousY = rig.trackingSpace.position.y;
        if (!ApplyInitialHeight(rig.transform, rig.trackingSpace, rig.centerEyeAnchor, initialEyeHeight)) return;
        AppliedOffset = rig.trackingSpace.position.y - previousY;
        IsCalibrated = true;
    }

    public static bool ApplyInitialHeight(Transform origin, Transform trackingSpace, Transform eyes, float eyeHeight)
    {
        if (origin == null || trackingSpace == null || eyes == null ||
            trackingSpace == origin || !trackingSpace.IsChildOf(origin) || !eyes.IsChildOf(trackingSpace) ||
            float.IsNaN(eyeHeight) || float.IsInfinity(eyeHeight) || eyeHeight <= 0f) return false;
        float measuredHeight = eyes.position.y - origin.position.y;
        if (float.IsNaN(measuredHeight) || float.IsInfinity(measuredHeight)) return false;
        // Shift the common tracking space so both hands and the head receive the same correction.
        // Leave the locomotion origin on the floor; do not clamp height every frame.
        trackingSpace.position += Vector3.up * (eyeHeight - measuredHeight);
        return true;
    }
}
