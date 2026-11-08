using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SoilAnimationRegression
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<string> Checks = new();

    public static string Main()
    {
        Checks.Clear();
        GameObject fixture = null;
        try
        {
            Check(EditorApplication.isPlaying && EditorApplication.isPaused, "Paused Play fixture");
            var catalogue = Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial");
            var scoop = catalogue.Find("soil_scoop_first");
            var pour = catalogue.Find("soil_cover_seed");
            Check((int)GardenControlDemo.CutStem == 8 && (int)GardenControlDemo.PickTomato == 9 &&
                (int)GardenControlDemo.SoilScoop == 10, "Previous serialized demo values preserved");
            Check(scoop.demo == GardenControlDemo.SoilScoop && pour.demo == GardenControlDemo.SoilPour &&
                catalogue.Find("soil_seed_covered").demo == GardenControlDemo.None, "Only actionable soil cues animate");
            fixture = new GameObject("Temporary soil demonstration regression");
            var demo = fixture.AddComponent<GardenControlDemonstration>();
            var guide = Object.FindFirstObjectByType<TomatoPrototypeGuide>();
            demo.Configure(catalogue, guide != null ? guide.GardeningDemonstrationAnchor : null);
            foreach (GardenInputMode mode in new[] { GardenInputMode.Hands, GardenInputMode.Controllers })
            {
                demo.SetInputMode(mode);
                demo.Show(scoop);
                var anchor = Field<GameObject>(demo, "anchor");
                Check(anchor != null && demo.IsShowing(scoop), mode + ": scoop guide appears");
                Check(anchor.GetComponentsInChildren<Oculus.Interaction.Grabbable>(true).Length == 0 &&
                    anchor.GetComponentsInChildren<GardenShovel>(true).Length == 0 &&
                    anchor.GetComponentsInChildren<GardenSoilSource>(true).Length == 0,
                    mode + ": copied props cannot collect soil or register grabs");
                Check(anchor.GetComponentsInChildren<Collider>(true).All(c => !c.enabled) &&
                    anchor.GetComponentsInChildren<Rigidbody>(true).All(b => b.isKinematic && !b.detectCollisions),
                    mode + ": guide has no physical collisions");
                object soil = Field<object>(demo, "soil");
                Transform examplePot = anchor.GetComponentsInChildren<Transform>(true)
                    .Single(t => t.name == "Example planted pot");
                Check(examplePot.GetComponentsInChildren<MeshRenderer>(true).Any(r => r.enabled &&
                    r.sharedMaterials.Contains(Field<Material>(soil, "soilMaterial"))),
                    mode + ": copied pot ground uses brown soil instead of the red placeholder");
                Check(examplePot.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(r => r.name == "Example PotSmall" || r.name == "Example Ground" || r.name == "Example Plate")
                    .All(r => !r.enabled), mode + ": hidden original pot meshes remain hidden");
                var load = Field<GameObject>(soil, "load");
                Check(!load.activeSelf, mode + ": scoop begins with an empty blade");
                Check(Field<TextMesh>(demo, "caption").text.Contains(mode == GardenInputMode.Hands ? "Pinch" : "Grip"),
                    mode + ": caption uses the matching grab input");
                Capture(anchor, "ScoopEmpty-" + mode);
                demo.TickSpoken(2f);
                Check(load.activeSelf, mode + ": short stroke visibly loads the blade");
                if (mode == GardenInputMode.Hands)
                {
                    Vector3 pinch = (Field<Transform>(demo, "harvestThumbTip").position +
                        Field<Transform>(demo, "harvestIndexTip").position) * 0.5f;
                    Vector3 handle = (Vector3)soil.GetType().GetProperty("GripPoint").GetValue(soil);
                    Check(Vector3.Distance(pinch, handle) < 0.005f, "Hands: Meta pinch is aligned with the shovel handle");
                }
                Vector3 loadedPosition = load.transform.position;
                Capture(anchor, "ScoopLoaded-" + mode);
                demo.TickSpoken(4.2f);
                Check(Vector3.Distance(load.transform.position, loadedPosition) > 0.2f,
                    mode + ": hand and loaded blade carry the soil across to the pot");
                demo.TickSpoken(7.05f);
                Check(Field<Transform[]>(soil, "grains").Any(g => g.gameObject.activeSelf),
                    mode + ": tipping shows falling soil");
                Check(Quaternion.Angle(Field<Transform>(demo, "motion").localRotation, Quaternion.identity) > 55f,
                    mode + ": displayed tilt meets the real shovel threshold");
                Capture(anchor, "ScoopPour-" + mode);
                demo.TickSpoken(8.5f);
                Check(!load.activeSelf && !Field<GameObject>(soil, "seed").activeSelf &&
                    Field<GameObject>(soil, "cover").activeSelf, mode + ": one scoop empties and covers the example seed");
                Capture(anchor, "ScoopCovered-" + mode);
                demo.Show(pour);
                anchor = Field<GameObject>(demo, "anchor");
                soil = Field<object>(demo, "soil");
                Check(Field<GameObject>(soil, "load").activeSelf &&
                    !anchor.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Example soil supply"),
                    mode + ": loaded guide starts over the pot without another soil supply");
                Check(demo.UsesHandTeachingTiming && demo.CycleDurationSeconds >= 12f,
                    mode + ": silent practice holds poses for a readable cycle");
                demo.TickSpoken(7.75f);
                Capture(anchor, "LoadedPour-" + mode);
                demo.TickSpoken(9.1f);
                Check(Field<GameObject>(soil, "cover").activeSelf && !Field<GameObject>(soil, "load").activeSelf,
                    mode + ": loaded guide finishes the transfer");
                demo.SetInputMode(GardenInputMode.Unknown);
                Check(Field<GameObject>(demo, "anchor") == null, mode + ": changing mode removes the previous guide");
            }
            demo.SetInputMode(GardenInputMode.Hands);
            var voice = fixture.AddComponent<GronnyVoicePlayer>();
            // Menu playback avoids binding a test source to the real gardener and
            // allows an isolated fixture without advancing the player's tutorial.
            voice.Configure(catalogue, demo, menuPlayback: true);
            bool needsSoil = true;
            bool loaded = false;
            voice.SetBackgroundDemonstration(scoop.id, () => needsSoil && !loaded);
            Call(voice, "Update");
            Check(demo.IsShowing(scoop), "Scoop animation persists without repeating speech");
            loaded = true;
            voice.SetBackgroundDemonstration(pour.id, () => needsSoil && loaded);
            Call(voice, "Update");
            Check(demo.IsShowing(pour), "Loading the real shovel can replace the scoop guide immediately");
            needsSoil = false;
            Call(voice, "Update");
            Check(Field<GameObject>(demo, "anchor") == null, "Coverage removes the soil guide");
            needsSoil = true;
            Call(voice, "Update");
            typeof(GronnyVoicePlayer).GetField("menuPlayback", Flags).SetValue(voice, false);
            voice.SuspendForMenu();
            Check(Field<GameObject>(demo, "anchor") == null, "Menu suspension hides the guide");
            return string.Join("\n", Checks);
        }
        catch (Exception error) { Checks.Add("FAIL: " + error); throw; }
        finally
        {
            if (fixture != null) Object.DestroyImmediate(fixture);
            File.WriteAllLines("Docs/Validation/SoilAnimationRegression.txt", Checks);
        }
    }

    private static void Capture(GameObject anchor, string name)
    {
        Directory.CreateDirectory("Docs/Validation/SoilAnimations");
        var cameraObject = new GameObject("Temporary animation preview camera");
        var target = new RenderTexture(960, 960, 24);
        var pixels = new Texture2D(960, 960, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        var parts = anchor.GetComponentsInChildren<Transform>(true);
        var layers = parts.Select(part => part.gameObject.layer).ToArray();
        var bakedHands = new List<(SkinnedMeshRenderer skin, MeshFilter filter, MeshRenderer renderer, Mesh mesh)>();
        try
        {
            foreach (var part in parts) part.gameObject.layer = 31;
            // Camera.Render alone does not refresh GPU skinning in paused Play.
            // Bake the posed hands for faithful stills; runtime keeps Meta skinning.
            foreach (var skin in anchor.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                skin.BakeMesh(mesh);
                Vector3 skinScale = skin.transform.lossyScale;
                Vector3[] vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                    vertices[i] = new Vector3(vertices[i].x / skinScale.x, vertices[i].y / skinScale.y, vertices[i].z / skinScale.z);
                mesh.vertices = vertices;
                mesh.RecalculateBounds();
                var filter = skin.gameObject.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var renderer = skin.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = skin.sharedMaterials;
                skin.enabled = false;
                bakedHands.Add((skin, filter, renderer, mesh));
            }
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 0.43f;
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.07f, 0.05f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 4f;
            camera.transform.position = anchor.transform.position - anchor.transform.forward * 1.5f + Vector3.up * 0.55f;
            camera.transform.LookAt(anchor.transform.position, Vector3.up);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 960, 960), 0, 0);
            pixels.Apply();
            File.WriteAllBytes("Docs/Validation/SoilAnimations/" + name + ".png", pixels.EncodeToPNG());
        }
        finally
        {
            foreach (var item in bakedHands)
            {
                Object.DestroyImmediate(item.renderer);
                Object.DestroyImmediate(item.filter);
                Object.DestroyImmediate(item.mesh);
                item.skin.enabled = true;
            }
            for (int i = 0; i < parts.Length; i++) parts[i].gameObject.layer = layers[i];
            RenderTexture.active = previous;
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(pixels);
        }
    }

    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Flags).GetValue(target);
    private static void Call(object target, string name) => target.GetType().GetMethod(name, Flags).Invoke(target, null);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Checks.Add("PASS: " + message);
    }
}
