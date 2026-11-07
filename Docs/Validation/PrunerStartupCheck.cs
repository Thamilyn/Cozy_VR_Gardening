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

// Run through Pipeline run_script in a fresh, paused Garden_Moves Play session.
public static class PrunerStartupCheck
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly List<string> checks = new List<string>();

    public static string Main()
    {
        checks.Clear();
        try
        {
            Check(EditorApplication.isPlaying && EditorApplication.isPaused, "Fresh paused Play session");
            var menu = Object.FindFirstObjectByType<GardenMenuController>();
            var pruner = Object.FindFirstObjectByType<GardenPruner>();
            var seed = Object.FindObjectsByType<SeedItem>(FindObjectsSortMode.None)
                .First(item => item.Crop == SeedCrop.Tomato && !item.IsPlanted);
            Check(menu != null && pruner != null && !GardenMenuController.HasStarted &&
                GardenMenuController.GardenSuspended, "Initial menu remains open before starting");
            var suspension = Field<GardenInteractionSuspension>(menu, "suspension");
            suspension.Tick();
            Check(GrabsEnabled(pruner), "Controller and hand grabs are enabled from the initial menu");
            Check(!pruner.GetComponent<Rigidbody>().isKinematic, "Initial menu does not freeze unheld shears");
            Check(!seed.GetComponentsInChildren<GrabInteractable>(true).Any(grab => grab.enabled),
                "Initial menu still blocks seed interaction");

            var grab = pruner.GetComponent<Grabbable>();
            Pose original = new Pose(pruner.transform.position, pruner.transform.rotation);
            grab.ProcessPointerEvent(new PointerEvent(9921, PointerEventType.Hover, original));
            grab.ProcessPointerEvent(new PointerEvent(9921, PointerEventType.Select, original));
            suspension.Tick();
            Pose moved = new Pose(original.position + Vector3.up * 0.1f, original.rotation);
            grab.ProcessPointerEvent(new PointerEvent(9921, PointerEventType.Move, moved));
            Check(grab.SelectingPointsCount == 1 && Vector3.Distance(pruner.transform.position, moved.position) < 0.001f,
                "Shears accept Meta Select and Move while the initial menu is open");
            grab.ProcessPointerEvent(new PointerEvent(9921, PointerEventType.Unselect, moved));
            grab.ProcessPointerEvent(new PointerEvent(9921, PointerEventType.Unhover, moved));
            suspension.Tick();
            Check(GrabsEnabled(pruner) && !pruner.GetComponent<Rigidbody>().isKinematic,
                "Releasing during the initial menu leaves shears available for another grab");

            Continue(menu);
            Check(GardenMenuController.HasStarted && !GardenMenuController.GardenSuspended,
                "Normal Play and release flow still starts the garden");
            var tutorial = Object.FindFirstObjectByType<GronnyTutorial>();
            Check(!Field<bool>(tutorial, "initialized"), "Tutorial waits for normal start");
            var startup = (IEnumerator)Call(tutorial, "Start");
            Check(!startup.MoveNext() && Field<bool>(tutorial, "initialized"), "Real tutorial initializes after starting");
            Check(GrabsEnabled(pruner), "Shears remain grabbable during the first walking lesson");
            Check(!seed.GetComponentsInChildren<GrabInteractable>(true).Any(item => item.enabled),
                "Walking lesson retains the existing seed lock");
            Field<HashSet<string>>(tutorial, "learned").Add("move");
            Call(tutorial, "RefreshInteractionLocks");
            Check(GrabsEnabled(pruner) && seed.GetComponentsInChildren<GrabInteractable>(true).Any(item => item.enabled),
                "Shears remain available when seed grabbing unlocks");

            menu.Open();
            Check(!GrabsEnabled(pruner), "Later pause menus still suspend unheld shears");
            Continue(menu);
            Check(GrabsEnabled(pruner), "Continuing from a later pause restores both grab types");
            checks.Add("NOT TESTED: physical Quest 3 hands/controllers or device comfort.");
            checks.Add("RESULT: PASS");
            return string.Join(Environment.NewLine, checks);
        }
        catch (Exception exception)
        {
            checks.Add("RESULT: FAIL\n" + exception);
            throw;
        }
        finally
        {
            File.WriteAllLines("Docs/Validation/PrunerStartupVerification.txt",
                new[] { "Unity " + Application.unityVersion, "UTC " + DateTime.UtcNow.ToString("O"),
                    "Real menu, tutorial and Meta pointer events; Play paused; no scene saves." }.Concat(checks));
        }
    }

    private static bool GrabsEnabled(GardenPruner pruner) =>
        pruner.GetComponentsInChildren<GrabInteractable>(true).All(grab => grab.enabled) &&
        pruner.GetComponentsInChildren<HandGrabInteractable>(true).All(grab => grab.enabled);

    private static void Continue(GardenMenuController menu)
    {
        float now = Time.unscaledTime;
        Call(menu, "HandleInputRelease", false, now);
        Call(menu, "HandleInputRelease", true, now);
        Call(menu, "HandleInputRelease", true, now + 0.16f);
        Call(menu, "Continue");
        Call(menu, "HandleInputRelease", true, now);
        Call(menu, "HandleInputRelease", true, now + 0.21f);
    }

    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks.Add("PASS: " + message);
    }
}
