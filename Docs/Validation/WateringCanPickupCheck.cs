using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WateringCanPickupCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string message, List<string> checks)
    {
        if (!value) throw new InvalidOperationException(message);
        checks.Add("PASS: " + message);
    }
    static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Private).Invoke(target, args);
    static void Continue(GardenMenuController menu)
    {
        float now = Time.unscaledTime;
        Call(menu, "HandleInputRelease", false, now);
        Call(menu, "HandleInputRelease", true, now);
        Call(menu, "HandleInputRelease", true, now + 0.16f);
        Call(menu, "Continue");
        Call(menu, "HandleInputRelease", true, now);
        Call(menu, "HandleInputRelease", true, now + 0.21f);
    }
    public static string Prepare()
    {
        var checks = new List<string>();
        var can = Object.FindFirstObjectByType<WateringCan>();
        var body = can.GetComponent<Rigidbody>();
        Check(EditorApplication.isPlaying && EditorApplication.isPaused, "Real scene in paused Play", checks);
        Check(Vector3.Dot(can.transform.up, Vector3.up) > 0.999f, "Can starts upright", checks);
        Check(body.constraints == RigidbodyConstraints.FreezeAll, "Initial pose is parked until first pickup", checks);
        Check(can.GetComponentsInChildren<GrabInteractable>(true).All(g => !g.enabled), "Initial menu still suspends controller pickup", checks);
        Continue(Object.FindFirstObjectByType<GardenMenuController>());
        Check(!GardenMenuController.GardenSuspended, "Real menu continue and release flow starts the garden", checks);
        var tutorial = Object.FindFirstObjectByType<GronnyTutorial>();
        if (!(bool)typeof(GronnyTutorial).GetField("initialized", Private).GetValue(tutorial))
        {
            IEnumerator start = (IEnumerator)Call(tutorial, "Start");
            Check(!start.MoveNext(), "Tutorial initializes after starting", checks);
        }
        Call(tutorial, "RefreshInteractionLocks");
        Check(can.GetComponentsInChildren<GrabInteractable>(true).All(g => g.enabled) &&
            can.GetComponentsInChildren<HandGrabInteractable>(true).All(g => g.enabled),
            "Both grab types remain available before planting or covering soil", checks);
        Check(!GronnyTutorial.Allows(GardenTutorialAction.Pour), "Tutorial still blocks pouring before the watering step", checks);
        SimulationMode previous = Physics.simulationMode;
        Vector3 position = can.transform.position;
        Quaternion rotation = can.transform.rotation;
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            body.AddTorque(Vector3.forward * 10f, ForceMode.Impulse);
            for (int i = 0; i < 150; i++) Physics.Simulate(0.02f);
        }
        finally { Physics.simulationMode = previous; }
        Check(Vector3.Distance(position, can.transform.position) < 0.001f &&
            Quaternion.Angle(rotation, can.transform.rotation) < 0.1f,
            "Can remains at its upright starting pose through three seconds of physics and an impulse", checks);
        File.WriteAllLines("Docs/Validation/WateringCanPickupVerification.txt", checks);
        EditorApplication.isPaused = false;
        return string.Join("\n", checks);
    }
    public static string Verify()
    {
        EditorApplication.isPaused = true;
        var checks = new List<string>();
        var can = Object.FindFirstObjectByType<WateringCan>();
        var body = can.GetComponent<Rigidbody>();
        var grabbable = can.GetComponent<Grabbable>();
        var controllers = can.GetComponentsInChildren<GrabInteractable>(true);
        var hands = can.GetComponentsInChildren<HandGrabInteractable>(true);
        var pickup = can.transform.Find("Handle").GetComponent<BoxCollider>();
        Check(pickup != null && pickup.isTrigger, "Upper handle has a non-solid pickup volume", checks);
        Check(controllers.All(g => g.enabled && g.Rigidbody == body && ReferenceEquals(g.PointableElement, grabbable) && g.Colliders.Contains(pickup)),
            "Controller grabs cache the upper-handle volume and target the correct body and Grabbable", checks);
        Check(hands.All(g => g.enabled && g.Rigidbody == body && ReferenceEquals(g.PointableElement, grabbable) && g.Colliders.Contains(pickup)),
            "Hand grabs cache the same upper-handle volume and target the correct body and Grabbable", checks);
        Check(can.GetComponent<BoxCollider>() != null && !can.GetComponent<BoxCollider>().isTrigger &&
            can.GetComponent<MeshCollider>().isTrigger, "Flat body supplies solid contact; imported full hull is pickup only", checks);
        int id = 9400;
        foreach (IPointableElement pointable in new IPointableElement[] { controllers[0].PointableElement, hands[0].PointableElement })
        {
            Pose initial = new Pose(can.transform.position, can.transform.rotation);
            Pose lifted = new Pose(initial.position + Vector3.up * 0.12f, initial.rotation);
            pointable.ProcessPointerEvent(new PointerEvent(id, PointerEventType.Hover, initial));
            pointable.ProcessPointerEvent(new PointerEvent(id, PointerEventType.Select, initial));
            Check(can.IsHeld && body.constraints != RigidbodyConstraints.FreezeAll,
                "Meta selection clears the parked state for pointer " + id, checks);
            pointable.ProcessPointerEvent(new PointerEvent(id, PointerEventType.Move, lifted));
            Check(Vector3.Distance(can.transform.position, lifted.position) < 0.002f,
                "Selected can follows the grab target for pointer " + id, checks);
            pointable.ProcessPointerEvent(new PointerEvent(id, PointerEventType.Unselect, lifted));
            pointable.ProcessPointerEvent(new PointerEvent(id, PointerEventType.Unhover, lifted));
            Check(!can.IsHeld && !body.isKinematic && body.constraints == RigidbodyConstraints.None,
                "Releasing returns the can to normal dynamic physics for pointer " + id, checks);
            id++;
        }
        SimulationMode previous = Physics.simulationMode;
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
            for (int i = 0; i < 200; i++) Physics.Simulate(0.02f);
        }
        finally { Physics.simulationMode = previous; }
        Check(Vector3.Dot(can.transform.up, Vector3.up) > 0.98f, "Can settles upright on the real table after being released", checks);
        var menu = Object.FindFirstObjectByType<GardenMenuController>();
        menu.Open();
        Check(controllers.All(g => !g.enabled) && hands.All(g => !g.enabled), "Pause still suspends both grab types", checks);
        Continue(menu);
        Call(Object.FindFirstObjectByType<GronnyTutorial>(), "RefreshInteractionLocks");
        Check(controllers.All(g => g.enabled) && hands.All(g => g.enabled), "Resuming restores both grab types", checks);
        checks.Add("NOT TESTED: real Quest hand/controller sensors, comfort or device rendering.");
        checks.Add("RESULT: PASS");
        File.AppendAllLines("Docs/Validation/WateringCanPickupVerification.txt", checks);
        return string.Join("\n", checks);
    }
}
