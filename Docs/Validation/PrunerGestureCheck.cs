using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Paused Play fixture. Supplies synthetic IHand joints and a selected hand to the
// SDK release override; does not simulate sensor tracking or the SDK candidate search.
public static class PrunerGestureCheck
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<string> checks = new List<string>();

    public static string Main()
    {
        checks.Clear();
        GameObject handObject = null;
        GameObject obstacle = null;
        PrunerHandGrab gestures = null;
        Grabbable grabbable = null;
        HandGrabInteractor interactor = null;
        try
        {
            Check(EditorApplication.isPlaying && EditorApplication.isPaused, "Fresh paused Play session");
            GardenPruner pruner = Object.FindFirstObjectByType<GardenPruner>();
            gestures = pruner.GetComponent<PrunerHandGrab>();
            Check(gestures != null && gestures.enabled, "Prefab installs the gesture component");
            Check(Field<float>(pruner, "closure") == 0f, "Shears start open on the table");
            var bottom = Field<Transform>(pruner, "bottomPart");
            Quaternion open = bottom.localRotation;
            grabbable = pruner.GetComponent<Grabbable>();
            var grab = pruner.GetComponentInChildren<HandGrabInteractable>();
            Check(grab.PalmGrabRules[HandFinger.Index] == Oculus.Interaction.GrabAPI.FingerRequirement.Required,
                "Pickup uses a palm grasp including the index finger");

            Collider table = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
                .First(collider => collider.name == "TableSet" && collider.enabled && !collider.isTrigger);
            Vector3 tablePoint = new Vector3(table.bounds.center.x, table.bounds.max.y, table.bounds.center.z);
            var hand = new SampleHand { Position = tablePoint + Vector3.up * 0.18f };
            handObject = new GameObject("Pruner synthetic hand interactor");
            interactor = handObject.AddComponent<HandGrabInteractor>();
            interactor.InjectHand(hand);
            FindField(interactor, "_selectedInteractable").SetValue(interactor, grab);
            Call(gestures, "OnHandSelected", interactor);
            var release = (Func<bool>)FindField(interactor, "_computeShouldUnselectOverride").GetValue(interactor);
            Check(release != null, "Selection installs the SDK release predicate");
            Pose pose = new Pose(pruner.transform.position, pruner.transform.rotation);
            grabbable.ProcessPointerEvent(new PointerEvent(interactor.Identifier, PointerEventType.Hover, pose));
            grabbable.ProcessPointerEvent(new PointerEvent(interactor.Identifier, PointerEventType.Select, pose));
            Check(grabbable.SelectingPointsCount == 1, "Meta pointer selection holds the shears");

            hand.SetClosed(true);
            Check(gestures.TryGetSqueeze(out float squeeze, out int id) && squeeze > 0.99f,
                "Closing all four fingers produces a full squeeze");
            Call(pruner, "UpdateBlades", true, squeeze, id, 1f);
            Check(Quaternion.Angle(open, bottom.localRotation) > 21.9f,
                "A fist closes the blades, including in the initial menu");
            Check(!release(), "Closed fist pointing down does not release");

            hand.SetClosed(false);
            hand.Closed[0] = true;
            Check(gestures.TryGetSqueeze(out squeeze, out id) && squeeze < 0.3f,
                "Bending only the index finger cannot fully close the shears");
            Check(!release(), "A partially open hand does not release");
            hand.SetClosed(false);
            hand.PalmDirection = Vector3.up;
            Check(gestures.TryGetSqueeze(out squeeze, out id) && squeeze < 0.01f,
                "Opening the hand produces an open-blade squeeze");
            Call(pruner, "UpdateBlades", true, squeeze, id, 1f);
            Check(Quaternion.Angle(open, bottom.localRotation) < 0.01f && grabbable.SelectingPointsCount == 1,
                "Opening the hand opens the blades while retaining the grasp");
            Check(!release(), "An open palm facing up retains the shears");
            hand.PalmDirection = Vector3.right;
            Check(!release(), "An open palm facing sideways retains the shears");
            hand.PalmDirection = Vector3.down;
            Physics.SyncTransforms();
            Check(!release(), "An open palm over the real table starts a release dwell");
            Check(Field<float>(gestures, "releaseStartedAt") >= 0f, "The scene's TableSet collider is recognized");
            Set(gestures, "releaseStartedAt", Time.unscaledTime - 0.2f);
            Check(release(), "A stable open palm down over the table permits release");
            hand.SetClosed(true);
            Check(!release() && Field<float>(gestures, "releaseStartedAt") < 0f,
                "Closing the hand cancels a pending release");
            hand.SetClosed(false);
            hand.Position = tablePoint + Vector3.up * 0.8f;
            Check(!release(), "An open palm too far above the table retains the tool");
            hand.Position = tablePoint + Vector3.right * 10f + Vector3.up * 0.18f;
            Check(!release(), "An open palm down away from the table does not release");
            hand.Position = tablePoint + Vector3.up * 0.18f;
            obstacle = new GameObject("Pot blocking the tabletop");
            obstacle.transform.position = tablePoint + Vector3.up * 0.08f;
            obstacle.AddComponent<BoxCollider>().size = Vector3.one * 0.08f;
            Physics.SyncTransforms();
            Check(!release(), "A solid prop between the hand and tabletop blocks release");
            Object.DestroyImmediate(obstacle);
            obstacle = null;
            Physics.SyncTransforms();
            hand.Valid = false;
            Check(!release() && !gestures.TryGetSqueeze(out _, out _), "Tracking loss does not count as a release gesture");
            hand.Valid = true;
            hand.Side = Handedness.Left;
            Check(!release(), "Left palm down starts the same release dwell");
            Set(gestures, "releaseStartedAt", Time.unscaledTime - 0.2f);
            Check(release(), "SDK left-hand palm orientation also permits tabletop release");

            grabbable.ProcessPointerEvent(new PointerEvent(interactor.Identifier, PointerEventType.Unselect, pose));
            grabbable.ProcessPointerEvent(new PointerEvent(interactor.Identifier, PointerEventType.Unhover, pose));
            Call(gestures, "OnHandRemoved", interactor);
            Check(FindField(interactor, "_computeShouldUnselectOverride").GetValue(interactor) == null,
                "Ending the grasp clears its SDK release override");
            Check(!gestures.TryGetSqueeze(out _, out _), "No gesture samples remain attached to a released hand");
            Call(pruner, "UpdateBlades", false, 0f, -1, 1f);
            Check(Quaternion.Angle(open, bottom.localRotation) < 0.01f,
                "Released shears return to the open resting pose");
            Call(gestures, "OnHandSelected", interactor);
            gestures.enabled = false;
            Check(FindField(interactor, "_computeShouldUnselectOverride").GetValue(interactor) == null,
                "Disabling the component clears its override");
            gestures.enabled = true;
            checks.Add("NOT TESTED: physical Quest 3 tracking, hand comfort and normal SDK candidate acquisition.");
            checks.Add("RESULT: PASS");
            return string.Join(Environment.NewLine, checks);
        }
        catch (Exception exception) { checks.Add("RESULT: FAIL\n" + exception); throw; }
        finally
        {
            if (gestures != null && interactor != null) Call(gestures, "OnHandRemoved", interactor);
            if (interactor != null) FindField(interactor, "_selectedInteractable").SetValue(interactor, null);
            if (obstacle != null) Object.DestroyImmediate(obstacle);
            if (handObject != null) Object.DestroyImmediate(handObject);
            File.WriteAllLines("Docs/Validation/PrunerGestureVerification.txt",
                new[] { "Unity " + Application.unityVersion, "UTC " + DateTime.UtcNow.ToString("O"),
                    "Synthetic IHand samples, SDK release predicate and real scene tabletop; no scene saves." }.Concat(checks));
        }
    }

    private static FieldInfo FindField(object target, string name)
    {
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo field = type.GetField(name, Private);
            if (field != null) return field;
        }
        throw new MissingFieldException(name);
    }
    private static T Field<T>(object target, string name) => (T)FindField(target, name).GetValue(target);
    private static void Set(object target, string name, object value) => FindField(target, name).SetValue(target, value);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks.Add("PASS: " + message);
    }

    private sealed class SampleHand : IHand
    {
        public readonly bool[] Closed = new bool[4];
        public Vector3 Position;
        public Vector3 PalmDirection = Vector3.down;
        public Handedness Side = Handedness.Right;
        public bool Valid = true;
        public Handedness Handedness => Side;
        public bool IsConnected => Valid;
        public bool IsHighConfidence => Valid;
        public bool IsTrackedDataValid => Valid;
        public bool IsDominantHand => false;
        public bool IsPointerPoseValid => Valid;
        public float Scale => 1f;
        public int CurrentDataVersion => 1;
        public event Action WhenHandUpdated { add { } remove { } }
        public void SetClosed(bool closed) { for (int i = 0; i < Closed.Length; i++) Closed[i] = closed; }
        public bool GetRootPose(out Pose pose)
        {
            pose = new Pose(Position, Quaternion.FromToRotation(Side == Handedness.Left ? Constants.LeftPalmar : Constants.RightPalmar, PalmDirection));
            return Valid;
        }
        public bool GetJointPose(HandJointId joint, out Pose pose)
        {
            GetRootPose(out Pose root);
            int[] bases = { (int)HandJointId.HandIndex1, (int)HandJointId.HandMiddle1, (int)HandJointId.HandRing1, (int)HandJointId.HandPinky1 };
            int[] tips = { (int)HandJointId.HandIndexTip, (int)HandJointId.HandMiddleTip, (int)HandJointId.HandRingTip, (int)HandJointId.HandPinkyTip };
            for (int i = 0; i < bases.Length; i++)
            {
                Vector3 point = new Vector3((i - 1.5f) * 0.015f, 0f, 0.04f);
                if ((int)joint == bases[i] + 1) point += Vector3.forward * 0.025f;
                else if ((int)joint == tips[i]) point += Vector3.forward * 0.025f + (Closed[i] ? Vector3.down : Vector3.forward) * 0.035f;
                else if ((int)joint != bases[i]) continue;
                pose = new Pose(root.position + root.rotation * point, root.rotation);
                return Valid;
            }
            pose = root;
            return false;
        }
        public bool GetFingerIsHighConfidence(HandFinger finger) => Valid;
        public bool GetFingerIsPinching(HandFinger finger) => false;
        public bool GetIndexFingerIsPinching() => false;
        public float GetFingerPinchStrength(HandFinger finger) => 0f;
        public bool GetPointerPose(out Pose pose) => GetRootPose(out pose);
        public bool GetJointPoseLocal(HandJointId joint, out Pose pose) => GetJointPose(joint, out pose);
        public bool GetJointPoseFromWrist(HandJointId joint, out Pose pose) => GetJointPose(joint, out pose);
        public bool GetPalmPoseLocal(out Pose pose) { pose = Pose.identity; return Valid; }
        public bool GetJointPosesLocal(out ReadOnlyHandJointPoses poses) { poses = default; return false; }
        public bool GetJointPosesFromWrist(out ReadOnlyHandJointPoses poses) { poses = default; return false; }
    }
}
