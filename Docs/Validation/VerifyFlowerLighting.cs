var cycle=UnityEngine.Object.FindFirstObjectByType<GardenPhaseTimeCycle>();
if(cycle==null)throw new System.InvalidOperationException("Garden day/night cycle missing");
var settings=new UnityEditor.SerializedObject(cycle);var sun=(UnityEngine.Light)settings.FindProperty("sun").objectReferenceValue;
var source=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>().First(g=>g.name=="Nature - Flower_06_2");
var specimen=UnityEngine.Object.Instantiate(source);specimen.name="Flower lighting verification";specimen.hideFlags=UnityEngine.HideFlags.HideAndDontSave;specimen.transform.position=new UnityEngine.Vector3(1000,0,1000);specimen.transform.localRotation=UnityEngine.Quaternion.identity;
foreach(var transform in specimen.GetComponentsInChildren<UnityEngine.Transform>(true))transform.gameObject.layer=31;
foreach(var lod in specimen.GetComponentsInChildren<UnityEngine.LODGroup>())lod.enabled=false;
var renderer=specimen.GetComponentInChildren<UnityEngine.Renderer>();
var material=new UnityEngine.Material(renderer.sharedMaterial);material.SetFloat("_Wind_Strength",0);renderer.sharedMaterial=material;
var lit=material.shader;var unlit=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/3D set of stylized nature - GHIBLI style/Art/Materials/Flowers.mat").shader;
var cameraObject=new UnityEngine.GameObject("Flower lighting camera"){hideFlags=UnityEngine.HideFlags.HideAndDontSave};var camera=cameraObject.AddComponent<UnityEngine.Camera>();camera.enabled=false;camera.transform.position=new UnityEngine.Vector3(1000.45f,0.35f,999.45f);camera.transform.LookAt(new UnityEngine.Vector3(1000,0.2f,1000));camera.fieldOfView=40;camera.nearClipPlane=0.01f;camera.farClipPlane=3;camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=UnityEngine.Color.black;camera.cullingMask=1<<31;
var target=new UnityEngine.RenderTexture(512,512,24);camera.targetTexture=target;var texture=new UnityEngine.Texture2D(512,512,UnityEngine.TextureFormat.RGB24,false);
var oldActive=UnityEngine.RenderTexture.active;var oldMode=UnityEngine.RenderSettings.ambientMode;var oldSky=UnityEngine.RenderSettings.ambientSkyColor;var oldEquator=UnityEngine.RenderSettings.ambientEquatorColor;var oldGround=UnityEngine.RenderSettings.ambientGroundColor;var oldSun=UnityEngine.RenderSettings.sun;var oldRotation=sun.transform.rotation;var oldIntensity=sun.intensity;var oldColor=sun.color;
var pixels=new System.Collections.Generic.Dictionary<string,UnityEngine.Color[]>();
try{
 UnityEngine.RenderSettings.sun=sun;
 foreach(var shader in new[]{unlit,lit}){
  material.shader=shader;
  foreach(var name in new[]{"day","night"}){
   var moment=settings.FindProperty(name);var ambient=moment.FindPropertyRelative("ambientColor").colorValue;
   sun.intensity=moment.FindPropertyRelative("sunIntensity").floatValue;sun.color=moment.FindPropertyRelative("sunColor").colorValue;
   sun.transform.rotation=oldRotation*UnityEngine.Quaternion.Euler(moment.FindPropertyRelative("sunElevation").floatValue-settings.FindProperty("day").FindPropertyRelative("sunElevation").floatValue,0,0);
   UnityEngine.RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;UnityEngine.RenderSettings.ambientSkyColor=ambient;UnityEngine.RenderSettings.ambientEquatorColor=ambient*0.65f;UnityEngine.RenderSettings.ambientGroundColor=ambient*0.4f;
   camera.Render();UnityEngine.RenderTexture.active=target;texture.ReadPixels(new UnityEngine.Rect(0,0,512,512),0,0);texture.Apply();var key=(shader==lit?"Lit":"Unlit")+name;
   pixels[key]=texture.GetPixels();System.IO.File.WriteAllBytes("Docs/Validation/Flower"+key+".png",texture.EncodeToPNG());
  }
 }
}finally{
 UnityEngine.RenderTexture.active=oldActive;UnityEngine.RenderSettings.ambientMode=oldMode;UnityEngine.RenderSettings.ambientSkyColor=oldSky;UnityEngine.RenderSettings.ambientEquatorColor=oldEquator;UnityEngine.RenderSettings.ambientGroundColor=oldGround;UnityEngine.RenderSettings.sun=oldSun;sun.transform.rotation=oldRotation;sun.intensity=oldIntensity;sun.color=oldColor;
 camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(specimen);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);
}
System.Func<UnityEngine.Color[],double> brightness=values=>values.Average(c=>(double)(0.2126f*c.r+0.7152f*c.g+0.0722f*c.b));
var litDay=brightness(pixels["Litday"]);var litNight=brightness(pixels["Litnight"]);var unlitDay=brightness(pixels["Unlitday"]);var unlitNight=brightness(pixels["Unlitnight"]);
var report=new{status=litDay>0&&litNight/litDay<0.8?"PASS":"CHECK_LIGHTING",litDay=litDay,litNight=litNight,litNightToDayRatio=litNight/litDay,originalUnlitNightToDayRatio=unlitNight/unlitDay,shader=lit.name,shaderMessages=UnityEditor.ShaderUtil.GetShaderMessages(lit).Select(m=>new{severity=m.severity.ToString(),message=m.message}).ToArray(),note="Editor isolated render using actual cycle day/night sun and ambient settings; scene lighting restored"};
System.IO.File.WriteAllText("Docs/Validation/FlowerLightingVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return report;
