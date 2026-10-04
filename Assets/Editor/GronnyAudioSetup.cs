using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Creates the initial catalogue once; later Inspector timing edits are preserved.</summary>
public static class GronnyAudioSetup
{
    private const string CataloguePath = "Assets/Resources/GronnyAudio/Tutorial.asset";
    private const string Interaction = "Packages/com.meta.xr.sdk.interaction";
    private const string Core = "Packages/com.meta.xr.sdk.core";

    [MenuItem("Garden/Gronny/Create initial audio catalogue")]
    public static void CreateInitialCatalogue()
    {
        if (AssetDatabase.LoadAssetAtPath<GronnyAudioCatalogue>(CataloguePath) != null)
        {
            Debug.Log("Gronny catalogue already exists. Inspector edits have been preserved.");
            return;
        }
        Directory.CreateDirectory("Assets/Resources/GronnyAudio");
        AssetDatabase.Refresh();
        var catalogue = ScriptableObject.CreateInstance<GronnyAudioCatalogue>();
        var cues = new List<GronnyAudioCatalogue.Cue>();
        string[] ids = {
            "01_welcome", "02_tomato_intro", "03_plant_seed", "04_first_watering", "05_water_target",
            "06_enough_water", "07_first_advance", "08_sprout", "09_young_plant", "10_flowering",
            "11_green_fruit", "12_harvest", "13_complete", "help_planting", "help_low_water",
            "help_excess_water", "help_drained", "help_calendar", "help_refill"
        };
        foreach (string id in ids)
        {
            var cue = MakeCue(id, "Assets/Audio/dialogues_cozy_gardening", GardenInputMode.Unknown);
            if (id == "04_first_watering")
            {
                cue.demo = GardenControlDemo.Water;
                cue.steps = new[] { Step(0, GardenDemoPose.Open, "Pick up the watering can"),
                    Step(2.6f, GardenDemoPose.Pinch, "Hold the can"),
                    Step(4.6f, GardenDemoPose.Pinch, "Tilt over the soil", rotation: new Vector3(0, 0, -65)),
                    Step(7.0f, GardenDemoPose.Pinch, "Aim near the roots", rotation: new Vector3(0, 0, -65)) };
            }
            if (id == "06_enough_water" || id == "help_excess_water")
            {
                cue.demo = GardenControlDemo.Upright;
                cue.steps = new[] { Step(0, GardenDemoPose.Pinch, "Stop pouring", rotation: new Vector3(0, 0, -65)),
                    Step(1.2f, GardenDemoPose.Pinch, "Hold the can upright") };
                cue.priority = id == "help_excess_water" ? 100 : 50;
            }
            if (id == "help_refill") cue.priority = 20;
            cues.Add(cue);
        }
        foreach (GardenInputMode mode in new[] { GardenInputMode.Hands, GardenInputMode.Controllers })
        {
            string suffix = mode == GardenInputMode.Hands ? "hands" : "controllers";
            foreach (string control in new[] { "move", "grab", "calendar", "calendar_select" })
            {
                var cue = MakeCue("controls_" + control + "_" + suffix, "Assets/Audio/controls_", mode);
                cue.demo = control switch { "move" => GardenControlDemo.Move, "grab" => GardenControlDemo.Grab,
                    "calendar" => GardenControlDemo.Calendar, _ => GardenControlDemo.CalendarSelect };
                cue.steps = ControlSteps(control, mode);
                cues.Add(cue);
            }
        }
        catalogue.cues = cues.ToArray();
        catalogue.leftHand = Hand("Left", "l");
        catalogue.rightHand = Hand("Right", "r");
        string left = Core + "/Meshes/MetaQuestTouchPlus/MetaQuestTouchPlus_Left.fbx";
        string right = Core + "/Meshes/MetaQuestTouchPlus/MetaQuestTouchPlus_Right.fbx";
        catalogue.leftController = Require<GameObject>(left);
        catalogue.rightController = Require<GameObject>(right);
        catalogue.controllerStick = Clip(left, "left_touchplus_controller_stickN");
        catalogue.controllerY = Clip(left, "left_touchplus_controller_button02");
        catalogue.controllerGrip = Clip(right, "right_quest_touch_plus_controller_grip");
        catalogue.controllerTrigger = Clip(right, "right_quest_touch_plus_controller_trigger");
        ConfigureControllerTargets(catalogue);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) throw new InvalidOperationException("The installed URP Unlit shader is missing.");
        var material = new Material(shader) { name = "Gronny demonstration" };
        material.SetColor("_BaseColor", new Color(0.4f, 0.85f, 0.9f, 1f));
        AssetDatabase.CreateAsset(material, "Assets/Resources/GronnyAudio/Demonstration.mat");
        catalogue.demonstrationMaterial = material;
        AssetDatabase.CreateAsset(catalogue, CataloguePath);
        AssetDatabase.SaveAssets();
        WriteInventory(catalogue);
        Debug.Log("Gronny: connected all " + cues.Count + " audio files.");
    }

    private static GronnyAudioCatalogue.Cue MakeCue(string id, string folder, GardenInputMode mode)
    {
        string[] matches = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
            .Where(file => Path.GetFileNameWithoutExtension(file) == id && Path.GetExtension(file) != ".meta")
            .ToArray();
        if (matches.Length != 1) throw new InvalidOperationException(id + ": expected one audio file; found " + matches.Length);
        return new GronnyAudioCatalogue.Cue { id = id, clip = Require<AudioClip>(matches[0].Replace('\\', '/')),
            mode = mode, assisted = id.StartsWith("help_") || id.StartsWith("controls_") ||
                id == "03_plant_seed" || id == "04_first_watering" || id == "05_water_target" ||
                id == "06_enough_water" || id == "07_first_advance" || id == "08_sprout" || id == "12_harvest",
            cooldown = id == "help_excess_water" ? 8f : id.StartsWith("help_") ? 35f : 0f };
    }

    private static GronnyAudioCatalogue.HandVisual Hand(string side, string suffix)
    {
        string animations = Interaction + "/Runtime/Animations/Hands/";
        return new GronnyAudioCatalogue.HandVisual {
            prefab = Require<GameObject>(Interaction + "/Runtime/Prefabs/HandGrab/Ghost-Hand" + side + ".prefab"),
            animationRoot = "OculusHand_" + suffix.ToUpperInvariant(),
            pinch = Require<AnimationClip>(animations + "HandPinch_" + suffix + "_.anim"),
            curled = Require<AnimationClip>(animations + "HandMidFist_" + suffix + "_.anim"),
            point = Require<AnimationClip>(animations + "IndexPoint_" + suffix + "_.anim"),
            thumbFree = Require<AnimationClip>(animations + "ThumbUp_" + suffix + "_.anim") };
    }

    private static AnimationClip Clip(string path, string name) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(clip => clip.name == name)
        ?? throw new InvalidOperationException("Meta controller animation is missing: " + name);

    public static void ConfigureControllerTargets(GronnyAudioCatalogue catalogue)
    {
        foreach (var cue in catalogue.cues.Where(cue => cue.mode == GardenInputMode.Controllers))
        {
            AnimationClip clip = cue.demo switch {
                GardenControlDemo.Move => catalogue.controllerStick,
                GardenControlDemo.Grab => catalogue.controllerGrip,
                GardenControlDemo.Calendar => catalogue.controllerY,
                _ => catalogue.controllerTrigger };
            cue.controllerTarget = AnimationUtility.GetCurveBindings(clip).First().path;
        }
    }

    private static T Require<T>(string path) where T : UnityEngine.Object =>
        AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing asset: " + path);

    private static GronnyAudioCatalogue.Step Step(float at, GardenDemoPose pose, string label,
        Vector3 position = default, Vector3 rotation = default, float press = 0f, Vector3 thumbRotation = default) =>
        new() { at = at, pose = pose, label = label, position = position, rotation = rotation, press = press,
            thumbRotation = thumbRotation };

    public static void ConfigureHandMovement(GronnyAudioCatalogue catalogue) =>
        catalogue.Find("controls_move_hands").steps = ControlSteps("move", GardenInputMode.Hands);

    private static GronnyAudioCatalogue.Step[] ControlSteps(string control, GardenInputMode mode)
    {
        bool hands = mode == GardenInputMode.Hands;
        return control switch {
            "move" when hands => new[] {
                Step(0, GardenDemoPose.ThumbFree, "Curl the index; keep the thumb free"),
                Step(3.2f, GardenDemoPose.Curled, "Tap the index with the thumb"),
                Step(4.5f, GardenDemoPose.ThumbFree, "Movement active"),
                Step(5.8f, GardenDemoPose.ThumbFree, "Swipe forward to step", thumbRotation: new Vector3(0, 0, 18)),
                Step(7.0f, GardenDemoPose.ThumbFree, "Swipe forward to step", thumbRotation: new Vector3(0, 0, -18)),
                Step(8.3f, GardenDemoPose.ThumbFree, "Swipe back to step back", thumbRotation: new Vector3(0, 0, -18)),
                Step(9.5f, GardenDemoPose.ThumbFree, "Swipe back to step back", thumbRotation: new Vector3(0, 0, 18)),
                Step(10.5f, GardenDemoPose.Point, "Straighten the index to exit") },
            "move" => new[] { Step(0, GardenDemoPose.Open, "Move the joystick", press: 1),
                Step(3f, GardenDemoPose.Open, "Release to stop") },
            "grab" => new[] {
                Step(0, GardenDemoPose.Open, "Move close to the object"),
                Step(2.3f, GardenDemoPose.Pinch, hands ? "Pinch and hold" : "Hold the grip button", press: 1),
                Step(4.2f, GardenDemoPose.Pinch, "Keep holding while moving", new Vector3(0.08f, 0.05f, 0), press: 1),
                Step(6.5f, GardenDemoPose.Open, "Release to let go", new Vector3(0.08f, 0.05f, 0)) },
            "calendar" when hands => new[] {
                Step(0, GardenDemoPose.Open, "Left hand away from objects", new Vector3(-0.08f, 0, 0)),
                Step(3.4f, GardenDemoPose.Pinch, "Pinch", new Vector3(-0.08f, 0, 0)),
                Step(6f, GardenDemoPose.Open, "Release", new Vector3(-0.08f, 0, 0)) },
            "calendar" => new[] { Step(0, GardenDemoPose.Open, "Left controller: Y"),
                Step(1.2f, GardenDemoPose.Open, "Press Y", press: 1), Step(3f, GardenDemoPose.Open, "Release Y") },
            _ => new[] { Step(0, GardenDemoPose.Point, hands ? "Point at the button" : "Point the controller"),
                Step(2.5f, GardenDemoPose.Pinch, hands ? "Pinch" : "Press the index trigger", press: 1),
                Step(5f, GardenDemoPose.Point, "Release to select") }
        };
    }

    private static void WriteInventory(GronnyAudioCatalogue catalogue)
    {
        Directory.CreateDirectory("Docs");
        var report = new StringBuilder("# Inventario de audio de Gronny\n\n");
        report.AppendLine("Archivo original | Duración (s) | Modo | Demostración | SHA-256\n---|---:|---|---|---");
        using var sha = SHA256.Create();
        foreach (var cue in catalogue.cues)
        {
            string path = AssetDatabase.GetAssetPath(cue.clip);
            string hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
            report.AppendLine($"`{path}` | {cue.clip.length:0.00} | {cue.mode} | {cue.demo} | `{hash}`");
        }
        File.WriteAllText("Docs/GronnyAudioInventory.md", report.ToString());
    }
}
