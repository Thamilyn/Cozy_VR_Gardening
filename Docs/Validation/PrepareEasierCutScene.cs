if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
    throw new System.InvalidOperationException("Switch validation scenes outside Play.");
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
        throw new System.InvalidOperationException("Preserve unsaved scene changes before switching validation scenes.");
var setup = UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
System.IO.File.WriteAllText("Docs/Validation/EasierStemCutSceneSetup.json", Newtonsoft.Json.JsonConvert.SerializeObject(setup));
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Garden_Moves.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
return new { scene = scene.path, dirty = scene.isDirty };
