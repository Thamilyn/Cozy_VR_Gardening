using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Finds the current placeholder watering can model and installs its runtime behaviour.
/// This keeps imported model assets untouched and works if the object is loaded a few frames late.
/// </summary>
public sealed class WateringCanSetup : MonoBehaviour
{
    private const string TargetSceneName = "Garden_Moves";
    private const string WateringCanName = "bottle-oil";
    private const float SearchDuration = 10f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForInitialScene()
    {
        if (SceneManager.GetActiveScene().name == TargetSceneName &&
            FindFirstObjectByType<WateringCanSetup>(FindObjectsInactive.Include) == null)
        {
            GameObject setup = new("[Cozy Garden] Watering Can Setup");
            setup.AddComponent<WateringCanSetup>();
        }
    }

    private IEnumerator Start()
    {
        float deadline = Time.unscaledTime + SearchDuration;

        do
        {
            GameObject wateringCan = FindSceneObject(WateringCanName);
            if (wateringCan != null)
            {
                GardenGrabSetup.ConfigureGrabbable(wateringCan);

                if (wateringCan.GetComponent<WateringCan>() == null)
                {
                    wateringCan.AddComponent<WateringCan>();
                }

                yield break;
            }

            yield return null;
        }
        while (Time.unscaledTime < deadline);

        Debug.LogWarning($"Watering can setup could not find a GameObject named '{WateringCanName}'.", this);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == objectName)
                {
                    return candidate.gameObject;
                }
            }
        }

        return null;
    }
}
