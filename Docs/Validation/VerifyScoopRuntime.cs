// Unity Pipeline eval_file fixture: synthetic Meta events on the real scene props.
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var checks = new System.Collections.Generic.List<string>();
void Check(bool valid, string message) { if (!valid) throw new System.Exception(message); checks.Add("PASS: " + message); }
object Field(object target, string name) => target.GetType().GetField(name, flags).GetValue(target);
void Tick(GardenShovel shovel) => typeof(GardenShovel).GetMethod("Update", flags).Invoke(shovel, null);
void SavePreview(UnityEngine.GameObject root, string path, UnityEngine.Vector3 cameraPosition,
    UnityEngine.Vector3 target, float size) {
    var preview = new UnityEditor.PreviewRenderUtility();
    try {
        preview.BeginStaticPreview(new UnityEngine.Rect(0f, 0f, 640f, 640f));
        preview.camera.orthographic = true;
        preview.camera.orthographicSize = size;
        preview.camera.nearClipPlane = 0.01f; preview.camera.farClipPlane = 2f;
        preview.camera.backgroundColor = new UnityEngine.Color(0.22f, 0.25f, 0.25f);
        preview.camera.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
        preview.camera.transform.position = cameraPosition;
        preview.camera.transform.LookAt(target);
        preview.lights[0].intensity = 1.3f;
        preview.lights[0].transform.rotation = UnityEngine.Quaternion.Euler(45f, 30f, 0f);
        preview.lights[1].intensity = 0.7f;
        foreach (var filter in root.GetComponentsInChildren<UnityEngine.MeshFilter>()) {
            var renderer = filter.GetComponent<UnityEngine.MeshRenderer>();
            if (renderer == null || !renderer.enabled) continue;
            var materials = renderer.sharedMaterials;
            var properties = new UnityEngine.MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            var matrix = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int i = 0; i < filter.sharedMesh.subMeshCount && i < materials.Length; i++)
                preview.DrawMesh(filter.sharedMesh, matrix, materials[i], i, properties);
        }
        preview.Render(true, false);
        var texture = preview.EndStaticPreview();
        System.IO.File.WriteAllBytes(path, UnityEngine.ImageConversion.EncodeToPNG(texture));
        UnityEngine.Object.DestroyImmediate(texture);
    } finally { preview.Cleanup(); }
}
var guide = UnityEngine.Object.FindFirstObjectByType<GronnyTutorial>();
Check(guide != null && (bool)Field(guide, "initialized"), "Tutorial initialized after menu selection");
var learned = (System.Collections.Generic.HashSet<string>)Field(guide, "learned");
learned.Add("move"); learned.Add("grab");
var seeds = UnityEngine.Object.FindFirstObjectByType<SeedsController>();
var seed = System.Array.Find(UnityEngine.Object.FindObjectsByType<SeedItem>(UnityEngine.FindObjectsSortMode.None), s => s.Crop == SeedCrop.Tomato && !s.IsPlanted);
var pot = System.Array.Find(UnityEngine.Object.FindObjectsByType<GardenPot>(UnityEngine.FindObjectsSortMode.None), p => p.AcceptsTomato && p.GetComponent<TomatoPotStages>() != null);
var potStages = pot.GetComponent<TomatoPotStages>();
Check((bool)Field(potStages, "soilFilled"), "Pot starts with a visible soil bed before planting");
SavePreview(pot.gameObject, "Docs/Validation/PotSoilBed.png", new UnityEngine.Vector3(0.35f, 0.48f, 0.5f),
    new UnityEngine.Vector3(0f, 0.13f, 0f), 0.25f);
seed.transform.position = pot.PlantingPoint;
Check(seeds.TryPlant(seed), "Real tomato seed planted for the soil step");
Check((bool)Field(potStages, "soilFilled"), "Planting preserves the visible soil bed while the seed needs covering");
var state = seeds.GetState(seeds.PrototypeTomato);
seeds.ReceiveWater(pot, 1f);
Check(!state.soilCovered && state.waterMillilitres == 0f, "Uncovered seed rejects watering before the scoop");
var shovel = UnityEngine.Object.FindFirstObjectByType<GardenShovel>();
Check(shovel.enabled && shovel.didAwake && shovel.didStart && Field(shovel, "body") != null,
    "Real shovel completes Awake and Start without the Meta initialization exception");
