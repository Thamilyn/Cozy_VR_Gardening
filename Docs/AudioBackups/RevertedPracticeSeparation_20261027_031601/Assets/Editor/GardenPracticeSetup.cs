using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>Creates an independent practice scene with the already configured Quest rig and table.</summary>
public static class GardenPracticeSetup
{
    public const string PracticePath = "Assets/Scenes/Garden_Practice.unity";
    public const string GardenPath = "Assets/Scenes/Garden_Moves.unity";

    [MenuItem("Garden/Create practice scene")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Create the practice scene outside Play.");
        if (EditorSceneManager.GetSceneManagerSetup().Any(scene => scene.isLoaded &&
            UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scene.path).isDirty))
            throw new InvalidOperationException("Save pending scene edits before creating practice.");
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PracticePath) == null &&
            !AssetDatabase.CopyAsset(GardenPath, PracticePath))
            throw new InvalidOperationException("Could not create the practice scene from the existing garden.");
        var scenes = new List<EditorBuildSettingsScene>
        {
            new(PracticePath, true), new(GardenPath, true)
        };
        scenes.AddRange(EditorBuildSettings.scenes.Where(scene => scene.path != PracticePath && scene.path != GardenPath));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
    }
}
