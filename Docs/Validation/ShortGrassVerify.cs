var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var chunks=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>().Where(g=>g.name.StartsWith("Short grass chunk ")).ToArray();
var near=chunks.SelectMany(g=>g.GetComponentsInChildren<UnityEngine.MeshFilter>()).Where(f=>f.name=="Dense blades").Select(f=>f.sharedMesh).ToArray();
var far=chunks.SelectMany(g=>g.GetComponentsInChildren<UnityEngine.MeshFilter>()).Where(f=>f.name=="Distant blades").Select(f=>f.sharedMesh).ToArray();
var messages=UnityEditor.ShaderUtil.GetShaderMessages(UnityEngine.Shader.Find("Cozy Garden/Short Meadow")).Select(m=>new{severity=m.severity.ToString(),message=m.message}).ToArray();
var old=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.MeshRenderer>(true)).Where(r=>r.name.StartsWith("Grass")&&r.GetComponentInParent<OVRCameraRig>(true)==null).ToArray();
var floor=UnityEngine.GameObject.Find("Floor").GetComponent<UnityEngine.MeshCollider>();UnityEngine.RaycastHit hit;
var report=new{chunks=chunks.Length,nearTriangles=near.Sum(m=>(long)m.GetIndexCount(0)/3),farTriangles=far.Sum(m=>(long)m.GetIndexCount(0)/3),note="Logical triangles across all chunks before frustum, native LOD and GPU coverage reduction. Only six meshes are stored.",largestChunkVertices=near.Max(m=>m.vertexCount),uniqueNearMeshes=near.Distinct().Count(),uniqueFarMeshes=far.Distinct().Count(),meshMemoryMiB=System.Math.Round(near.Concat(far).Distinct().Sum(m=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(m))/1048576.0,2),activeOldGrass=old.Count(r=>r.enabled),hiddenOldGrass=old.Count(r=>!r.enabled),floorFound=floor.Raycast(new UnityEngine.Ray(new UnityEngine.Vector3(0,1,-2),UnityEngine.Vector3.down),out hit,2),floorY=hit.point.y,shaderMessages=messages,shaderErrors=messages.Count(m=>m.severity=="Error"),quest3Performance="Not measured on headset"};
System.IO.File.WriteAllText("Docs/Validation/ShortGrassVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return report;

