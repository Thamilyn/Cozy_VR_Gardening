using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

public static class ConnectSoilVoice
{
    private sealed class Line { public string id = ""; public string text = ""; }

    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Connect recordings outside Play Mode.");
        var catalogue = AssetDatabase.LoadAssetAtPath<GronnyAudioCatalogue>("Assets/Resources/GronnyAudio/Tutorial.asset");
        var lines = Newtonsoft.Json.JsonConvert.DeserializeObject<Line[]>(File.ReadAllText("Docs/GronnySoilDialogues.json"));
        if (catalogue == null || lines == null || lines.Length != 3)
            throw new InvalidOperationException("Missing soil voice catalogue or incomplete three-cue script.");
        var template = (AudioImporter)AssetImporter.GetAtPath("Assets/Audio/dialogues_cozy_gardening/13_complete.wav");
        if (template == null)
            throw new InvalidOperationException("Missing existing Gronny voice import settings.");
        foreach (var line in lines)
        {
            string source = "output/GronnySoilVoice_20261108/" + line.id + ".wav";
            string destination = "Assets/Audio/dialogues_cozy_gardening/" + line.id + ".wav";
            if (catalogue.Find(line.id) == null || !File.Exists(source))
                throw new InvalidOperationException("Missing cue or generated recording: " + line.id);
            if (File.Exists(destination) && Hash(source) != Hash(destination))
                throw new InvalidOperationException("Preserve the existing different recording: " + destination);
            var existingClip = catalogue.Find(line.id).clip;
            if (existingClip != null && AssetDatabase.GetAssetPath(existingClip) != destination)
                throw new InvalidOperationException("Preserve the existing assigned voice: " + line.id);
        }
        foreach (var line in lines)
        {
            string source = "output/GronnySoilVoice_20261108/" + line.id + ".wav";
            string destination = "Assets/Audio/dialogues_cozy_gardening/" + line.id + ".wav";
            if (!File.Exists(destination)) File.Copy(source, destination);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
            var importer = (AudioImporter)AssetImporter.GetAtPath(destination);
            importer.defaultSampleSettings = template.defaultSampleSettings;
            importer.forceToMono = template.forceToMono;
            importer.loadInBackground = template.loadInBackground;
            importer.SaveAndReimport();
        }
        // Reimports can reload the catalogue. Assign every reference only once all
        // imports have finished, so a later import cannot discard an earlier cue.
        catalogue = AssetDatabase.LoadAssetAtPath<GronnyAudioCatalogue>("Assets/Resources/GronnyAudio/Tutorial.asset");
        var report = new List<object>();
        foreach (var line in lines)
        {
            string destination = "Assets/Audio/dialogues_cozy_gardening/" + line.id + ".wav";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(destination);
            if (clip == null || clip.length <= 0f || clip.channels != 1)
                throw new InvalidOperationException("Invalid imported voice: " + line.id);
            var cue = catalogue.Find(line.id);
            cue.clip = clip;
            cue.transcript = line.text;
            report.Add(new { id = line.id, path = destination, guid = AssetDatabase.AssetPathToGUID(destination),
                seconds = clip.length, sampleRate = clip.frequency, channels = clip.channels, sha256 = Hash(destination) });
        }
        EditorUtility.SetDirty(catalogue);
        AssetDatabase.SaveAssetIfDirty(catalogue);
        return new { connected = report, total = report.Count };
    }

    private static string Hash(string path)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
    }
}
