if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
    throw new System.InvalidOperationException("Restore scenes outside Play.");
var setup = Newtonsoft.Json.JsonConvert.DeserializeObject<UnityEditor.SceneManagement.SceneSetup[]>(
    System.IO.File.ReadAllText("Docs/Validation/EasierStemCutSceneSetup.json"));
UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var tools = new System.Collections.Generic.List<object>();
foreach (var tool in UnityEngine.Object.FindObjectsByType<GardenPruner>(UnityEngine.FindObjectsSortMode.None))
{
    var serialized = new UnityEditor.SerializedObject(tool);
    tools.Add(new { name = tool.name, radius = serialized.FindProperty("cuttingRadius").floatValue,
        squeeze = serialized.FindProperty("squeezeToCut").floatValue });
}
return new { scene = scene.path, dirty = scene.isDirty, playing = UnityEditor.EditorApplication.isPlaying, tools };
