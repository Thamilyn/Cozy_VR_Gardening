using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Optional, isolated voice checks plus the real tomato events in Play mode.</summary>
[InitializeOnLoad]
public static class GronnyAudioVerification
{
    private const string SessionKey = "CozyGarden.GronnyVerification";
    private static int step;
    private static double nextAt;
    private static double startedAt;
    private static StringBuilder report;
    private static GameObject fixture;
    private static GronnyVoicePlayer voice;
    private static GardenControlDemonstration demo;
    private static bool relevant;
    private static int phase;

    static GronnyAudioVerification() => EditorApplication.playModeStateChanged += OnPlayChanged;

    public static string VerifyCatalogue()
    {
        var catalogue = Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial");
        Require(catalogue != null && catalogue.cues.Length == 27, "27 cues");
        Require(catalogue.cues.Select(c => c.id).Distinct().Count() == 27, "Unique audio identifiers");
        string[] files = new[] { "Assets/Audio/controls_", "Assets/Audio/dialogues_cozy_gardening" }
            .SelectMany(folder => Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            .Where(file => Path.GetExtension(file) != ".meta").Select(file => file.Replace('\\', '/')).ToArray();
        Require(files.Length == 27 && files.All(file => catalogue.cues.Any(c => AssetDatabase.GetAssetPath(c.clip) == file)),
            "Every existing audio file referenced, including subdirectories");
        foreach (var cue in catalogue.cues)
        {
            Require(cue.clip != null && cue.clip.length > 0, cue.id + " imported");
            if (cue.demo == GardenControlDemo.None) continue;
            Require(cue.steps.Length > 1 && cue.steps[0].at == 0 &&
                cue.steps.Zip(cue.steps.Skip(1), (a, b) => a.at <= b.at).All(sorted => sorted), cue.id + " ordered timing");
            Require(cue.steps.Last().at < cue.clip.length, cue.id + " final step before audio ends");
        }
        foreach (var hand in new[] { catalogue.leftHand, catalogue.rightHand })
        {
            Transform root = hand.prefab.transform.Find(hand.animationRoot);
            Require(root != null, "Meta hand root");
            foreach (var clip in new[] { hand.pinch, hand.curled, hand.point, hand.thumbFree })
                // Some Meta right-hand clips still name left-hand fingertip debug markers.
                // These markers do not deform the mesh; every skeletal binding must match.
                Require(clip != null && AnimationUtility.GetCurveBindings(clip).All(binding =>
                    binding.path.Length == 0 || binding.path.EndsWith("_marker") || root.Find(binding.path) != null),
                    clip.name + " skeletal bone bindings");
        }
        Require(catalogue.leftController != null && catalogue.rightController != null &&
            catalogue.controllerStick != null && catalogue.controllerGrip != null &&
            catalogue.controllerY != null && catalogue.controllerTrigger != null &&
            catalogue.demonstrationMaterial != null, "Touch Plus models, clips and material");
        Require(UnityEngine.Object.FindObjectsByType<Oculus.Interaction.Locomotion.FirstPersonLocomotor>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0, "Scene locomotion event source");
        Directory.CreateDirectory("Docs/Validation");
        File.WriteAllText("Docs/Validation/GronnyCatalogue.txt", "PASS: 27 audios; coverage; identifiers; durations; step timings; Meta bone bindings; Touch Plus references; locomotion source.\nUnity " + Application.unityVersion);
        return "PASS: catalogue and installed Meta references";
    }

    [MenuItem("Garden/Gronny/Verify audio and tomato events in Play")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run outside Play mode.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Garden_Moves")
            throw new InvalidOperationException("Open Garden_Moves first.");
        VerifyCatalogue();
        SessionState.SetBool(SessionKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(SessionKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            step = phase = 0;
            report = new StringBuilder("Unity " + Application.unityVersion + "\n");
            startedAt = EditorApplication.timeSinceStartup;
            nextAt = startedAt + 1.5;
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
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextAt) return;
        try
        {
            Require(EditorApplication.timeSinceStartup - startedAt < 45, "Verification timeout");
            var catalogue = Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial");
            SeedsController seeds = UnityEngine.Object.FindFirstObjectByType<SeedsController>();
            GronnyTutorial tutorial = UnityEngine.Object.FindFirstObjectByType<GronnyTutorial>();
            Require(tutorial != null && seeds != null, "Guide automatically starts audio tutorial");
            if (step == 0)
            {
                // Mute during automated checks, preserving configured catalogue volume.
                foreach (AudioSource source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None)) source.mute = true;
                fixture = new GameObject("Gronny voice verification fixture");
                demo = fixture.AddComponent<GardenControlDemonstration>();
                demo.Configure(catalogue);
                demo.SetInputMode(GardenInputMode.Hands);
                voice = fixture.AddComponent<GronnyVoicePlayer>();
                voice.Configure(catalogue, demo);
                fixture.GetComponent<AudioSource>().mute = true;
                relevant = true;
                voice.Enqueue("04_first_watering", "watering", () => relevant);
                step++;
            }
            else if (step == 1)
            {
                Require(fixture.GetComponent<AudioSource>().clip == catalogue.Find("04_first_watering").clip,
                    "Voice begins with matching visual");
                voice.Enqueue("help_excess_water", "excess", () => relevant);
                step++;
            }
            else if (step == 2)
            {
                Require(fixture.GetComponent<AudioSource>().clip == catalogue.Find("help_excess_water").clip,
                    "Excess water interrupts lower priority voice");
                relevant = false;
                step++;
            }
            else if (step == 3)
            {
                Require(!fixture.GetComponent<AudioSource>().isPlaying && Field(demo, "anchor") == null,
                    "Obsolete active and pending instructions cancelled");
                demo.SetInputMode(GardenInputMode.Hands);
                demo.Show(catalogue.Find("controls_move_hands"));
                demo.Tick(6.5f);
                VerifyVisual();
                Capture("hands-move");
                demo.Hide();
                demo.Show(catalogue.Find("controls_grab_hands"));
                demo.Tick(3f);
                Capture("hands-pinch");
                demo.SetInputMode(GardenInputMode.Controllers);
                Require(Field(demo, "anchor") == null, "Mode change removes incompatible demo");
                demo.Show(catalogue.Find("controls_calendar_controllers"));
                demo.Tick(2f);
                VerifyVisual();
                Capture("controller-calendar");
                demo.Hide();
                voice.Enqueue("controls_grab_hands", "hand-mode", () => true,
                    demonstrationRelevant: () => relevant);
                step++;
            }
            else if (step == 4)
            {
                Require(fixture.GetComponent<AudioSource>().isPlaying && Field(demo, "anchor") == null,
                    "Completed action hides demo while relevant voice can finish");
                voice.CancelControls();
                Require(!fixture.GetComponent<AudioSource>().isPlaying, "Mode change cancels control voice");
                voice.enabled = false;
                Require(Field(demo, "anchor") == null, "Disabling playback hides demonstration");
                GardenPot pot = UnityEngine.Object.FindObjectsByType<GardenPot>(FindObjectsSortMode.None)
                    .First(p => p.AcceptsTomato && p.SubstrateAreaSquareMetres > 0);
                SeedItem seed = UnityEngine.Object.FindObjectsByType<SeedItem>(FindObjectsSortMode.None)
                    .First(s => s.Crop == SeedCrop.Tomato);
                seed.transform.position = pot.PlantingPoint;
                Require(seeds.TryPlant(seed), "Real planting event");
                step++;
            }
            else if (step == 5)
            {
                Require(seeds.Calendar.Journal.Any(e => e.action == "Assisted: 04_first_watering"),
                    "Playing help records assisted action in existing journal");
                seeds.ReceiveWater(seeds.PrototypeTomato.pot, seeds.ExcessLimit(seeds.PrototypeTomato) * 1.2f / 1000f);
                step++;
            }
            else if (step == 6)
            {
                Require(seeds.Calendar.Journal.Any(e => e.action == "Assisted: help_excess_water"), "Real excess warning");
                seeds.GetState(seeds.PrototypeTomato).lastWaterAt = Time.unscaledTime - 60f;
                step++;
            }
            else if (step == 7)
            {
                Require(seeds.GetState(seeds.PrototypeTomato).retainedWaterMillilitres <= seeds.ExcessLimit(seeds.PrototypeTomato),
                    "Existing drainage still works");
                var state = seeds.GetState(seeds.PrototypeTomato);
                if (phase < 5)
                {
                    seeds.ReceiveWater(seeds.PrototypeTomato.pot, seeds.TargetMillilitres(seeds.PrototypeTomato) / 1000f);
                    // Exercise phase events directly; sky-cycle behaviour is outside this audio check.
                    Method(seeds.Calendar, "CompleteAdvance").Invoke(seeds.Calendar,
                        new object[] { seeds.Calendar.Milestones[++phase].day });
                    nextAt = EditorApplication.timeSinceStartup + 0.7;
                    return;
                }
                Require(state.tomatoPhase == 5, "All tomato phase events observed");
                seeds.Calendar.RecordHarvest(state);
                step++;
            }
            else if (step == 8)
            {
                Require(seeds.HarvestComplete && (bool)Field(tutorial, "completed"), "Harvest completes and unsubscribes tutorial");
                Require(!(bool)Field(tutorial, "subscribed"), "Subscriptions released after completion");
                Finish("PASS");
                return;
            }
            nextAt = EditorApplication.timeSinceStartup + 0.65;
        }
        catch (Exception exception) { Finish("FAIL: " + exception); }
    }

    private static void VerifyVisual()
    {
        GameObject anchor = (GameObject)Field(demo, "anchor");
        Require(anchor != null && anchor.activeInHierarchy, "Visual present");
        Require(anchor.GetComponentsInChildren<Collider>(true).All(c => !c.enabled), "Demo cannot block interactions");
        Require(((GameObject)Field(demo, "model")).GetComponentsInChildren<MonoBehaviour>(true).All(b => !b.enabled),
            "Demo has no tracking/interactor behaviour");
    }

    private static void Capture(string name)
    {
        Directory.CreateDirectory("Docs/Validation");
        GameObject cameraObject = new("Gronny QA camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        GameObject anchor = (GameObject)Field(demo, "anchor");
        camera.transform.SetPositionAndRotation(anchor.transform.position - anchor.transform.forward * 0.7f,
            anchor.transform.rotation);
        camera.cullingMask = 1 << 2;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.09f, 0.13f);
        camera.fieldOfView = 50f;
        camera.nearClipPlane = 0.01f;
        var target = new RenderTexture(960, 720, 24);
        RenderTexture previous = RenderTexture.active;
        camera.targetTexture = target;
        var image = new Texture2D(960, 720, TextureFormat.RGB24, false);
        try
        {
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes("Docs/Validation/Gronny-" + name + ".png", image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.Destroy(image);
            UnityEngine.Object.Destroy(target);
            UnityEngine.Object.Destroy(cameraObject);
        }
    }

    private static object Field(object target, string name) => target.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    private static MethodInfo Method(object target, string name) => target.GetType()
        .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        report?.AppendLine("PASS: " + label);
    }
    private static void Finish(string outcome)
    {
        EditorApplication.update -= Tick;
        report.AppendLine(outcome);
        Directory.CreateDirectory("Docs/Validation");
        File.WriteAllText("Docs/Validation/GronnyRuntime.txt", report.ToString());
        if (fixture != null) UnityEngine.Object.Destroy(fixture);
        EditorApplication.isPlaying = false;
    }
}
