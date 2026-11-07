using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Run with Pipeline run_script during a paused, fresh Garden_Moves Play session.
// Simulates the squeeze samples; physical controller/hand tracking needs a Quest check.
public static class PrunerRuntimeCheck
{
    private static readonly StringBuilder report = new StringBuilder();
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    public static string Main()
    {
        report.Clear();
        report.AppendLine("Unity " + Application.unityVersion + " — " + DateTime.UtcNow.ToString("O"));
        try
        {
            Check(EditorApplication.isPlaying && EditorApplication.isPaused, "Paused Play session");
            GardenMenuController menu = Object.FindFirstObjectByType<GardenMenuController>();
            Check(menu != null, "Scene menu available for pause regression");
            Set(menu, "visible", false);
            Set(menu, "awaitingRelease", false);
            Set(menu, "started", true);
            Field<GardenInteractionSuspension>(menu, "suspension").Release();
            Check(!GardenMenuController.GardenSuspended, "Garden input is available");
            GardenPruner pruner = Object.FindFirstObjectByType<GardenPruner>();
            Check(pruner != null && pruner.enabled, "Scene prefab initializes GardenPruner");
            Rigidbody body = pruner.GetComponent<Rigidbody>();
            Check(body != null && pruner.GetComponentsInChildren<Rigidbody>().Length == 1, "One shared Rigidbody");
            Check(pruner.GetComponent<Grabbable>().MaxGrabPoints == 1, "Single-hand grab transform");
            Check(pruner.GetComponentsInChildren<GrabInteractable>().All(g => g.Rigidbody == body) &&
                pruner.GetComponentsInChildren<GrabInteractable>().Length > 0, "Controller grab uses the tool body");
            Check(pruner.GetComponentsInChildren<HandGrabInteractable>().All(g => g.Rigidbody == body) &&
                pruner.GetComponentsInChildren<HandGrabInteractable>().Length > 0, "Hand grab uses the tool body");
            Check(pruner.GetComponentsInChildren<MeshCollider>().Count(c => c.convex && c.enabled) == 2,
                "Separate convex colliders for both moving pieces");
            body.isKinematic = true;

            Transform bottom = Field<Transform>(pruner, "bottomPart");
            Transform top = Field<Transform>(pruner, "topPart");
            Tick(pruner, true, 0f);
            Quaternion openBottom = bottom.localRotation;
            Quaternion openTop = top.localRotation;
            Capture(pruner, "Docs/Validation/PrunerOpen.png");
            Tick(pruner, true, 1f, 2);
            Check(Mathf.Abs(Quaternion.Angle(openBottom, bottom.localRotation) - 22f) < 0.01f &&
                Mathf.Abs(Quaternion.Angle(openTop, top.localRotation) - 22f) < 0.01f, "Both blades animate around the hinge");
            Capture(pruner, "Docs/Validation/PrunerClosed.png");

            foreach (GronnyTutorial tutorial in Object.FindObjectsByType<GronnyTutorial>(FindObjectsSortMode.None))
                tutorial.enabled = false;
            SeedsController seeds = Object.FindFirstObjectByType<SeedsController>();
            GardenPot pot = Object.FindObjectsByType<GardenPot>(FindObjectsSortMode.None)
                .First(p => p.AcceptsTomato && p.GetComponent<TomatoPotStages>() != null);
            SeedItem seed = Object.FindObjectsByType<SeedItem>(FindObjectsSortMode.None)
                .First(s => s.Crop == SeedCrop.Tomato && !s.IsPlanted);
            seed.transform.position = pot.PlantingPoint;
            Check(seeds.TryPlant(seed), "Plant actual tomato seed in actual pot");
            seeds.PrototypeTomato.stage = GrowthStage.GreenFruit;
            seeds.GetState(seeds.PrototypeTomato).tomatoPhase = 4;
            seeds.GetState(seeds.PrototypeTomato).stage = GrowthStage.GreenFruit.ToString();
            seed.ShowStage(GrowthStage.GreenFruit);
            Check(seed.StageVisual.GetComponentInChildren<TomatoFruit>() == null, "Green fruit has no cuttable ripe component");
            seeds.PrototypeTomato.stage = GrowthStage.Mature;
            seeds.GetState(seeds.PrototypeTomato).tomatoPhase = 5;
            seeds.GetState(seeds.PrototypeTomato).stage = GrowthStage.Mature.ToString();
            seed.ShowStage(GrowthStage.Mature);
            TomatoFruit fruit = seed.StageVisual.GetComponentInChildren<TomatoFruit>();
            Check(fruit != null && fruit.CanCutStem, "Only mature attached fruit becomes cuttable");
            // The fruit was spawned while paused; initialize its normal Meta Start
            // before delivering pointer events without advancing the whole scene.
            Invoke(fruit.GetComponent<Grabbable>(), "Start");
            Transform attachment = fruit.transform.parent;
            Vector3 attachedPosition = fruit.transform.localPosition;
            Vector3 attachedScale = fruit.transform.localScale;
            Vector3 worldScale = fruit.transform.lossyScale;

            pruner.transform.position = fruit.StemPosition + Vector3.right;
            Tick(pruner, true, 0f);
            Tick(pruner, true, 1f);
            Check(fruit.CanCutStem, "A complete snip far from the stalk does not detach fruit");
            Align(pruner, fruit.StemPosition);
            Tick(pruner, true, 1f);
            Check(fruit.CanCutStem, "Moving closed blades onto the stalk does not cut");
            Tick(pruner, false, 0f);
            Tick(pruner, true, 1f);
            Check(fruit.CanCutStem, "Grabbing with the trigger held does not cut");
            Tick(pruner, true, 0f);
            Tick(pruner, true, 1f, 7);
            Check(fruit.CanCutStem, "Changing the holding hand disarms the cut");
            Tick(pruner, true, 0f, 7);
            Tick(pruner, false, 0f, -1);
            Tick(pruner, true, 1f, 7);
            Check(fruit.CanCutStem, "Lost tracking requires reopening before another cut");

            Tick(pruner, true, 0f, 7);
            Set(menu, "visible", true);
            try { Tick(pruner, true, 1f, 7); }
            finally { Set(menu, "visible", false); }
            Tick(pruner, true, 1f, 7);
            Check(fruit.CanCutStem, "Menu pause suppresses cutting and requires reopening");
            Tick(pruner, true, 0f, 7);
            Tick(pruner, true, 0.6f, 7);
            Check(fruit.CanCutStem, "Partial squeeze animates without cutting");
            Tick(pruner, true, 1f, 7);
            Check(fruit.transform.parent == null && !fruit.CanCutStem, "Full closing stroke detaches ripe fruit");
            Check(!fruit.GetComponent<Rigidbody>().isKinematic && fruit.GetComponent<Rigidbody>().useGravity,
                "Cut fruit is released with dynamic gravity");
            Check(Vector3.Distance(worldScale, fruit.transform.lossyScale) < 0.0001f, "Cut preserves fruit world scale");
            Check(!seeds.HarvestComplete && !fruit.TryCutStem(), "Snipping does not count harvest or allow a second cut");

            Set(fruit, "recoveryRemaining", 0f);
            Invoke(fruit, "Update");
            Check(fruit.transform.parent == attachment && fruit.CanCutStem &&
                Vector3.Distance(attachedPosition, fruit.transform.localPosition) < 0.0001f &&
                Vector3.Distance(attachedScale, fruit.transform.localScale) < 0.0001f,
                "Recovery restores plant attachment and scale and permits another cut");
            Tick(pruner, true, 1f, 7);
            Check(fruit.CanCutStem, "Holding closed after recovery cannot repeat the cut");
            Tick(pruner, true, 0f, 7);
            Tick(pruner, true, 1f, 7);
            Check(!fruit.CanCutStem && fruit.transform.parent == null, "Opening rearms the next snip");

            Grabbable fruitGrab = fruit.GetComponent<Grabbable>();
            Pose pose = new Pose(fruit.transform.position, fruit.transform.rotation);
            fruitGrab.ProcessPointerEvent(new PointerEvent(9901, PointerEventType.Hover, pose));
            fruitGrab.ProcessPointerEvent(new PointerEvent(9901, PointerEventType.Select, pose));
            Check(fruitGrab.SelectingPointsCount == 1 && !fruit.TryCutStem(), "Cut tomato can be picked up and cannot be cut while held");
            TomatoHarvestBasket tray = Object.FindFirstObjectByType<TomatoHarvestBasket>();
            BoxCollider volume = new SerializedObject(tray).FindProperty("collectionVolume").objectReferenceValue as BoxCollider;
            pose.position = volume.transform.TransformPoint(volume.center);
            fruitGrab.ProcessPointerEvent(new PointerEvent(9901, PointerEventType.Move, pose));
            fruitGrab.ProcessPointerEvent(new PointerEvent(9901, PointerEventType.Unselect, pose));
            fruitGrab.ProcessPointerEvent(new PointerEvent(9901, PointerEventType.Unhover, pose));
            Invoke(fruit, "Update");
            Check(seeds.HarvestComplete && fruit.transform.parent == tray.transform && !fruitGrab.enabled,
                "Delivering cut fruit to the real tray records harvest and disables grabbing");
            int harvests = seeds.Calendar.Journal.Count(e => e.action == "Harvested");
            Check(harvests == 1, "Exactly one harvest journal entry");
            Check(!fruit.TryCutStem(), "Stored fruit cannot be cut again");
            Invoke(fruit, "Update");
            Check(seeds.Calendar.Journal.Count(e => e.action == "Harvested") == harvests,
                "Stored fruit does not duplicate journal entries");
            report.AppendLine("NOT TESTED: physical Quest 3 tracking, analog trigger hardware, hand gesture comfort, APK or device performance.");
            return report.ToString();
        }
        catch (Exception exception)
        {
            report.AppendLine("FAIL: " + exception);
            throw;
        }
        finally { File.WriteAllText("Docs/Validation/PrunerRuntimeVerification.txt", report.ToString()); }
    }

