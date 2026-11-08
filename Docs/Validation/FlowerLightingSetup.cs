var shader=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>("Assets/Materials/GardenFlowerLit.shadergraph");
if(shader==null||!shader.isSupported)throw new System.InvalidOperationException("Lit flower shader unavailable");
var errors=UnityEditor.ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
if(errors.Length>0)throw new System.InvalidOperationException(string.Join("\n",errors));
var component=UnityEngine.GameObject.Find("Floor").GetComponent("GardenDistantGround");
var serialized=new UnityEditor.SerializedObject(component);serialized.FindProperty("flowerLightingShader").objectReferenceValue=shader;serialized.ApplyModifiedProperties();
component.SendMessage("Rebuild");UnityEditor.EditorUtility.SetDirty(component);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(component.gameObject.scene);
return new{shader=shader.name,flowers=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>().Where(g=>g.name.StartsWith("Nature - Flower")).SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Renderer>()).SelectMany(r=>r.sharedMaterials).Select(m=>m.shader.name).Distinct().ToArray()};
