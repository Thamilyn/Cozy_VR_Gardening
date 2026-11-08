var catalogue = UnityEngine.Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial");
var fixture = new UnityEngine.GameObject("Temporary pickup controller inspection");
try
{
    var demo = fixture.AddComponent<GardenControlDemonstration>();
    demo.Configure(catalogue, UnityEngine.Object.FindFirstObjectByType<TomatoPrototypeGuide>().GardeningDemonstrationAnchor);
    demo.SetInputMode(GardenInputMode.Controllers);
    demo.Show(catalogue.Find("harvest_stem_cut"));
    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    var model = (UnityEngine.GameObject)typeof(GardenControlDemonstration).GetField("model", flags).GetValue(demo);
    var anchor = (UnityEngine.GameObject)typeof(GardenControlDemonstration).GetField("anchor", flags).GetValue(demo);
    var report = new System.Collections.Generic.List<object>();
    foreach (float time in new[] { 0f, 1.8f, 2.9f })
    {
        demo.TickSpoken(time);
        var renderers = new System.Collections.Generic.List<object>();
        foreach (var renderer in model.GetComponentsInChildren<UnityEngine.Renderer>(true))
        {
            string bakedBounds = null;
            string sourceBounds = null;
            if (renderer is UnityEngine.SkinnedMeshRenderer skin)
            {
                var mesh = new UnityEngine.Mesh();
                skin.BakeMesh(mesh, false);
                bakedBounds = mesh.bounds.ToString("F4");
                sourceBounds = skin.sharedMesh.bounds.ToString("F4");
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            renderers.Add(new { name = renderer.name, enabled = renderer.enabled, active = renderer.gameObject.activeInHierarchy,
                position = anchor.transform.InverseTransformPoint(renderer.bounds.center).ToString("F4"),
                size = renderer.bounds.size.ToString("F4"), scale = renderer.transform.lossyScale.ToString("F4"), bakedBounds, sourceBounds });
        }
        report.Add(new { time, rootScale = model.transform.localScale.ToString("F4"), rootPosition = model.transform.localPosition.ToString("F4"), renderers });
    }
    var bindings = new System.Collections.Generic.List<string>();
    foreach (var binding in UnityEditor.AnimationUtility.GetCurveBindings(catalogue.controllerGrip))
        bindings.Add(binding.path + ":" + binding.propertyName);
    return new { report, bindings };
}
finally { UnityEngine.Object.DestroyImmediate(fixture); }
