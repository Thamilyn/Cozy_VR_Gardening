var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var renderers=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Renderer>(true)).ToArray();
var rockMaterial=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Materials/GardenPastel/PastelStone.mat");
var rocks=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Renderer>().Where(r=>r.sharedMaterials.Contains(rockMaterial)).ToArray();
var plants=renderers.Where(r=>r.transform.parent!=null && r.transform.parent.name=="Plant_Pot").Select(r=>new{name=r.name,shader=r.sharedMaterial.shader.name,material=UnityEditor.AssetDatabase.GetAssetPath(r.sharedMaterial),shaderErrors=UnityEditor.ShaderUtil.ShaderHasError(r.sharedMaterial.shader)}).ToArray();
return new{scene=scene.name,saved=!scene.isDirty,playing=UnityEditor.EditorApplication.isPlaying,rockRenderers=rocks.Length,rockShaderErrors=UnityEditor.ShaderUtil.ShaderHasError(rockMaterial.shader),plants};


