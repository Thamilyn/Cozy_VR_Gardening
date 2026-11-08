using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Oculus.Interaction;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class EasierStemCutRegression
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<string> Checks = new();
    private static GameObject fixture;

    public static string Main()
    {
        Checks.Clear();
        var source = Object.FindFirstObjectByType<SeedsController>();
        var original = source.PrototypeTomato.seed.HarvestFruit;
        var state = source.GetState(source.PrototypeTomato);
        var tool = Object.FindFirstObjectByType<GardenPruner>();
        var toolGrab = tool.GetComponent<Grabbable>();
        Vector3 toolPosition = tool.transform.position;
        Quaternion toolRotation = tool.transform.rotation;
        var menu = Object.FindFirstObjectByType<GardenMenuController>();
        try
        {
            Check(EditorApplication.isPlaying && EditorApplication.isPaused, "Paused Play fixture");
            Check(Field<float>(tool, "cuttingRadius") == 0.025f && Field<float>(tool, "squeezeToCut") == 0.75f,
                "Real Pruner instance inherits 2.5 cm tolerance and 75 percent deliberate squeeze");
            Check(Resources.Load<Shader>("GardenHarvest/StemCutGuide") != null &&
                !ShaderUtil.ShaderHasError(Resources.Load<Shader>("GardenHarvest/StemCutGuide")), "Stem guide shader imported without errors");
            original.gameObject.SetActive(false);
            Pose pose = new(toolPosition, toolRotation);
            toolGrab.ProcessPointerEvent(new PointerEvent(5911, PointerEventType.Hover, pose));
            toolGrab.ProcessPointerEvent(new PointerEvent(5911, PointerEventType.Select, pose));
            Check(toolGrab.SelectingPointsCount == 1, "Tool held through Meta pointer selection");

            TomatoFruit fruit = CreateFruit(original);
            OrientAcrossStalk(tool, fruit);
            Align(tool, fruit.StemPosition);
            Tick(tool, 0f);
            Call(fruit, "Update");
            var guide = Field<LineRenderer>(fruit, "stemGuide");
            Check(guide.gameObject.activeSelf && guide.startColor.g > guide.startColor.r,
                "Aligned open blades show a green target at the real stalk");
            var label = fruit.GetComponent<GardenObjectLabel>();
            Check(Field<UnityEngine.UI.Text>(label, "captionText").text == "Close Pruner", "Aligned target asks for a deliberate closing action");
            Check(!fruit.IsStemCut && !fruit.IsPickedUp, "Alignment and proximity alone do not cut or pick up");
            Tick(tool, 1f, false);
            Check(!fruit.IsStemCut, "Unheld tool cannot cut");
            Tick(tool, 0f);
            state.tomatoPhase = 4;
            state.stage = GrowthStage.GreenFruit.ToString();
            Tick(tool, 0.75f);
            Call(fruit, "Update");
            Check(!fruit.IsStemCut && !guide.gameObject.activeSelf, "Green source rejects cutting and hides target feedback");
            state.tomatoPhase = 5;
            state.stage = GrowthStage.Mature.ToString();
            Vector3 centre = fruit.transform.TransformPoint(fruit.GetComponent<SphereCollider>().center);
            Vector3 axis = (fruit.StemPosition - centre).normalized;
            Vector3 fruitSurface = centre + axis * (TomatoFruit.DiameterMetres * 0.49f);
            Align(tool, fruitSurface);
            Tick(tool, 0f);
            Tick(tool, 0.75f);
            bool outsideFruit = (bool)typeof(TomatoFruit).GetMethod("IsOutsideFruit", Flags).Invoke(fruit, new object[] { fruitSurface });
            Check(!fruit.IsStemCut && !outsideFruit,
                "Closing on the fruit surface is rejected even inside the enlarged stem tolerance");
            Align(tool, fruit.StemPosition + Vector3.up);
            Tick(tool, 0f);
            Tick(tool, 0.75f);
            Align(tool, fruit.StemPosition);
            Tick(tool, 0.75f);
            Check(!fruit.IsStemCut, "Moving already closed blades onto the stalk still cannot cut");

            Vector3 outward = Vector3.ProjectOnPlane(axis, BladeDirection(tool)).normalized;
            Align(tool, fruit.StemPosition + outward * 0.022f);
            Tick(tool, 0f);
            Tick(tool, 0.6f);
            Check(!fruit.IsStemCut, "Partial 60 percent squeeze is still insufficient");
            Call(fruit, "Update");
            Capture(fruit, tool, "AlignedGreen");
            Vector3 scale = fruit.transform.lossyScale;
            Tick(tool, 0.75f);
            Check(fruit.IsStemCut && !fruit.IsPickedUp && fruit.transform.parent == null,
                "75 percent squeeze cuts at 22 mm offset that the former 15 mm tolerance rejected");
            Check(Vector3.Distance(scale, fruit.transform.lossyScale) < 0.0001f && !guide.gameObject.activeSelf,
                "Cut preserves scale and immediately hides the stem target");
            Tick(tool, 1f);
            Check(!fruit.CanCutStem && !fruit.CanStore, "Repeated closure cannot cut again or count as a pickup/delivery");

            fruit = CreateFruit(original);
            OrientAcrossStalk(tool, fruit);
            Align(tool, fruit.StemPosition);
            Tick(tool, 0f);
            Tick(tool, 0.5f);
            centre = fruit.transform.TransformPoint(fruit.GetComponent<SphereCollider>().center);
            outward = Vector3.ProjectOnPlane(fruit.StemPosition - centre, BladeDirection(tool)).normalized;
            Align(tool, fruit.StemPosition + outward * 0.032f);
            Tick(tool, 0.75f);
            Check(fruit.IsStemCut, "A small 32 mm drift during the same closing stroke uses the brief alignment grace");

            fruit = CreateFruit(original);
            OrientAcrossStalk(tool, fruit);
            Align(tool, fruit.StemPosition);
            Tick(tool, 0f);
            Tick(tool, 0.5f);
            Set(tool, "alignedAt", Time.unscaledTime - 0.2f);
            centre = fruit.transform.TransformPoint(fruit.GetComponent<SphereCollider>().center);
            outward = Vector3.ProjectOnPlane(fruit.StemPosition - centre, BladeDirection(tool)).normalized;
            Align(tool, fruit.StemPosition + outward * 0.032f);
            Tick(tool, 0.75f);
            Check(!fruit.IsStemCut, "Alignment older than 180 ms cannot authorize a distant cut");
            Align(tool, fruit.StemPosition);
            Tick(tool, 0f);
            Tick(tool, 0.5f);
            Align(tool, fruit.StemPosition + outward * 0.06f);
            Tick(tool, 0.75f);
            Check(!fruit.IsStemCut, "Large movement beyond the extra centimetre is rejected");
            Align(tool, fruit.StemPosition);
            Tick(tool, 0f);
            Tick(tool, 0.5f);
            Set(menu, "visible", true);
            Tick(tool, 0.75f);
            Call(fruit, "Update");
            Check(!fruit.IsStemCut && !Field<LineRenderer>(fruit, "stemGuide").gameObject.activeSelf,
                "Pausing blocks the cut and hides target feedback");
            Set(menu, "visible", false);
            Tick(tool, 0.75f);
            Check(!fruit.IsStemCut, "Resuming with closed blades requires reopening");
            Tick(tool, 0f);
            Tick(tool, 0.5f);
            Tick(tool, 0.75f, false);
            Tick(tool, 0.75f);
            Check(!fruit.IsStemCut, "Tracking loss discards alignment memory and requires reopening");
            Tick(tool, 0f);
            Tick(tool, 0.5f);
            Tick(tool, 0.75f, true, 5912);
            Check(!fruit.IsStemCut, "Changing the holding hand discards alignment memory");
            Tick(tool, 0f);
            Set(fruit, "alignmentUntil", float.NegativeInfinity);
            Call(fruit, "Update");
            Check(Field<LineRenderer>(fruit, "stemGuide").startColor.r > Field<LineRenderer>(fruit, "stemGuide").startColor.g,
                "Expired alignment returns the marker to amber");
            Capture(fruit, tool, "TargetAmber");
            Check(source.Calendar.Journal.Count(entry => entry.action == "Harvested") == 0,
                "No cut or feedback sample awards a harvest without pickup and tray delivery");
            Checks.Add("NOT TESTED: physical tracking or perceived ease on Quest 3. Squeeze samples, input loss and grace expiry were supplied synthetically.");
            return string.Join("\n", Checks);
        }
        catch (Exception error) { Checks.Add("FAIL: " + error); throw; }
        finally
        {
            Set(menu, "visible", false);
            state.tomatoPhase = 5;
            state.stage = GrowthStage.Mature.ToString();
            if (fixture != null) Object.DestroyImmediate(fixture);
            original.gameObject.SetActive(true);
            Pose pose = new(tool.transform.position, tool.transform.rotation);
            toolGrab.ProcessPointerEvent(new PointerEvent(5911, PointerEventType.Unselect, pose));
            toolGrab.ProcessPointerEvent(new PointerEvent(5911, PointerEventType.Unhover, pose));
            Tick(tool, 0f, false);
            tool.transform.SetPositionAndRotation(toolPosition, toolRotation);
            var body = tool.GetComponent<Rigidbody>();
            body.position = toolPosition;
            body.rotation = toolRotation;
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            File.WriteAllLines("Docs/Validation/EasierStemCutRegression.txt", Checks);
        }
    }

    private static TomatoFruit CreateFruit(TomatoFruit original)
    {
        if (fixture != null) Object.DestroyImmediate(fixture);
        fixture = new GameObject("Temporary ergonomic harvest fruit");
        fixture.transform.SetParent(original.transform.parent, false);
        fixture.transform.SetLocalPositionAndRotation(original.transform.localPosition, original.transform.localRotation);
        fixture.transform.localScale = original.transform.localScale;
        MeshFilter source = original.GetComponentInChildren<MeshFilter>(true);
        var mesh = new GameObject(source.name);
        mesh.transform.SetParent(fixture.transform, false);
        mesh.transform.SetLocalPositionAndRotation(source.transform.localPosition, source.transform.localRotation);
        mesh.transform.localScale = source.transform.localScale;
        mesh.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
        mesh.AddComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
        var fruit = fixture.AddComponent<TomatoFruit>();
        fruit.Initialize(original.Source);
        return fruit;
    }

    private static Vector3 BladeDirection(GardenPruner tool)
    {
        Transform model = Field<Transform>(tool, "model");
        return model.TransformVector(Field<Vector3>(tool, "cuttingEnd") - Field<Vector3>(tool, "cuttingStart")).normalized;
    }

    private static void OrientAcrossStalk(GardenPruner tool, TomatoFruit fruit)
    {
        Vector3 centre = fruit.transform.TransformPoint(fruit.GetComponent<SphereCollider>().center);
        Vector3 target = Vector3.Cross(fruit.StemPosition - centre, Vector3.up).normalized;
        tool.transform.rotation = Quaternion.FromToRotation(BladeDirection(tool), target) * tool.transform.rotation;
    }

    private static void Align(GardenPruner tool, Vector3 point)
    {
        Transform model = Field<Transform>(tool, "model");
        Vector3 middle = model.TransformPoint((Field<Vector3>(tool, "cuttingStart") + Field<Vector3>(tool, "cuttingEnd")) * 0.5f);
        tool.transform.position += point - middle;
        tool.GetComponent<Rigidbody>().position = tool.transform.position;
    }

    private static void Capture(TomatoFruit fruit, GardenPruner tool, string name)
    {
        Directory.CreateDirectory("Docs/Validation/EasierStemCutVideo");
        var cameraObject = new GameObject("Temporary stem feedback preview");
        var target = new RenderTexture(900, 700, 24);
        var pixels = new Texture2D(900, 700, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 0.13f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 2f;
            camera.targetTexture = target;
            camera.transform.position = fruit.transform.position + new Vector3(0f, 0.03f, -0.4f);
            camera.transform.LookAt(fruit.transform.position);
            Set(fruit, "guideViewer", camera.transform);
            Call(fruit, "Update");
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 900, 700), 0, 0);
            pixels.Apply();
            File.WriteAllBytes("Docs/Validation/EasierStemCutVideo/" + name + ".png", pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(pixels);
        }
    }

    private static void Tick(GardenPruner tool, float squeeze, bool held = true, int id = 5911) =>
        typeof(GardenPruner).GetMethod("UpdateBlades", Flags).Invoke(tool, new object[] { held, squeeze, id, 1f });
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Flags).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    private static void Call(object target, string name) => target.GetType().GetMethod(name, Flags).Invoke(target, null);
    private static void Check(bool passed, string message)
    {
        if (!passed) throw new Exception(message);
        Checks.Add("PASS: " + message);
    }
}