    private static void Tick(GardenPruner pruner, bool held, float squeeze, int hand = 1) =>
        typeof(GardenPruner).GetMethod("UpdateBlades", PrivateInstance).Invoke(pruner, new object[] { held, squeeze, hand, 1f });

    private static void Align(GardenPruner pruner, Vector3 stalk)
    {
        Transform model = Field<Transform>(pruner, "model");
        Vector3 center = model.TransformPoint((Field<Vector3>(pruner, "cuttingStart") + Field<Vector3>(pruner, "cuttingEnd")) * 0.5f);
        pruner.transform.position += stalk - center;
        pruner.GetComponent<Rigidbody>().position = pruner.transform.position;
    }

    private static T Field<T>(object target, string field) => (T)target.GetType().GetField(field, PrivateInstance).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, PrivateInstance).SetValue(target, value);
    private static void Invoke(object target, string method) => target.GetType().GetMethod(method, PrivateInstance).Invoke(target, null);
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        report.AppendLine("PASS: " + description);
    }

    private static void Capture(GardenPruner pruner, string path)
    {
        GameObject cameraObject = new GameObject("Pruner verification camera");
        RenderTexture texture = new RenderTexture(640, 640, 24);
        Texture2D image = new Texture2D(640, 640, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        Transform[] parts = pruner.GetComponentsInChildren<Transform>(true);
        int[] layers = parts.Select(part => part.gameObject.layer).ToArray();
        try
        {
            foreach (Transform part in parts) part.gameObject.layer = 31;
            Transform model = Field<Transform>(pruner, "model");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 0.14f;
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.16f, 0.17f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 2f;
            camera.transform.SetPositionAndRotation(pruner.transform.position + model.up * 0.5f,
                Quaternion.LookRotation(-model.up, model.forward));
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            for (int i = 0; i < parts.Length; i++) parts[i].gameObject.layer = layers[i];
            RenderTexture.active = previous;
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(image);
            texture.Release();
            Object.DestroyImmediate(texture);
        }
    }
}
