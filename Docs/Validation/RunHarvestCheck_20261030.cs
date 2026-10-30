if (!UnityEditor.EditorApplication.isPlaying || GardenMenuController.GardenSuspended)
    throw new System.InvalidOperationException("Run with the garden resumed in Play Mode.");
foreach (var tutorial in UnityEngine.Object.FindObjectsByType<GronnyTutorial>(UnityEngine.FindObjectsSortMode.None))
    tutorial.enabled = false;
var type = typeof(TomatoRuntimeVerification);
var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
type.GetField("harvestOnly", flags).SetValue(null, true);
type.GetField("step", flags).SetValue(null, 100);
type.GetField("startedAt", flags).SetValue(null, UnityEditor.EditorApplication.timeSinceStartup);
type.GetField("nextStepAt", flags).SetValue(null, UnityEditor.EditorApplication.timeSinceStartup + 0.1);
type.GetField("result", flags).SetValue(null,
    "Unity " + UnityEngine.Application.unityVersion + System.Environment.NewLine +
    "UTC: " + System.DateTime.UtcNow.ToString("O") + System.Environment.NewLine +
    "SETUP: resumed launch menu with deterministic release samples; tutorial disabled for focused harvest." + System.Environment.NewLine);
UnityEditor.SessionState.SetBool("CozyGarden.TomatoRuntimeCheck", true);
UnityEditor.SessionState.SetBool("CozyGarden.TomatoRuntimeCheck.HarvestOnly", true);
var tick = (UnityEditor.EditorApplication.CallbackFunction)System.Delegate.CreateDelegate(
    typeof(UnityEditor.EditorApplication.CallbackFunction), type.GetMethod("Tick", flags));
UnityEditor.EditorApplication.update -= tick;
UnityEditor.EditorApplication.update += tick;
var tray = UnityEngine.Object.FindFirstObjectByType<TomatoHarvestBasket>();
var volume = (UnityEngine.BoxCollider)new UnityEditor.SerializedObject(tray).FindProperty("collectionVolume").objectReferenceValue;
return new { started = true, frame = UnityEngine.Time.frameCount, volumePosition = volume.transform.position.ToString("F4"), volumeSize = volume.size.ToString("F4") };
