var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var nature=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>().Where(g=>g.name.StartsWith("Nature - ")).SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Renderer>()).ToArray();
var names=new[]{"Cozy Garden/Styled Props","Cozy Garden/Landscape","Cozy Garden/Short Meadow","Cozy Garden/Pastel Sky","Cozy Garden/PastelLeaves","Cozy Garden/PastelLayered","Cozy Garden/PastelProps","Cozy Garden/PastelFlowers"};
var errors=names.Select(UnityEngine.Shader.Find).Where(s=>s!=null).SelectMany(s=>UnityEditor.ShaderUtil.GetShaderMessages(s).Where(m=>m.severity.ToString()=="Error").Select(m=>new{shader=s.name,message=m.message})).ToArray();
var sky=UnityEngine.RenderSettings.skybox;var cycle=UnityEngine.Object.FindFirstObjectByType<GardenPhaseTimeCycle>();var settings=new UnityEditor.SerializedObject(cycle);
var report=new{scene=scene.path,sky=sky.shader.name,skyAssignedToCycle=settings.FindProperty("skybox").objectReferenceValue==sky,dayNightTextures=sky.GetTexture("_Tex")!=null&&sky.GetTexture("_Tex_Blend")!=null,usedNatureShaders=nature.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.shader.name).Distinct().ToArray(),shaderErrors=errors,deletedAssets=0,quest3Performance="Not measured on headset"};
System.IO.File.WriteAllText("Docs/Validation/PastelVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);return report;
