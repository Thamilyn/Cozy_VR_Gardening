using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Connects the supplied water-saving lesson without changing Meta assets.</summary>
public static class GronnyWaterSavingAudioSetup
{
    [Serializable] private sealed class Line { public string id = ""; public string cueId = ""; public string text = ""; }
    [Serializable] private sealed class Script { public Line[] entries = Array.Empty<Line>(); }
    [Serializable] private sealed class Import
    {
        public FileRecord[] files = Array.Empty<FileRecord>();
        public string welcome_sha256 = "";
    }
    [Serializable] private sealed class FileRecord
    {
        public string id = "";
        public string backup = "";
        public string source_sha256 = "";
        public string override_sha256 = "";
        public string previous_revision_backup = "";
    }

    [MenuItem("Garden/Gronny/Connect water-saving lesson")]
    public static void Connect()
    {
        var catalogue = AssetDatabase.LoadAssetAtPath<GronnyAudioCatalogue>("Assets/Resources/GronnyAudio/Tutorial.asset");
        if (catalogue == null) throw new InvalidOperationException("The existing Gronny catalogue is missing.");
        Apply(catalogue);
        EditorUtility.SetDirty(catalogue);
        AssetDatabase.SaveAssetIfDirty(catalogue);
        GronnyAudioSetup.WriteInventory(catalogue);
    }

    [MenuItem("Garden/Gronny/Connect water warning variations")]
    public static void ConnectWarningVariations() =>
        ConnectVariations("Docs/GronnyWaterWarningVariations.json", "Connect Gronny water warning variations");

    [MenuItem("Garden/Gronny/Connect completed watering variations")]
    public static void ConnectCompletedWateringVariations() =>
        ConnectVariations("Docs/GronnyCompletedWateringVariations.json", "Connect Gronny completed watering variations");

    private static void ConnectVariations(string scriptPath, string undoLabel)
    {
        var catalogue = AssetDatabase.LoadAssetAtPath<GronnyAudioCatalogue>("Assets/Resources/GronnyAudio/Tutorial.asset");
        if (catalogue == null) throw new InvalidOperationException("The existing Gronny catalogue is missing.");
        var script = JsonUtility.FromJson<Script>("{\"entries\":" + File.ReadAllText(scriptPath) + "}");
        if (script.entries.Length == 0 || script.entries.Select(line => line.id).Distinct().Count() != script.entries.Length)
            throw new InvalidOperationException("Voice variations need a non-empty script with unique IDs.");
        var assignments = new Dictionary<GronnyAudioCatalogue.Cue, GronnyAudioCatalogue.VoiceVariation[]>();
        foreach (var group in script.entries.GroupBy(line => line.cueId))
        {
            var cue = catalogue.Find(group.Key);
            if (cue == null) throw new InvalidOperationException("Missing voice cue: " + group.Key);
            assignments.Add(cue, group.Select(line =>
            {
                if (string.IsNullOrWhiteSpace(line.text)) throw new InvalidOperationException("Missing transcript: " + line.id);
                var clip = LoadClip(line.id);
                if (clip == cue.clip || clip.length <= 0f || cue.steps.Any(step => step.at >= clip.length))
                    throw new InvalidOperationException("Voice variation must be a new recording long enough for its demonstration: " + line.id);
                return new GronnyAudioCatalogue.VoiceVariation { clip = clip, transcript = line.text };
            }).ToArray());
        }
        // Validate the complete batch before changing any catalogue references.
        Undo.RecordObject(catalogue, undoLabel);
        foreach (var assignment in assignments) assignment.Key.variations = assignment.Value;
        EditorUtility.SetDirty(catalogue);
        AssetDatabase.SaveAssetIfDirty(catalogue);
        GronnyAudioSetup.WriteInventory(catalogue);
    }

