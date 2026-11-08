var floor=UnityEngine.GameObject.Find("Floor");
floor.GetComponent("GardenDistantGround").SendMessage("Rebuild");
var cameraObject=new UnityEngine.GameObject("Path review camera") { hideFlags=UnityEngine.HideFlags.HideAndDontSave };
var camera=cameraObject.AddComponent<UnityEngine.Camera>();
camera.transform.position=new UnityEngine.Vector3(10f,17f,-1f);
camera.transform.LookAt(new UnityEngine.Vector3(2f,0f,10f));
camera.fieldOfView=52f;camera.nearClipPlane=0.05f;camera.farClipPlane=180f;
var target=new UnityEngine.RenderTexture(1400,1000,24);
var previous=UnityEngine.RenderTexture.active;
var image=new UnityEngine.Texture2D(1400,1000,UnityEngine.TextureFormat.RGB24,false);
try{camera.targetTexture=target;camera.Render();UnityEngine.RenderTexture.active=target;image.ReadPixels(new UnityEngine.Rect(0,0,1400,1000),0,0);image.Apply();System.IO.File.WriteAllBytes("Docs/Validation/PathReview.png",image.EncodeToPNG());}
finally{UnityEngine.RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);}
return new {capture="Docs/Validation/PathReview.png"};
