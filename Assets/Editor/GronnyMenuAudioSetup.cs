using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Adds the recorded menu cues without replacing existing Inspector edits or audio assets.</summary>
public static class GronnyMenuAudioSetup
{
    private const string CataloguePath = "Assets/Resources/GronnyAudio/Tutorial.asset";
    private const string Controller = "Packages/com.meta.xr.sdk.core/Meshes/MetaQuestTouchPlus/MetaQuestTouchPlus_Right.fbx";

    [Serializable] private sealed class Entry { public string id = ""; public string text = ""; }
    [Serializable] private sealed class Script { public Entry[] entries = Array.Empty<Entry>(); }

    [MenuItem("Garden/Gronny/Connect menu and accessibility audio")]
    public static void Connect()
    {
        var catalogue = AssetDatabase.LoadAssetAtPath<GronnyAudioCatalogue>(CataloguePath);
        if (catalogue == null) throw new InvalidOperationException("Create the original Gronny catalogue first.");
        AddCues(catalogue);
        EditorUtility.SetDirty(catalogue);
        AssetDatabase.SaveAssetIfDirty(catalogue);
    }

    public static void AddCues(GronnyAudioCatalogue catalogue)
    {
        string json = File.ReadAllText("Docs/GronnyMenuYAccesibilidad.json");
        Entry[] entries = JsonUtility.FromJson<Script>("{\"entries\":" + json + "}").entries;
        var cues = catalogue.cues.ToList();
        foreach (Entry entry in entries)
        {
            if (catalogue.Find(entry.id) != null) continue;
            string baseId = entry.id.EndsWith("_accessible") ? entry.id[..^11]
                : entry.id.EndsWith("_left") ? entry.id[..^5]
                : entry.id.EndsWith("_right") ? entry.id[..^6] : null;
            GronnyAudioCatalogue.Cue template = baseId != null ? catalogue.Find(baseId) : null;
            if (baseId != null && template == null)
                throw new InvalidOperationException("Missing original cue: " + baseId);
            var cue = template != null
                ? JsonUtility.FromJson<GronnyAudioCatalogue.Cue>(JsonUtility.ToJson(template))
                : new GronnyAudioCatalogue.Cue { cooldown = 0f };
            cue.id = entry.id;
            cue.instruction = cue.instructionDetail = "";
            cue.transcript = entry.text;
            cue.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/menu_accessibility/" + entry.id + ".wav");
            if (cue.clip == null) throw new InvalidOperationException("Missing recorded audio: " + entry.id);
            cue.rightHand = entry.id.EndsWith("_right");
            if (template?.clip != null && template.clip.length > 0f)
                foreach (var step in cue.steps) step.at *= cue.clip.length / template.clip.length;
            if (cue.rightHand && cue.mode == GardenInputMode.Hands)
                foreach (var step in cue.steps) step.label = step.label?.Replace("Left", "Right").Replace("left", "right");
            if (cue.rightHand && cue.mode == GardenInputMode.Controllers)
            {
                bool movement = cue.demo == GardenControlDemo.Move;
                string animation = "right_quest_touch_plus_controller_" + (movement ? "stickN" : "button02");
                cue.controllerAnimation = AssetDatabase.LoadAllAssetsAtPath(Controller)
                    .OfType<AnimationClip>().Single(clip => clip.name == animation);
                cue.controllerTarget = AnimationUtility.GetCurveBindings(cue.controllerAnimation)
                    .First(binding => !string.IsNullOrEmpty(binding.path)).path;
                if (!movement)
                    foreach (var step in cue.steps) step.label = step.label?.Replace("Left", "Right").Replace("Y", "B");
            }
            cues.Add(cue);
        }
        catalogue.cues = cues.ToArray();
        GronnyAudioSetup.ApplyInstructionCaptions(catalogue);
    }
}
