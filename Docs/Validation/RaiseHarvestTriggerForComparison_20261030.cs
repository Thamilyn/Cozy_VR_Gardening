if (!UnityEditor.EditorApplication.isPlaying || GardenMenuController.GardenSuspended)
    throw new System.InvalidOperationException("Run with the garden resumed in Play Mode.");
var tray = UnityEngine.Object.FindFirstObjectByType<TomatoHarvestBasket>();
var volume = (UnityEngine.BoxCollider)new UnityEditor.SerializedObject(tray).FindProperty("collectionVolume").objectReferenceValue;
var before = volume.transform.localPosition;
var position = before;
position.y = 0.112f;
volume.transform.localPosition = position;
UnityEngine.Physics.SyncTransforms();
return new { before = before.ToString("F4"), after = position.ToString("F4"), world = volume.transform.position.ToString("F4"), trigger = volume.isTrigger };
