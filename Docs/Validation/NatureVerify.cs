var objects=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>().Where(g=>g.name.StartsWith("Nature - ")).ToArray();
var renderers=objects.SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Renderer>()).ToArray();
var filters=objects.SelectMany(g=>g.GetComponentsInChildren<UnityEngine.MeshFilter>()).Where(f=>f.GetComponent<UnityEngine.Renderer>()!=null).ToArray();
var shaders=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.shader).Concat(new[]{UnityEngine.Shader.Find("Cozy Garden/Landscape")}).Distinct().ToArray();
var shaderMessages=shaders.SelectMany(s=>UnityEditor.ShaderUtil.GetShaderMessages(s).Select(m=>new {shader=s.name,severity=m.severity.ToString(),message=m.message})).ToArray();
var floor=UnityEngine.GameObject.Find("Floor").GetComponent<UnityEngine.MeshCollider>();
UnityEngine.RaycastHit hit;
var hasFloor=floor.Raycast(new UnityEngine.Ray(new UnityEngine.Vector3(0,1,-2),UnityEngine.Vector3.down),out hit,2f);
return new {prefabInstances=objects.Length, renderers=renderers.Length, prefabTriangles=filters.Sum(f=>Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(i=>(long)f.sharedMesh.GetIndexCount(i)/3)), totalEnvironmentTriangles=3296+filters.Sum(f=>Enumerable.Range(0,f.sharedMesh.subMeshCount).Sum(i=>(long)f.sharedMesh.GetIndexCount(i)/3)), activeDecorativeColliders=objects.SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Collider>()).Count(c=>c.enabled), grassGround=hasFloor, floorHeight=hit.point.y, removedPlatform=UnityEngine.GameObject.Find("Floor (1)")==null, shaderMessages=shaderMessages, sceneDirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty};
