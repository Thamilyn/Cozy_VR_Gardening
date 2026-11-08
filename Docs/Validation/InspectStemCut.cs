var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var fruits = UnityEngine.Object.FindObjectsByType<TomatoFruit>(UnityEngine.FindObjectsSortMode.None);
var pruners = UnityEngine.Object.FindObjectsByType<GardenPruner>(UnityEngine.FindObjectsSortMode.None);
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var fruitData = new System.Collections.Generic.List<object>();
foreach (var fruit in fruits)
{
    var mesh = fruit.GetComponentInChildren<UnityEngine.MeshFilter>();
    fruitData.Add(new { name = fruit.name, ripe = fruit.IsRipe, canCut = fruit.CanCutStem,
        cut = fruit.IsStemCut, picked = fruit.IsPickedUp, position = fruit.transform.position.ToString("F5"),
        stem = fruit.StemPosition.ToString("F5"), mesh = mesh != null ? mesh.name : null });
}
var toolData = new System.Collections.Generic.List<object>();
foreach (var tool in pruners)
{
    var model = (UnityEngine.Transform)typeof(GardenPruner).GetField("model", flags).GetValue(tool);
    toolData.Add(new { name = tool.name, enabled = tool.enabled,
        held = tool.GetComponent<Oculus.Interaction.Grabbable>()?.SelectingPointsCount,
        closure = typeof(GardenPruner).GetField("closure", flags).GetValue(tool),
        armed = typeof(GardenPruner).GetField("cutArmed", flags).GetValue(tool),
        model = model != null ? model.name : null });
}
return new { scene = scene.path, dirty = scene.isDirty, playing = UnityEditor.EditorApplication.isPlaying,
    paused = UnityEditor.EditorApplication.isPaused, suspended = GardenMenuController.GardenSuspended,
    fruits = fruitData, tools = toolData };
