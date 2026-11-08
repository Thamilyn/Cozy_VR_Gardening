var material=new UnityEngine.Material(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Materials/GardenShortGrass.mat"));material.SetVector("_GroundCentre",new UnityEngine.Vector4(1000,0,1000,22));material.SetVector("_GroundShape",new UnityEngine.Vector4(42,20,128,128));
var specimen=new UnityEngine.GameObject("Short grass specimen");
var blade=new UnityEngine.Mesh();
blade.vertices=new[]{new UnityEngine.Vector3(-0.01f,0,0),new UnityEngine.Vector3(0.01f,0,0),new UnityEngine.Vector3(-0.006f,0.07f,0.003f),new UnityEngine.Vector3(0.006f,0.07f,0.003f),new UnityEngine.Vector3(0,0.12f,0.02f)};
blade.normals=Enumerable.Repeat(UnityEngine.Vector3.up,5).ToArray();
blade.uv=new[]{UnityEngine.Vector2.zero,UnityEngine.Vector2.zero,new UnityEngine.Vector2(0.07f,0.58f),new UnityEngine.Vector2(0.07f,0.58f),new UnityEngine.Vector2(0.12f,1f)};
blade.SetUVs(1,Enumerable.Repeat(UnityEngine.Vector3.zero,5).ToList());blade.colors=Enumerable.Repeat(new UnityEngine.Color(0.9f,1f,0.85f,1f),5).ToArray();
blade.triangles=new[]{0,2,1,1,2,3,2,4,3};blade.RecalculateBounds();
specimen.AddComponent<UnityEngine.MeshFilter>().sharedMesh=blade;
specimen.AddComponent<UnityEngine.MeshRenderer>().sharedMaterial=material;
specimen.name="Wind verification specimen";specimen.hideFlags=UnityEngine.HideFlags.HideAndDontSave;
specimen.transform.position=new UnityEngine.Vector3(1000,0,1000);
foreach(var transform in specimen.GetComponentsInChildren<UnityEngine.Transform>(true)) transform.gameObject.layer=31;
foreach(var lod in specimen.GetComponentsInChildren<UnityEngine.LODGroup>()) lod.enabled=false;
var cameraObject=new UnityEngine.GameObject("Wind verification camera"){hideFlags=UnityEngine.HideFlags.HideAndDontSave};
var camera=cameraObject.AddComponent<UnityEngine.Camera>();camera.enabled=false;
camera.transform.position=new UnityEngine.Vector3(1000.22f,0.18f,999.78f);camera.transform.LookAt(new UnityEngine.Vector3(1000,0.06f,1000));
camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=new UnityEngine.Color(0.08f,0.1f,0.12f,1);camera.cullingMask=1<<31;camera.nearClipPlane=0.01f;camera.farClipPlane=3;camera.fieldOfView=40;
var target=new UnityEngine.RenderTexture(384,384,24);camera.targetTexture=target;
var texture=new UnityEngine.Texture2D(384,384,UnityEngine.TextureFormat.RGB24,false);
UnityEngine.Color32[] first=null;double started=UnityEditor.EditorApplication.timeSinceStartup;double firstTime=0;
System.Action<string> capture=path=>{var previous=UnityEngine.RenderTexture.active;try{camera.Render();UnityEngine.RenderTexture.active=target;texture.ReadPixels(new UnityEngine.Rect(0,0,384,384),0,0);texture.Apply();System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());}finally{UnityEngine.RenderTexture.active=previous;}};
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
    UnityEditor.EditorApplication.QueuePlayerLoopUpdate();UnityEditor.SceneView.RepaintAll();
    var elapsed=UnityEditor.EditorApplication.timeSinceStartup-started;
    if(elapsed<0.3)return;
    try{
        if(first==null){capture("Docs/Validation/ShortGrassWindA.png");first=texture.GetPixels32();firstTime=UnityEditor.EditorApplication.timeSinceStartup;return;}
        if(UnityEditor.EditorApplication.timeSinceStartup-firstTime<1.2)return;
        capture("Docs/Validation/ShortGrassWindB.png");var second=texture.GetPixels32();int changed=0;int silhouette=0;var background=first[0];
        for(int i=0;i<first.Length;i++){
            int delta=System.Math.Abs(first[i].r-second[i].r)+System.Math.Abs(first[i].g-second[i].g)+System.Math.Abs(first[i].b-second[i].b);
            if(delta>8)changed++;
            bool a=System.Math.Abs(first[i].r-background.r)+System.Math.Abs(first[i].g-background.g)+System.Math.Abs(first[i].b-background.b)>18;
            bool b=System.Math.Abs(second[i].r-background.r)+System.Math.Abs(second[i].g-background.g)+System.Math.Abs(second[i].b-background.b)>18;
            if(a!=b)silhouette++;
        }
        System.IO.File.WriteAllText("Docs/Validation/ShortGrassWindVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status=changed>10&&silhouette>2?"PASS":"NO_VISIBLE_MOTION",changedPixels=changed,changedSilhouettePixels=silhouette,intervalSeconds=UnityEditor.EditorApplication.timeSinceStartup-firstTime,mode="Editor isolated GPU render; not a headset performance test"},Newtonsoft.Json.Formatting.Indented));
    }catch(System.Exception error){System.IO.File.WriteAllText("Docs/Validation/ShortGrassWindVerification.json",error.ToString());}
    UnityEditor.EditorApplication.update-=tick;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(specimen);UnityEngine.Object.DestroyImmediate(blade);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);
};
UnityEditor.EditorApplication.update+=tick;
return new{status="Capturing two GPU frames",report="Docs/Validation/ShortGrassWindVerification.json"};



