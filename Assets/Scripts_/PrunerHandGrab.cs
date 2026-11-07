using System;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

/// <summary>Keeps the shears in an open hand until the player places them palm-down over a table.</summary>
[DisallowMultipleComponent]
public sealed class PrunerHandGrab : MonoBehaviour
{
    [Tooltip("Optional table hierarchy. Otherwise recognizes the project's Table / Mesa objects.")]
    [SerializeField] private Transform releaseTable;
    [SerializeField, Min(0.01f)] private float maximumTableDistance = 0.45f;
    [SerializeField, Range(0f, 80f)] private float maximumPalmDownAngle = 45f;
    [SerializeField, Min(0f)] private float releaseHoldSeconds = 0.15f;
    [SerializeField, Range(0f, 0.5f)] private float openHandThreshold = 0.2f;

    private static readonly HandJointId[] FingerBases =
        { HandJointId.HandIndex1, HandJointId.HandMiddle1, HandJointId.HandRing1, HandJointId.HandPinky1 };
    private static readonly HandJointId[] FingerMiddles =
        { HandJointId.HandIndex2, HandJointId.HandMiddle2, HandJointId.HandRing2, HandJointId.HandPinky2 };
    private static readonly HandJointId[] FingerTips =
        { HandJointId.HandIndexTip, HandJointId.HandMiddleTip, HandJointId.HandRingTip, HandJointId.HandPinkyTip };

    private readonly RaycastHit[] tableHits = new RaycastHit[32];
    private HandGrabInteractable[] handGrabs;
    private HandGrabInteractor holdingHand;
    private HandGrabInteractable heldGrab;
    private GardenPruner pruner;
    private Rigidbody body;
    private float releaseStartedAt = -1f;

    private void Awake()
    {
        pruner = GetComponent<GardenPruner>();
        body = GetComponent<Rigidbody>();
        handGrabs = GetComponentsInChildren<HandGrabInteractable>(true);
    }

    private void OnEnable()
    {
        foreach (HandGrabInteractable grab in handGrabs)
        {
            grab.WhenSelectingInteractorAdded.Action += OnHandSelected;
            grab.WhenSelectingInteractorRemoved.Action += OnHandRemoved;
            foreach (HandGrabInteractor interactor in grab.SelectingInteractors) OnHandSelected(interactor);
        }
    }

    private void OnHandSelected(HandGrabInteractor interactor)
    {
        ClearHandOverride();
        holdingHand = interactor;
        heldGrab = interactor.SelectedInteractable;
        // Override only this selection. Meta still owns motion, hand switching and
        // cancellation, and clears the override when the selection ends.
        interactor.SetComputeShouldUnselectOverride(ShouldReleaseHand);
    }

    private void OnHandRemoved(HandGrabInteractor interactor)
    {
        if (interactor == holdingHand) ClearHandOverride();
    }

    public bool TryGetSqueeze(out float squeeze, out int interactorId)
    {
        squeeze = 0f;
        interactorId = -1;
        if (holdingHand == null || heldGrab == null || holdingHand.SelectedInteractable != heldGrab ||
            !TryReadHand(holdingHand.Hand, out squeeze, out _, out _)) return false;
        interactorId = holdingHand.Identifier;
        return true;
    }

    private bool ShouldReleaseHand()
    {
        if (!isActiveAndEnabled || pruner == null || !pruner.isActiveAndEnabled) return true;
        if (holdingHand == null || heldGrab == null || holdingHand.SelectedInteractable != heldGrab ||
            !TryReadHand(holdingHand.Hand, out _, out float mostClosedFinger, out Pose wrist) ||
            mostClosedFinger > openHandThreshold)
        {
            releaseStartedAt = -1f;
            return false;
        }

        IHand hand = holdingHand.Hand;
        Vector3 palmDirection = wrist.rotation *
            (hand.Handedness == Handedness.Left ? Constants.LeftPalmar : Constants.RightPalmar);
        Vector3 palmPosition = wrist.position;
        if (hand.GetPalmPoseLocal(out Pose palm)) palmPosition += wrist.rotation * palm.position;
        bool placing = Vector3.Dot(palmDirection, Vector3.down) >=
            Mathf.Cos(maximumPalmDownAngle * Mathf.Deg2Rad) && IsAboveTable(palmPosition, palmDirection);
        if (!placing)
        {
            releaseStartedAt = -1f;
            return false;
        }
        if (releaseStartedAt < 0f) releaseStartedAt = Time.unscaledTime;
        return Time.unscaledTime - releaseStartedAt >= releaseHoldSeconds;
    }

