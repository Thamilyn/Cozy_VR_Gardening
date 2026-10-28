using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Only the scene-entry intent crosses from practice into a fresh garden.</summary>
public static class GardenSessionFlow
{
    public const string PracticeScene = "Garden_Practice";
    public const string GardeningScene = "Garden_Moves";
    private static bool arrivingFromPractice;

    public static bool IsPractice => SceneManager.GetActiveScene().name == PracticeScene;
    public static bool IsActivityScene(Scene scene) => scene.name == PracticeScene || scene.name == GardeningScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => arrivingFromPractice = false;

    public static AsyncOperation EnterGarden()
    {
        if (!IsPractice || !Application.CanStreamedLevelBeLoaded(GardeningScene)) return null;
        arrivingFromPractice = true;
        return SceneManager.LoadSceneAsync(GardeningScene, LoadSceneMode.Single);
    }

    public static bool ConsumeGardenArrival()
    {
        if (SceneManager.GetActiveScene().name != GardeningScene || !arrivingFromPractice) return false;
        arrivingFromPractice = false;
        return true;
    }
}