    public static void Apply(GronnyAudioCatalogue catalogue)
    {
        const string scriptPath = "Docs/GronnyWaterSavingDialogues.json";
        if (!File.Exists(scriptPath)) return;
        var script = JsonUtility.FromJson<Script>("{\"entries\":" + File.ReadAllText(scriptPath) + "}");
        var import = JsonUtility.FromJson<Import>(File.ReadAllText("Docs/WaterSavingAudioImport.json"));
        var cues = catalogue.cues.ToList();
        bool retime = catalogue.waterSavingAudioVersion == 0;
        // Update original cues first: new watering/refill demonstrations clone their timings.
        foreach (Line line in script.entries.Where(line => catalogue.Find(line.id) != null))
        {
            var cue = catalogue.Find(line.id);
            var clip = LoadClip(line.id);
            FileRecord record = import.files.First(file => file.id == line.id);
            bool retimeSeed = catalogue.waterSavingAudioVersion < 2 && !string.IsNullOrEmpty(record.override_sha256);
            bool retimeMovement = catalogue.waterSavingAudioVersion < 3 &&
                (line.id == "controls_move_hands" || line.id == "controls_move_controllers") &&
                !string.IsNullOrEmpty(record.override_sha256);
            if (retime || retimeSeed || retimeMovement)
            {
                string backup = retime ? record.backup : record.previous_revision_backup;
                if (!File.Exists(backup))
                    backup = Path.Combine("Docs/AudioBackups", Path.GetFileName(Path.GetDirectoryName(backup)), Path.GetFileName(backup));
                float oldDuration = ReadWaveDuration(backup);
                foreach (var step in cue.steps) step.at *= clip.length / oldDuration;
            }
            cue.clip = clip;
            cue.transcript = line.text;
        }
        foreach (Line line in script.entries.Where(line => catalogue.Find(line.id) == null))
        {
            GronnyAudioCatalogue.Cue cue;
            if (line.id.StartsWith("controls_watering_") || line.id.StartsWith("controls_refill_select_"))
            {
                bool hands = line.id.EndsWith("_hands");
                bool watering = line.id.StartsWith("controls_watering_");
                var template = catalogue.Find("controls_" + (watering ? "grab" : "calendar_select") + (hands ? "_hands" : "_controllers"));
                cue = JsonUtility.FromJson<GronnyAudioCatalogue.Cue>(JsonUtility.ToJson(template));
                var clip = LoadClip(line.id);
                foreach (var step in cue.steps) step.at *= clip.length / template.clip.length;
                if (watering)
                {
                    cue.demo = GardenControlDemo.WateringGrab;
                    cue.steps = new[]
                    {
                        Step(0f, GardenDemoPose.Open, hands ? "Move your open hand close" : "Move the controller close"),
                        Step(clip.length * 0.3f, GardenDemoPose.Curled, hands ? "Close your fingers around the can" : "Hold Grip", 1f),
                        Step(clip.length * 0.7f, GardenDemoPose.Curled, hands ? "Keep your fingers closed" : "Keep holding Grip", 1f)
                    };
                }
            }
            else cue = new GronnyAudioCatalogue.Cue { cooldown = line.id.StartsWith("help_") ? 12f : 0f };
            cue.id = line.id;
            cue.instruction = cue.instructionDetail = "";
            cue.clip = LoadClip(line.id);
            cue.transcript = line.text;
            cue.assisted = line.id.StartsWith("help_") || line.id.StartsWith("controls_");
            cue.priority = line.id == "water_supply_exhausted" ? 100 : line.id.StartsWith("help_") ? 20 : 0;
            cues.Add(cue);
        }
        catalogue.cues = cues.ToArray();
        if (retime)
        {
            // The replacement first-watering recording starts at tilting, after the separate grip cue.
            var watering = catalogue.Find("04_first_watering");
            watering.steps = new[]
            {
                new GronnyAudioCatalogue.Step { at = 0f, pose = GardenDemoPose.Curled,
                    rotation = new Vector3(0f, 0f, -65f), label = "Tilt over the soil, near the roots" },
                Step(watering.clip.length * 0.6f, GardenDemoPose.Curled, "Turn upright to stop pouring")
            };
        }
        catalogue.waterSavingAudioVersion = Mathf.Max(catalogue.waterSavingAudioVersion,
            import.files.Any(file => file.id == "controls_move_hands" && !string.IsNullOrEmpty(file.override_sha256)) ? 3 :
            import.files.Any(file => !string.IsNullOrEmpty(file.override_sha256)) ? 2 : 1);
        GronnyAudioSetup.ApplyInstructionCaptions(catalogue);
    }

    private static GronnyAudioCatalogue.Step Step(float at, GardenDemoPose pose, string caption, float press = 0f) =>
        new() { at = at, pose = pose, label = caption, press = press };

    public static string VerifyImportedFiles(GronnyAudioCatalogue catalogue)
    {
        var import = JsonUtility.FromJson<Import>(File.ReadAllText("Docs/WaterSavingAudioImport.json"));
        using var hash = System.Security.Cryptography.SHA256.Create();
        string Digest(string path) => BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
        if (Digest(AssetDatabase.GetAssetPath(catalogue.Find("01_welcome").clip)) != import.welcome_sha256)
            throw new InvalidOperationException("The welcome must match the latest approved recording.");
        if (import.files.Length != 45) throw new InvalidOperationException("Expected 45 lesson recordings, including the welcome and movement feedback.");
        foreach (var file in import.files)
            if (Digest(AssetDatabase.GetAssetPath(catalogue.Find(file.id).clip)) !=
                (string.IsNullOrEmpty(file.override_sha256) ? file.source_sha256 : file.override_sha256))
                throw new InvalidOperationException("Recording does not match the supplied source: " + file.id);
        return "PASS: 45 lesson WAVs match their source or approved replacement, including the welcome and movement feedback.";
    }

    private static AudioClip LoadClip(string id)
    {
        string[] matches = AssetDatabase.FindAssets(id + " t:AudioClip", new[] { "Assets/Audio" })
            .Select(AssetDatabase.GUIDToAssetPath).Where(path => Path.GetFileNameWithoutExtension(path) == id).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException("Expected exactly one supplied audio: " + id);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(matches[0]);
    }

    private static float ReadWaveDuration(string path)
    {
        using var reader = new BinaryReader(File.OpenRead(path));
        reader.BaseStream.Position = 12;
        uint bytesPerSecond = 0, dataBytes = 0;
        while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
        {
            string chunk = new(reader.ReadChars(4));
            uint size = reader.ReadUInt32();
            long start = reader.BaseStream.Position;
            if (chunk == "fmt ") { reader.BaseStream.Position = start + 8; bytesPerSecond = reader.ReadUInt32(); }
            if (chunk == "data") dataBytes = size;
            if (bytesPerSecond > 0 && dataBytes > 0) return (float)dataBytes / bytesPerSecond;
            reader.BaseStream.Position = start + size + size % 2;
        }
        throw new InvalidOperationException("Cannot read previous WAV duration: " + path);
    }
}
