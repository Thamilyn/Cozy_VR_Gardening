using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class GardenReferenceRegression
{
    public static object Main()
    {
        var checks = new List<string>();
        void Check(bool passed, string message)
        {
            if (!passed) throw new InvalidOperationException(message);
            checks.Add("PASS: " + message);
        }
        try
        {
            Check(EditorApplication.isPlaying && EditorApplication.isPaused, "Validation runs in paused Play");
            var stages = Object.FindObjectsByType<TomatoPotStages>(FindObjectsSortMode.None);
            Check(stages.Length == 2, "Only the two configured scene pots retain growth controllers");
            foreach (var stage in stages)
            {
                var settings = new SerializedObject(stage);
                var surface = settings.FindProperty("substrateSurface").objectReferenceValue as MeshFilter;
                Check(surface != null && surface.sharedMesh != null &&
                    settings.FindProperty("soil").objectReferenceValue != null,
                    stage.name + ": real planting surface and soil remain assigned");
                var prefabs = settings.FindProperty("stagePrefabs");
                Check(prefabs.arraySize == 7 && Enumerable.Range(0, prefabs.arraySize)
                    .All(i => prefabs.GetArrayElementAtIndex(i).objectReferenceValue != null),
                    stage.name + ": all seven growth models remain assigned");
                Check(stage.CurrentVisual != null && stage.CurrentVisual.activeInHierarchy,
                    stage.name + ": initial soil-bed visual was created successfully in Awake");
            }
            var plantingPot = Object.FindObjectsByType<GardenPot>(FindObjectsSortMode.None)
                .Single(p => p.PotId == "small-pot-1");
            Check(plantingPot.GetComponent<TomatoPotStages>() != null,
                "The real tomato planting pot retains its growth controller");
            var decorativePot = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                .Single(p => p.name == "PotSmall (2)");
            Check(decorativePot.GetComponent<TomatoPotStages>() == null &&
                decorativePot.GetComponentsInChildren<Renderer>(true).Length > 0 &&
                decorativePot.GetComponent<Collider>().enabled,
                "Decorative pot retains its models and collider without an invalid growth controller");
            var shovel = Object.FindFirstObjectByType<GardenShovel>();
            var wrapper = shovel.GetComponentInChildren<InteractableUnityEventWrapper>(true);
            var handGrab = wrapper.GetComponent<HandGrabInteractable>();
            Check(handGrab != null && new SerializedObject(wrapper).FindProperty("_interactableView")
                .objectReferenceValue == handGrab,
                "Shovel events reference the existing Meta HandGrabInteractable");
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Check((bool)typeof(InteractableUnityEventWrapper).GetField("_started", flags).GetValue(wrapper) &&
                ReferenceEquals(typeof(InteractableUnityEventWrapper).GetField("InteractableView", flags).GetValue(wrapper), handGrab),
                "Meta wrapper completed Awake and Start with a valid interactable view");
            var controllerGrab = shovel.GetComponentInChildren<GrabInteractable>(true);
            var body = shovel.GetComponent<Rigidbody>();
            Check(shovel.GetComponent<Grabbable>() != null && controllerGrab != null && body != null &&
                new SerializedObject(handGrab).FindProperty("_rigidbody").objectReferenceValue == body &&
                new SerializedObject(controllerGrab).FindProperty("_rigidbody").objectReferenceValue == body,
                "Hand and controller grabs retain the shovel body while menu/tutorial locks may disable interaction");
            Check(shovel.didAwake && shovel.didStart, "Shovel completes its gardening initialization");
            return new { passed = checks.Count, checks, physicalQuestInputTested = false };
        }
        catch (Exception error) { checks.Add("FAIL: " + error); throw; }
        finally { File.WriteAllLines("Docs/Validation/GardenReferenceRegression.txt", checks); }
    }
}
