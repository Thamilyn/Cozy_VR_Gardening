using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Checks the actual demo thumb trajectory and captures its phases in an isolated Play fixture.</summary>
[InitializeOnLoad]
public static class GardenThumbSwipeVerification
{
    private const string SessionKey = "CozyGarden.ThumbSwipeVerification";
    private static double runAt;

    static GardenThumbSwipeVerification() => EditorApplication.playModeStateChanged += OnPlayChanged;

    [MenuItem("Garden/Gronny/Verify visible thumb swipe in Play")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start outside Play.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Garden_Moves")
            throw new InvalidOperationException("Open Garden_Moves first.");
        SessionState.SetBool(SessionKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(SessionKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            runAt = EditorApplication.timeSinceStartup + 1f;
            EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(SessionKey, false);
        }
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < runAt) return;
        EditorApplication.update -= Tick;
        var report = new List<string> { "Unity " + Application.unityVersion, "UTC " + DateTime.UtcNow.ToString("O") };
        GameObject fixture = new("Thumb swipe verification (temporary)");
        try
        {
            var catalogue = Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial");
            CheckClip(catalogue.leftHand, report);
            CheckClip(catalogue.rightHand, report);
            // An unavailable XR runtime leaves the eyes at floor level. Supply a temporary
            // eye pose so the normal placement check can keep the demonstration visible.
            var rig = UnityEngine.Object.FindFirstObjectByType<OVRCameraRig>();
            rig.enabled = false;
            rig.centerEyeAnchor.position = new Vector3(0f, 1.6f, -7.88f);
            var demo = fixture.AddComponent<GardenControlDemonstration>();
            demo.Configure(catalogue);
            demo.SetInputMode(GardenInputMode.Hands);
            var cue = catalogue.Find("controls_move_hands");
            demo.Show(cue);
            Transform model = ((GameObject)Field(demo, "model")).transform;
            Check(model.GetComponentsInChildren<SkinnedMeshRenderer>().Any(renderer => renderer.enabled),
                "Hand mesh remains visible through the normal placement path at eye height", report);
            Transform tip = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == "l_thumb_finger_tip_marker");
            Transform pad = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == "l_thumb_finger_pad_marker");
            Transform[] index = model.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("b_l_index")).ToArray();
            float forwardStart = cue.steps[3].at;
            float forwardEnd = cue.steps[4].at;
            float forwardMiddle = (forwardStart + forwardEnd) * 0.5f;
            float backwardEnd = cue.steps[6].at;
            demo.Tick(forwardStart);
            Vector3 start = tip.position;
            Vector3 padStart = pad.position;
            Quaternion[] indexPose = index.Select(t => t.localRotation).ToArray();
            Vector3 handPosition = model.position;
            demo.Tick(forwardMiddle);
            Vector3 middle = tip.position;
            demo.Tick(forwardEnd);
            Vector3 end = tip.position;
            float padTravel = Vector3.Distance(padStart, pad.position);
            Check(padTravel > 0.012f && padTravel < 0.03f, "Thumb pad makes a short 12–30 mm stroke along the curled index", report);
            Check(Vector3.Distance(start, middle) > 0.003f && Vector3.Distance(middle, end) > 0.003f,
                "Thumb visibly moves within the instruction interval, not only at a pose boundary", report);
            Check(index.Select((t, i) => Quaternion.Angle(t.localRotation, indexPose[i]) < 0.01f).All(same => same) &&
                Vector3.Distance(model.position, handPosition) < 0.00001f,
                "Index stays curled and the whole hand does not substitute for thumb motion", report);
            demo.Tick(backwardEnd);
            Check(Vector3.Distance(tip.position, start) < 0.0001f, "Backward stroke reverses the same thumb trajectory", report);
            demo.Tick(forwardMiddle);
            Check(Vector3.Distance(tip.position, middle) < 0.0001f, "Seeking and repeating do not accumulate bone rotations", report);
            Check(((GameObject)Field(demo, "anchor")).GetComponentsInChildren<LineRenderer>(true).Length == 0,
                "Movement demonstration has no direction arrow or overlaid trajectory", report);
            Transform proximal = index.Single(t => t.name == "b_l_index1");
            Transform knuckle = index.Single(t => t.name == "b_l_index2");
            for (int frame = 0; frame <= 20; frame++)
            {
                demo.Tick(Mathf.Lerp(forwardStart, forwardEnd, frame / 20f));
                Vector3 axis = knuckle.position - proximal.position;
                Vector3 closest = proximal.position + axis * Mathf.Clamp01(Vector3.Dot(pad.position - proximal.position, axis) / axis.sqrMagnitude);
                float gap = Vector3.Distance(pad.position, closest);
                Check(gap > 0.005f && gap < 0.016f, "Thumb pad stays at the index surface: sample " + frame, report);
            }
            demo.Tick(forwardEnd);
            Vector3 pausedPad = pad.position;
            demo.Tick((forwardEnd + cue.steps[5].at) * 0.5f);
            Check(Vector3.Distance(pad.position, pausedPad) < 0.0001f, "Final contact pose pauses before the return stroke", report);
            Check(cue.steps.Last().at < cue.clip.length, "Existing voice covers all authored gesture steps", report);
            Capture(demo, model, cue, report);
            var seeds = UnityEngine.Object.FindFirstObjectByType<SeedsController>();
            Check(seeds.Calendar.Journal.Count == 0 && !GardenMenuController.HasStarted,
                "Preview changes no crop history, tutorial progress or session start", report);
            report.Add("Forward travel (metres): " + Vector3.Distance(start, end).ToString("F5"));
            report.Add("RESULT: PASS");
            Debug.Log("Thumb swipe verification passed.");
        }
        catch (Exception error)
        {
            report.Add("RESULT: FAIL\n" + error);
            Debug.LogError("Thumb swipe verification failed: " + error);
        }
        finally
        {
            UnityEngine.Object.Destroy(fixture);
            Directory.CreateDirectory("output");
            File.WriteAllLines("output/ThumbSwipeVerification.txt", report);
            EditorApplication.isPlaying = false;
        }
    }

    private static void CheckClip(GronnyAudioCatalogue.HandVisual hand, List<string> report)
    {
        var bindings = AnimationUtility.GetCurveBindings(hand.thumbSwipe);
        Transform root = hand.prefab.transform.Find(hand.animationRoot);
        Check(bindings.Length > 0 && bindings.All(binding => binding.path.Contains("_thumb") && root.Find(binding.path) != null),
            hand.animationRoot + ": dedicated swipe clip binds only existing thumb joints", report);
    }

    private static void Capture(GardenControlDemonstration demo, Transform model, GronnyAudioCatalogue.Cue cue, List<string> report)
    {
        const int size = 480;
        var atlas = new Texture2D(size * 4, size, TextureFormat.RGB24, false);
        var target = new RenderTexture(size, size, 24);
        RenderTexture previous = RenderTexture.active;
        GameObject cameraObject = new("Thumb swipe QA camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.cullingMask = 1 << 2;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.045f, 0.07f);
        camera.orthographic = true;
        camera.orthographicSize = 0.11f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 1.5f;
        camera.targetTexture = target;
        var anchor = (GameObject)Field(demo, "anchor");
        demo.Tick((cue.steps[3].at + cue.steps[4].at) * 0.5f);
        var skinned = model.GetComponentsInChildren<SkinnedMeshRenderer>().First();
        Bounds bounds = skinned.bounds;
        camera.transform.SetPositionAndRotation(bounds.center - anchor.transform.forward * 0.6f, anchor.transform.rotation);
        var caption = (TextMesh)Field(demo, "caption");
        caption.transform.position = bounds.center - camera.transform.up * 0.095f;
        caption.transform.rotation = camera.transform.rotation;
        caption.characterSize = 0.006f;
        float[] times = { cue.steps[3].at, (cue.steps[3].at + cue.steps[4].at) * 0.5f, cue.steps[4].at, cue.steps[6].at };
        string[] labels = { "Start", "Swiping", "End", "Back" };
        // Unity can reuse GPU skinning within a frame. Bake each sampled pose for this
        // contact sheet so the image records mesh motion as well as moving bone markers.
        var baked = new Mesh();
        GameObject snapshot = new("Thumb swipe baked QA pose");
        snapshot.layer = 2;
        snapshot.transform.SetParent(skinned.transform, false);
        snapshot.AddComponent<MeshFilter>().sharedMesh = baked;
        snapshot.AddComponent<MeshRenderer>().sharedMaterials = skinned.sharedMaterials;
        skinned.enabled = false;
        Vector3[] firstVertices = null;
        float meshTravel = 0f;
        try
        {
            for (int i = 0; i < times.Length; i++)
            {
                demo.Tick(times[i]);
                caption.text = labels[i];
                skinned.BakeMesh(baked);
                if (i == 0) firstVertices = baked.vertices;
                if (i == 2)
                {
                    Vector3[] endVertices = baked.vertices;
                    for (int j = 0; j < firstVertices.Length; j++)
                        meshTravel = Mathf.Max(meshTravel, skinned.transform.TransformVector(endVertices[j] - firstVertices[j]).magnitude);
                }
                camera.Render();
                RenderTexture.active = target;
                atlas.ReadPixels(new Rect(0, 0, size, size), size * i, 0);
            }
            atlas.Apply();
            Directory.CreateDirectory("output");
            File.WriteAllBytes("output/ThumbSwipeFrames.png", atlas.EncodeToPNG());
            Check(meshTravel > 0.005f, "The skinned hand mesh deforms with the thumb", report);
            CaptureVideo(demo, skinned, baked, camera, target, caption, cue, report);
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            skinned.enabled = true;
            UnityEngine.Object.Destroy(snapshot);
            UnityEngine.Object.Destroy(baked);
            UnityEngine.Object.Destroy(cameraObject);
            UnityEngine.Object.Destroy(target);
            UnityEngine.Object.Destroy(atlas);
        }
    }

    private static void CaptureVideo(GardenControlDemonstration demo, SkinnedMeshRenderer skinned, Mesh baked,
        Camera camera, RenderTexture target, TextMesh caption, GronnyAudioCatalogue.Cue cue, List<string> report)
    {
        var frameImage = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        try
        {
            var attributes = new UnityEditor.Media.VideoTrackAttributes {
                width = (uint)target.width, height = (uint)target.height,
                frameRate = new UnityEditor.Media.MediaRational(30), includeAlpha = false };
            using (var encoder = new UnityEditor.Media.MediaEncoder(Path.GetFullPath("output/ThumbSwipePreview.mp4"), attributes))
            {
                for (int frame = 0; frame < 150; frame++)
                {
                    float time = Mathf.Lerp(cue.steps[3].at, cue.steps[6].at, Mathf.Clamp01((frame - 15f) / 120f));
                    demo.Tick(time);
                    caption.text = time < cue.steps[4].at ? "Slide forward" : time < cue.steps[5].at ? "Pause" : "Slide back";
                    skinned.BakeMesh(baked);
                    camera.Render();
                    RenderTexture.active = target;
                    frameImage.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    frameImage.Apply();
                    if (!encoder.AddFrame(frameImage)) throw new InvalidOperationException("Unity could not encode preview frame " + frame);
                }
            }
            report.Add("PREVIEW: output/ThumbSwipePreview.mp4 (5 seconds, 30 fps, no audio)");
        }
        catch (Exception error) { report.Add("PREVIEW_UNAVAILABLE: " + error.Message); }
        finally { UnityEngine.Object.Destroy(frameImage); }
    }

    private static object Field(object target, string name) => target.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    private static void Check(bool condition, string label, List<string> report)
    {
        if (!condition) throw new InvalidOperationException(label);
        report.Add("PASS: " + label);
    }
}
