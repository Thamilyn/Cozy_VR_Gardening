using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using Oculus.Interaction.Locomotion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Pauses garden input without disabling tracking, UI rays or an existing Meta grab.</summary>
public sealed class GardenInteractionSuspension
{
    private readonly Scene scene;
    private readonly HashSet<Behaviour> disabled = new();
    private readonly HashSet<Rigidbody> frozen = new();
    private MonoBehaviour[] behaviours;
    private Grabbable[] props;
    private HandGrabInteractor[] hands;
    private GrabInteractor[] controllers;
    private FirstPersonLocomotor[] locomotors;
    private float nextSearch;

    public GardenInteractionSuspension(Scene scene) { this.scene = scene; }

    public void Tick()
    {
        if (behaviours == null || Time.unscaledTime >= nextSearch)
        {
            behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            props = Object.FindObjectsByType<Grabbable>(FindObjectsSortMode.None);
            hands = Object.FindObjectsByType<HandGrabInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            controllers = Object.FindObjectsByType<GrabInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            locomotors = Object.FindObjectsByType<FirstPersonLocomotor>(FindObjectsSortMode.None);
            nextSearch = Time.unscaledTime + 0.25f;
        }
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour.gameObject.scene != scene || !behaviour.enabled) continue;
            bool block = behaviour is ILocomotionEventBroadcaster ||
                behaviour is TeleportInteractor || behaviour is LocomotionAxisTurnerInteractor ||
                behaviour is LocomotionTurnerInteractor;
            if (behaviour is GrabInteractable || behaviour is HandGrabInteractable)
            {
                Grabbable prop = behaviour.GetComponentInParent<Grabbable>();
                block = prop != null && prop.SelectingPointsCount == 0;
            }
            if (!block) continue;
            disabled.Add(behaviour);
            behaviour.enabled = false;
        }
        // Broadcasters/connections are disconnected, while physical head movement and
        // collision remain available. Remove velocity left over from the previous input.
        foreach (FirstPersonLocomotor locomotor in locomotors)
            if (locomotor != null && locomotor.gameObject.scene == scene) locomotor.Velocity = Vector3.zero;
        foreach (Grabbable prop in props)
        {
            if (prop == null || prop.gameObject.scene != scene || prop.SelectingPointsCount > 0) continue;
            Rigidbody body = prop.GetComponent<Rigidbody>();
            if (body == null || body.isKinematic) continue;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            frozen.Add(body);
            body.isKinematic = true;
        }
    }

    public bool InputsReleased()
    {
        if (hands == null || controllers == null) return false;
        if (OVRInput.Get(OVRInput.RawButton.A | OVRInput.RawButton.B | OVRInput.RawButton.X | OVRInput.RawButton.Y) ||
            OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch) > 0.15f ||
            OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch) > 0.15f ||
            OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch).sqrMagnitude > 0.04f ||
            OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).sqrMagnitude > 0.04f) return false;
        foreach (GrabInteractor controller in controllers)
        {
            if (controller == null || !controller.isActiveAndEnabled) continue;
            ControllerRef source = controller.GetComponentInParent<ControllerRef>();
            if (source == null || HoldingController(source.Handedness)) continue;
            OVRInput.Controller side = source.Handedness == Handedness.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
            if (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, side) > 0.15f) return false;
        }
        OVRInput.Controller activeInput = OVRInput.GetActiveController();
        if ((activeInput & OVRInput.Controller.Hands) == 0)
            return (activeInput & OVRInput.Controller.Touch) != 0;
        bool tracked = false;
        foreach (HandGrabInteractor interactor in hands)
        {
            if (interactor == null || !interactor.isActiveAndEnabled) continue;
            IHand hand = interactor.Hand;
            if (hand == null || !hand.IsTrackedDataValid || !hand.IsHighConfidence) continue;
            tracked = true;
            if (Holding(hand.Handedness)) continue;
            // A straight index exits the locomotion microgesture and prepares a new pinch.
            if (hand.GetFingerPinchStrength(HandFinger.Index) > 0.3f ||
                !hand.GetJointPose(HandJointId.HandIndex1, out Pose basePose) ||
                !hand.GetJointPose(HandJointId.HandIndex2, out Pose middlePose) ||
                !hand.GetJointPose(HandJointId.HandIndexTip, out Pose tipPose) ||
                Vector3.Dot((middlePose.position - basePose.position).normalized,
                    (tipPose.position - middlePose.position).normalized) < 0.8f) return false;
        }
        return tracked;
    }

    private bool Holding(Handedness side)
    {
        foreach (HandGrabInteractor hand in hands)
            if (hand != null && hand.Hand != null && hand.Hand.Handedness == side && hand.HasSelectedInteractable) return true;
        return false;
    }

    private bool HoldingController(Handedness side)
    {
        foreach (GrabInteractor controller in controllers)
        {
            if (controller == null || !controller.HasSelectedInteractable) continue;
            ControllerRef source = controller.GetComponentInParent<ControllerRef>();
            if (source != null && source.Handedness == side) return true;
        }
        return false;
    }

    public void Release()
    {
        foreach (Rigidbody body in frozen)
            if (body != null) body.isKinematic = false;
        frozen.Clear();
        // Restore only components this suspension disabled; tutorial locks keep their owner.
        foreach (Behaviour behaviour in disabled)
            if (behaviour != null) behaviour.enabled = true;
        disabled.Clear();
        behaviours = null;
    }
}