Check(((GardenSoilSource[])Field(shovel, "sources")).Length > 0, "Shovel has initialized soil sources");
var source = System.Array.Find(UnityEngine.Object.FindObjectsByType<GardenSoilSource>(UnityEngine.FindObjectsSortMode.None), s => s.transform.parent == null);
var bounds = (UnityEngine.Bounds)Field(source, "surfaceBounds");
var top = source.transform.TransformPoint(new UnityEngine.Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
var localBlade = (UnityEngine.Vector3)Field(shovel, "localScoopPoint");
var body = shovel.GetComponent<UnityEngine.Rigidbody>();
var grab = shovel.GetComponent<Oculus.Interaction.Grabbable>();
const int pointer = 9936;
UnityEngine.Pose PoseFor(UnityEngine.Vector3 scoop, UnityEngine.Quaternion rotation) =>
    new UnityEngine.Pose(scoop - rotation * UnityEngine.Vector3.Scale(localBlade, shovel.transform.lossyScale), rotation);
void PlaceUnheld(UnityEngine.Vector3 scoop) {
    var pose = PoseFor(scoop, UnityEngine.Quaternion.identity);
    shovel.transform.SetPositionAndRotation(pose.position, pose.rotation);
    body.position = pose.position; body.rotation = pose.rotation;
}
var reachable = top + UnityEngine.Vector3.up * 0.12f;
PlaceUnheld(reachable); Tick(shovel);
PlaceUnheld(reachable + UnityEngine.Vector3.right * 0.03f); Tick(shovel);
Check(!shovel.HasSoil, "Unheld shovel does not collect soil");
PlaceUnheld(top + UnityEngine.Vector3.up * 0.3f);
var initialPose = new UnityEngine.Pose(shovel.transform.position, shovel.transform.rotation);
grab.ProcessPointerEvent(new Oculus.Interaction.PointerEvent(pointer, Oculus.Interaction.PointerEventType.Hover, initialPose));
grab.ProcessPointerEvent(new Oculus.Interaction.PointerEvent(pointer, Oculus.Interaction.PointerEventType.Select, initialPose));
Check(grab.SelectingPointsCount == 1, "Meta Select event holds the actual shovel");
Tick(shovel);
var highPose = PoseFor(top + UnityEngine.Vector3.up * 0.3f + UnityEngine.Vector3.right * 0.03f, UnityEngine.Quaternion.identity);
grab.ProcessPointerEvent(new Oculus.Interaction.PointerEvent(pointer, Oculus.Interaction.PointerEventType.Move, highPose)); Tick(shovel);
Check(!shovel.HasSoil, "Gesture far above the container does not load the blade");
var startPose = PoseFor(reachable, UnityEngine.Quaternion.identity);
grab.ProcessPointerEvent(new Oculus.Interaction.PointerEvent(pointer, Oculus.Interaction.PointerEventType.Move, startPose));
typeof(GardenShovel).GetMethod("ResetScoop", flags).Invoke(shovel, null); Tick(shovel);
var strokePose = PoseFor(reachable + UnityEngine.Vector3.right * 0.02f, UnityEngine.Quaternion.identity);
grab.ProcessPointerEvent(new Oculus.Interaction.PointerEvent(pointer, Oculus.Interaction.PointerEventType.Move, strokePose)); Tick(shovel);
Check(shovel.HasSoil, "A gentle 2 cm stroke 12 cm above the soil loads the blade immediately");
var load = (UnityEngine.GameObject)Field(shovel, "soilLoad");
Check(load.transform.parent == shovel.transform && load.GetComponent<UnityEngine.Renderer>().enabled &&
    load.GetComponent<UnityEngine.Renderer>().sharedMaterial == source.SoilMaterial,
    "Loaded soil has the real brown soil material and follows the shovel");
Check(UnityEngine.Vector3.Dot(load.transform.position - shovel.ScoopPoint, shovel.transform.up) > 0f,
    "Soil load sits above the blade face");
var modelFilter = System.Array.Find(shovel.GetComponentsInChildren<UnityEngine.MeshFilter>(), f => f.gameObject != load);
var loadInModel = modelFilter.transform.InverseTransformPoint(load.transform.position);
using (var meshData = UnityEditor.MeshUtility.AcquireReadOnlyMeshData(modelFilter.sharedMesh)) {
    var vertices = new Unity.Collections.NativeArray<UnityEngine.Vector3>(meshData[0].vertexCount, Unity.Collections.Allocator.Temp);
    try {
        meshData[0].GetVertices(vertices);
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        foreach (var vertex in vertices)
            if (UnityEngine.Mathf.Abs(vertex.z - loadInModel.z) < 0.025f) {
                minX = UnityEngine.Mathf.Min(minX, vertex.x);
                maxX = UnityEngine.Mathf.Max(maxX, vertex.x);
            }
        Check(maxX - minX >= load.transform.lossyScale.x && loadInModel.x >= minX && loadInModel.x <= maxX,
            "Soil sits over the broad metal blade, not over the narrow handle");
    } finally { vertices.Dispose(); }
}
SavePreview(shovel.gameObject, "Docs/Validation/ShovelSoilPlacement.png", new UnityEngine.Vector3(0.22f, 0.46f, 0.35f),
    new UnityEngine.Vector3(0f, 0.025f, 0.09f), 0.21f);
shovel.enabled = false;
typeof(GardenShovel).GetField("sources", flags).SetValue(shovel, null);
shovel.enabled = true;
Check(((GardenSoilSource[])Field(shovel, "sources")).Length > 0, "Re-enabling restores source discovery");
var overPot = pot.PlantingPoint + UnityEngine.Vector3.up * 0.1f;
var levelPose = PoseFor(overPot, UnityEngine.Quaternion.identity);
grab.ProcessPointerEvent(new Oculus.Interaction.PointerEvent(pointer, Oculus.Interaction.PointerEventType.Move, levelPose)); Tick(shovel);
Check(!state.soilCovered && shovel.HasSoil, "Level loaded blade over the pot does not complete coverage");
int covered = 0;
seeds.SeedCovered += _ => covered++;
// Leaving after the level pose must not force another level pose to empty the blade.
var awayPose = PoseFor(overPot + UnityEngine.Vector3.up * 0.5f, UnityEngine.Quaternion.Euler(0f, 0f, 60f));
grab.ProcessPointerEvent(new Oculus.Interaction.PointerEvent(pointer, Oculus.Interaction.PointerEventType.Move, awayPose)); Tick(shovel);
Check(!state.soilCovered && shovel.HasSoil, "Tilt away from the pot preserves the load");
var surface = pot.GetComponentInChildren<GardenSoil>(true).GetComponent<UnityEngine.MeshFilter>();
var surfaceBounds = surface.sharedMesh.bounds;
var edgePoint = surface.transform.TransformPoint(new UnityEngine.Vector3(surfaceBounds.max.x, surfaceBounds.max.y, surfaceBounds.center.z));
var overlappingPoint = edgePoint + surface.transform.right * 0.025f + surface.transform.up * 0.1f;
Check(!pot.CanReceiveSoilAt(overlappingPoint) && pot.CanReceiveSoilAt(overlappingPoint, 0.05f),
    "Loaded blade overlaps the pot while its centre is just outside the opening");
var tiltedPose = PoseFor(overlappingPoint, UnityEngine.Quaternion.Euler(0f, 0f, 60f));
grab.ProcessPointerEvent(new Oculus.Interaction.PointerEvent(pointer, Oculus.Interaction.PointerEventType.Move, tiltedPose)); Tick(shovel);
var started = UnityEditor.EditorApplication.timeSinceStartup;
System.IO.File.WriteAllText("Docs/Validation/ScoopRuntimeVerification.txt", string.Join("\n", checks) + "\nWaiting for the real pour hold interval.\n");
UnityEditor.EditorApplication.CallbackFunction finish = null;
finish = () => {
    if (UnityEditor.EditorApplication.timeSinceStartup - started < 1.2) return;
    UnityEditor.EditorApplication.update -= finish;
    try {
        Check(state.soilCovered && !shovel.HasSoil, "Entering already tilted with blade overlap consumes the scoop after the hold interval");
        Check(covered == 1 && !seeds.TryCoverSeed(pot), "Coverage is reported exactly once");
        var seedRenderer = (UnityEngine.Renderer)Field(seed, "seedRenderer");
        Check(!seedRenderer.enabled, "Covered seed is hidden");
        Check(guide.TryGetInstruction(out string instruction, out _) && instruction.Contains("Step 5/7"),
            "Tutorial advances once to watering");
        seeds.ReceiveWater(pot, 0.001f);
        Check(state.waterMillilitres > 0f, "Covered seed accepts the next watering step");
        System.IO.File.WriteAllText("Docs/Validation/ScoopRuntimeVerification.txt", string.Join("\n", checks) +
            "\nRESULT: PASS. Synthetic Meta pointer events in Unity " + UnityEngine.Application.unityVersion +
            ". Physical Quest 3 hands/controllers not tested.\n");
    } catch (System.Exception error) {
        System.IO.File.WriteAllText("Docs/Validation/ScoopRuntimeVerification.txt", string.Join("\n", checks) + "\nRESULT: FAIL\n" + error);
    } finally {
        grab.ProcessPointerEvent(new Oculus.Interaction.PointerEvent(pointer, Oculus.Interaction.PointerEventType.Unselect, tiltedPose));
        grab.ProcessPointerEvent(new Oculus.Interaction.PointerEvent(pointer, Oculus.Interaction.PointerEventType.Unhover, tiltedPose));
    }
};
UnityEditor.EditorApplication.update += finish;
return "Scoop checks passed; awaiting the real pour interval.";
