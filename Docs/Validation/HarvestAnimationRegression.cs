using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Oculus.Interaction;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class HarvestAnimationRegression
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
            var cut = catalogue.Find("harvest_cut_stem");
            var pick = catalogue.Find("harvest_stem_cut");
            Check((int)GardenControlDemo.WateringGrab == 7 && (int)GardenControlDemo.CutStem == 8,
                "Existing serialized demonstration values preserved");
            Check(catalogue.pruningShearsVisual != null && cut.demo == GardenControlDemo.CutStem &&
                pick.demo == GardenControlDemo.PickTomato, "Recorded cues reference the new visuals and existing Pruner");
            fixture = new GameObject("Temporary harvest UI regression");
            var demo = fixture.AddComponent<GardenControlDemonstration>();
            var guide = Object.FindFirstObjectByType<TomatoPrototypeGuide>();
            demo.Configure(catalogue, guide.GardeningDemonstrationAnchor);
            foreach (GardenInputMode mode in new[] { GardenInputMode.Hands, GardenInputMode.Controllers })
            {
                demo.SetInputMode(mode);
                demo.Show(cut);
                var anchor = Field<GameObject>(demo, "anchor");
                Check(anchor != null && demo.IsShowing(cut), mode + ": cut example appears");
                VerifyNonInteractive(anchor, mode + " cut");
                object harvest = Field<object>(demo, "harvest");
                Transform blade = Field<Transform>(harvest, "bottomBlade");
                GameObject stem = Field<GameObject>(harvest, "stem");
                Transform fruit = Field<Transform>(harvest, "tomato");
                demo.TickSpoken(6f);
                Quaternion open = blade.localRotation;
                Vector3 connected = fruit.localPosition;
                var handSkin = mode == GardenInputMode.Hands
                    ? Field<GameObject>(demo, "model").GetComponentInChildren<SkinnedMeshRenderer>() : null;
                var openHand = new Mesh();
                if (handSkin != null) handSkin.BakeMesh(openHand);
                Check(stem.activeSelf, mode + ": open blades leave the example stem intact");
                Capture(anchor, "CutOpen-" + mode);
                demo.TickSpoken(9.5f);
                if (handSkin != null)
                {
                    var closedHand = new Mesh();
                    handSkin.BakeMesh(closedHand);
                    Vector3[] openVertices = openHand.vertices;
                    Vector3[] closedVertices = closedHand.vertices;
                    float maximumMovement = 0f;
                    for (int i = 0; i < openVertices.Length; i++)
                        maximumMovement = Mathf.Max(maximumMovement, Vector3.Distance(openVertices[i], closedVertices[i]));
                    Check(maximumMovement > 0.02f, "Hands: squeeze deforms the actual Meta hand mesh");
                    Object.DestroyImmediate(closedHand);
                }
                Object.DestroyImmediate(openHand);
                Check(Quaternion.Angle(open, blade.localRotation) > 21f && !stem.activeSelf &&
                    fruit.localPosition.y < connected.y - 0.02f, mode + ": closing blades separates one whole fruit");
                Check(Field<TextMesh>(demo, "caption").text.Contains(mode == GardenInputMode.Hands ? "hand" : "Trigger"),
                    mode + ": caption explains the matching input");
                Capture(anchor, "CutClosed-" + mode);
                demo.Show(pick);
                anchor = Field<GameObject>(demo, "anchor");
                VerifyNonInteractive(anchor, mode + " pickup");
                Check(demo.UsesHandTeachingTiming && demo.CycleDurationSeconds >= 7.5f,
                    mode + ": silent pickup practice holds poses for a readable cycle");
                harvest = Field<object>(demo, "harvest");
                fruit = Field<Transform>(harvest, "tomato");
                demo.TickTeaching(3.4f);
                Vector3 grabbed = fruit.position;
                Capture(anchor, "PickGrip-" + mode);
                demo.TickTeaching(5.9f);
                Check(fruit.position.y > grabbed.y + 0.05f, mode + ": picked example tomato lifts with the hand/controller");
                Capture(anchor, "PickLift-" + mode);
                demo.SetInputMode(GardenInputMode.Unknown);
                Check(Field<GameObject>(demo, "anchor") == null, mode + ": changing mode removes the previous animation");
            }
            demo.SetInputMode(GardenInputMode.Hands);
            var voice = fixture.AddComponent<GronnyVoicePlayer>();
            voice.Configure(catalogue, demo);
            bool needsCut = true;
            bool needsPick = false;
            voice.SetBackgroundDemonstration(cut.id, () => needsCut);
            Call(voice, "Update");
            Check(demo.IsShowing(cut), "Cut animation continues without replaying speech while awaiting the action");
            voice.Enqueue(cut.id, "cut-visual-test", () => needsCut);
            Set(voice, "nextVoiceAt", 0f);
            Call(voice, "Update");
            needsCut = false;
            needsPick = true;
            voice.SetBackgroundDemonstration(pick.id, () => needsPick);
            Call(voice, "Update");
            Check(demo.IsShowing(pick), "Actual cut immediately replaces the stale visual while voice can finish");
            needsPick = false;
            Call(voice, "Update");
            Check(Field<GameObject>(demo, "anchor") == null, "Actual pickup removes the harvest animation");
            voice.SuspendForMenu();
            Check(Field<GameObject>(demo, "anchor") == null, "Menu suspension hides the demonstration");
            Checks.Add("NOT TESTED: headset comfort, physical input or device frame time. Rendered views and synthetic animation times used.");
            return string.Join("\n", Checks);
        }
        catch (Exception error) { Checks.Add("FAIL: " + error); throw; }
        finally
        {
            if (fixture != null) Object.DestroyImmediate(fixture);
            File.WriteAllLines("Docs/Validation/HarvestAnimationRegression.txt", Checks);
        }
    }

    private static void VerifyNonInteractive(GameObject anchor, string context)
    {
        Check(anchor.GetComponentsInChildren<Collider>(true).All(collider => !collider.enabled) &&
            anchor.GetComponentsInChildren<Rigidbody>(true).All(body => body.isKinematic && !body.detectCollisions),
            context + ": visual has no physical interaction");
        Check(anchor.GetComponentsInChildren<Grabbable>(true).Length == 0 &&
            anchor.GetComponentsInChildren<GardenPruner>(true).Length == 0 &&
            anchor.GetComponentsInChildren<TomatoFruit>(true).Length == 0,
            context + ": example cannot cut, harvest or register a Meta grab");
    }

    private static void Capture(GameObject anchor, string name)
    {
        Directory.CreateDirectory("Docs/Validation/HarvestAnimations");
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
            camera.transform.SetPositionAndRotation(anchor.transform.position - anchor.transform.forward * 1.5f,
                anchor.transform.rotation);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 960, 960), 0, 0);
            pixels.Apply();
            File.WriteAllBytes("Docs/Validation/HarvestAnimations/" + name + ".png", pixels.EncodeToPNG());
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
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    private static void Call(object target, string name) => target.GetType().GetMethod(name, Flags).Invoke(target, null);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Checks.Add("PASS: " + message);
    }
}
