var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!="Garden_Moves")throw new System.InvalidOperationException("Expected Garden_Moves");
var component=UnityEngine.GameObject.Find("Floor").GetComponent("GardenDistantGround");
var serialized=new UnityEditor.SerializedObject(component);
serialized.FindProperty("shortGrassMaterial").objectReferenceValue=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Materials/GardenShortGrass.mat");
serialized.FindProperty("grassHeight").floatValue=0.12f;
serialized.FindProperty("grassDensity").floatValue=128f;
serialized.ApplyModifiedProperties();
int hidden=0;
foreach(var renderer in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.MeshRenderer>(true)))
{
    if(renderer.GetComponentInParent<OVRCameraRig>(true)!=null||!renderer.name.StartsWith("Grass",System.StringComparison.OrdinalIgnoreCase))continue;
    UnityEditor.Undo.RecordObject(renderer,"Replace tall grass with short meadow");renderer.enabled=false;
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);hidden++;
}
component.SendMessage("Rebuild");
UnityEditor.EditorUtility.SetDirty(component);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new{hiddenOldGrassRenderers=hidden,scene=scene.path};
