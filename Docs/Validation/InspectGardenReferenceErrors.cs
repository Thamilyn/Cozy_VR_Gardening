using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class InspectGardenReferenceErrors
{
    public static object Main()
    {
        var stages = new List<object>();
        foreach (var stage in Object.FindObjectsByType<TomatoPotStages>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var settings = new SerializedObject(stage);
            stages.Add(new { path = Path(stage.transform), active = stage.gameObject.activeInHierarchy,
                prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(stage),
                surface = settings.FindProperty("substrateSurface").objectReferenceValue != null,
                soil = settings.FindProperty("soil").objectReferenceValue != null,
                ownPot = stage.GetComponent<GardenPot>() != null,
                parentPot = stage.GetComponentInParent<GardenPot>() != null,
                childPot = stage.GetComponentInChildren<GardenPot>(true) != null,
                nestedStages = stage.GetComponentsInChildren<TomatoPotStages>(true).Length });
        }
        var wrappers = new List<object>();
        foreach (var wrapper in Object.FindObjectsByType<Oculus.Interaction.InteractableUnityEventWrapper>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var settings = new SerializedObject(wrapper);
            if (settings.FindProperty("_interactableView").objectReferenceValue != null) continue;
            wrappers.Add(new { path = Path(wrapper.transform), active = wrapper.gameObject.activeInHierarchy,
                prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(wrapper),
                handGrab = wrapper.GetComponent<Oculus.Interaction.HandGrab.HandGrabInteractable>() != null,
                controllerGrab = wrapper.GetComponent<Oculus.Interaction.GrabInteractable>() != null });
        }
        var pots = new List<object>();
        foreach (var pot in Object.FindObjectsByType<GardenPot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            pots.Add(new { path = Path(pot.transform), id = pot.PotId,
                stages = pot.GetComponent<TomatoPotStages>() != null });
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        return new { scene = scene.path, dirty = scene.isDirty, stages, wrappers, pots };
    }

    private static string Path(Transform target) => target.parent != null ? Path(target.parent) + "/" + target.name : target.name;
}
