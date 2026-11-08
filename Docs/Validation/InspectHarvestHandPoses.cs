var catalogue = UnityEngine.Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial");
var fixture = new UnityEngine.GameObject("Temporary hand pose inspection");
try
{
    var demo = fixture.AddComponent<GardenControlDemonstration>();
    demo.Configure(catalogue);
    demo.SetInputMode(GardenInputMode.Hands);
    demo.Show(catalogue.Find("harvest_cut_stem"));
    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    var poses = (UnityEngine.Quaternion[][])typeof(GardenControlDemonstration).GetField("poses", flags).GetValue(demo);
    var joints = (UnityEngine.Transform[])typeof(GardenControlDemonstration).GetField("joints", flags).GetValue(demo);
    var differences = new System.Collections.Generic.List<object>();
    demo.TickSpoken(6f);
    var skin = ((UnityEngine.GameObject)typeof(GardenControlDemonstration).GetField("model", flags).GetValue(demo)).GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>();
    var openMesh = new UnityEngine.Mesh();
    skin.BakeMesh(openMesh);
    demo.TickSpoken(9.5f);
    var closedMesh = new UnityEngine.Mesh();
    skin.BakeMesh(closedMesh);
    float vertexDelta = 0f;
    var openVertices = openMesh.vertices;
    var closedVertices = closedMesh.vertices;
    for (int v = 0; v < openVertices.Length; v++) vertexDelta = UnityEngine.Mathf.Max(vertexDelta, UnityEngine.Vector3.Distance(openVertices[v], closedVertices[v]));
    UnityEngine.Object.DestroyImmediate(openMesh);
    UnityEngine.Object.DestroyImmediate(closedMesh);
    for (int i = 0; i < joints.Length; i++)
    {
        float angle = UnityEngine.Quaternion.Angle(poses[0][i], poses[2][i]);
        if (angle > 1f) differences.Add(new { joint = joints[i].name, angle });
    }
    return new { differences, vertexDelta, root = ((UnityEngine.Transform)typeof(GardenControlDemonstration).GetField("animationRoot", flags).GetValue(demo)).name,
        clip = catalogue.rightHand.curled.name, length = catalogue.rightHand.curled.length };
}
finally { UnityEngine.Object.DestroyImmediate(fixture); }
