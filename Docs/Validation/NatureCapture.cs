var cameraObject = new UnityEngine.GameObject("Nature review camera") { hideFlags=UnityEngine.HideFlags.HideAndDontSave };
var camera = cameraObject.AddComponent<UnityEngine.Camera>();
cameraObject.transform.position = new UnityEngine.Vector3(0f,1.3f,-3.6f);
cameraObject.transform.rotation = UnityEngine.Quaternion.Euler(2f,0f,0f);
camera.fieldOfView=75f; camera.nearClipPlane=0.05f; camera.farClipPlane=180f;
var target = new UnityEngine.RenderTexture(1600,900,24);
var previous=UnityEngine.RenderTexture.active;
var image=new UnityEngine.Texture2D(1600,900,UnityEngine.TextureFormat.RGB24,false);
try {camera.targetTexture=target;camera.Render();UnityEngine.RenderTexture.active=target;image.ReadPixels(new UnityEngine.Rect(0,0,1600,900),0,0);image.Apply();System.IO.File.WriteAllBytes("Docs/Validation/NaturePlayerView.png",image.EncodeToPNG());}
finally {UnityEngine.RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);}
return UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>().Where(g=>g.name.StartsWith("Nature - ")).Select(g=>new {name=g.name, triangles=g.GetComponentsInChildren<UnityEngine.MeshFilter>().Where(f=>f.GetComponent<UnityEngine.Renderer>()!=null).Sum(f=>(long)Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(i=>(long)f.sharedMesh.GetIndexCount(i)/3))}).GroupBy(g=>g.name).Select(g=>new {name=g.Key, count=g.Count(),triangles=g.Sum(x=>x.triangles)}).ToArray();