    private bool IsAboveTable(Vector3 palmPosition, Vector3 palmDirection)
    {
        int count = Physics.RaycastNonAlloc(palmPosition, palmDirection, tableHits,
            maximumTableDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (count == tableHits.Length) return false;
        Collider nearest = null;
        Vector3 surfaceNormal = Vector3.zero;
        float distance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = tableHits[i];
            if (hit.rigidbody == body || hit.collider.GetComponentInParent<OVRCameraRig>() != null ||
                hit.distance >= distance) continue;
            distance = hit.distance;
            nearest = hit.collider;
            surfaceNormal = hit.normal;
        }
        if (nearest == null || Vector3.Dot(surfaceNormal, Vector3.up) < 0.7f) return false;
        if (releaseTable != null) return nearest.transform.IsChildOf(releaseTable);
        // The current gardening table has its solid tabletop collider on TableSet,
        // alongside the decorative Table model. Its other props are not tabletops.
        if (nearest.name.Equals("TableSet", StringComparison.OrdinalIgnoreCase)) return true;
        for (Transform surface = nearest.transform; surface != null; surface = surface.parent)
        {
            string surfaceName = surface.name;
            if (surfaceName.Equals("Table", StringComparison.OrdinalIgnoreCase) ||
                surfaceName.StartsWith("Table (", StringComparison.OrdinalIgnoreCase) ||
                surfaceName.StartsWith("Table(Clone)", StringComparison.OrdinalIgnoreCase) ||
                surfaceName.Equals("Mesa", StringComparison.OrdinalIgnoreCase) ||
                surfaceName.StartsWith("Mesa (", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static bool TryReadHand(IHand hand, out float squeeze, out float mostClosedFinger, out Pose wrist)
    {
        squeeze = mostClosedFinger = 0f;
        wrist = Pose.identity;
        if (hand == null || !hand.IsConnected || !hand.IsTrackedDataValid || !hand.IsHighConfidence ||
            !hand.GetRootPose(out wrist)) return false;
        for (int i = 0; i < FingerBases.Length; i++)
        {
            HandFinger finger = (HandFinger)(i + (int)HandFinger.Index);
            if (!hand.GetFingerIsHighConfidence(finger) ||
                !hand.GetJointPose(FingerBases[i], out Pose fingerBase) ||
                !hand.GetJointPose(FingerMiddles[i], out Pose fingerMiddle) ||
                !hand.GetJointPose(FingerTips[i], out Pose fingerTip)) return false;
            Vector3 firstBone = fingerMiddle.position - fingerBase.position;
            Vector3 lastBones = fingerTip.position - fingerMiddle.position;
            if (firstBone.sqrMagnitude < 0.00000001f || lastBones.sqrMagnitude < 0.00000001f) return false;
            float curl = Mathf.InverseLerp(0.95f, 0.25f, Vector3.Dot(firstBone.normalized, lastBones.normalized));
            squeeze += curl / FingerBases.Length;
            mostClosedFinger = Mathf.Max(mostClosedFinger, curl);
        }
        return true;
    }

    private void ClearHandOverride()
    {
        if (holdingHand != null && holdingHand.SelectedInteractable == heldGrab)
            holdingHand.ClearComputeShouldUnselectOverride();
        holdingHand = null;
        heldGrab = null;
        releaseStartedAt = -1f;
    }

    private void OnDisable()
    {
        ClearHandOverride();
        foreach (HandGrabInteractable grab in handGrabs)
        {
            if (grab == null) continue;
            grab.WhenSelectingInteractorAdded.Action -= OnHandSelected;
            grab.WhenSelectingInteractorRemoved.Action -= OnHandRemoved;
        }
    }
}
