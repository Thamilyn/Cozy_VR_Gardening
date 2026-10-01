using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Restores the teacher's default Humanoid clips and scene references.</summary>
internal static class GardenerMixamoSetup
{
    private const string ScenePath = "Assets/Scenes/Garden_Moves.unity";
    private const string ControllerName = "Gardener Kawaii Greeting";
    private const string IdlePath = "Assets/Vroid/Animations/Idle (1).fbx";
    private static readonly string[] GreetingPaths =
    {
        "Assets/Vroid/Animations/Quick Formal Bow.fbx",
        "Assets/Vroid/Animations/Standing Greeting.fbx"
    };
    private static bool configuring;

    [MenuItem("Garden/Restore Gardener default greetings")]
    private static void Configure()
    {
        if (configuring || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        configuring = true;
        try
        {
            var paths = new string[GreetingPaths.Length + 1];
            paths[0] = IdlePath;
            GreetingPaths.CopyTo(paths, 1);
            foreach (string path in paths)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogError("Gardener animation FBX is missing: " + path);
                    return;
                }

                var clips = importer.clipAnimations;
                bool needsUpdate = importer.animationType != ModelImporterAnimationType.Human ||
                    clips == null || clips.Length == 0;
                if (clips == null || clips.Length == 0)
                    clips = importer.defaultClipAnimations;
                if (clips == null || clips.Length == 0)
                {
                    Debug.LogError("Gardener animation FBX has no clips: " + path);
                    return;
                }

                foreach (var clip in clips)
                {
                    if (!clip.loopTime) needsUpdate = true;
                    clip.loopTime = true;
                    string name = Path.GetFileNameWithoutExtension(path);
                    if (clip.name != name) needsUpdate = true;
                    clip.name = name;
                }

                if (needsUpdate)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                    EditorApplication.delayCall += Configure;
                    return;
                }
            }

            var idle = FindClip(IdlePath);
            if (idle == null)
            {
                Debug.LogError("Gardener idle clip could not be loaded: " + IdlePath);
                return;
            }
            var greetings = new AnimationClip[GreetingPaths.Length];
            for (int i = 0; i < greetings.Length; i++)
            {
                greetings[i] = FindClip(GreetingPaths[i]);
                if (greetings[i] == null)
                {
                    Debug.LogError("Gardener greeting clip could not be loaded: " + GreetingPaths[i]);
                    return;
                }
            }
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            try
            {
                if (openedHere)
                    scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

                GardenerMixamoGreeting controller = null;
                foreach (var root in scene.GetRootGameObjects())
                    if (root.name == ControllerName)
                        controller = root.GetComponent<GardenerMixamoGreeting>();
                if (controller == null)
                {
                    Debug.LogError("Gardener greeting controller is missing from " + ScenePath);
                    return;
                }

                var serialized = new SerializedObject(controller);
                var idleProperty = serialized.FindProperty("idleClip");
                var greetingProperty = serialized.FindProperty("greetingClips");
                bool changed = idleProperty.objectReferenceValue != idle ||
                    greetingProperty.arraySize != greetings.Length;
                if (!changed)
                    for (int i = 0; i < greetings.Length; i++)
                        if (greetingProperty.GetArrayElementAtIndex(i).objectReferenceValue != greetings[i])
                            changed = true;
                if (!changed) return;

                idleProperty.objectReferenceValue = idle;
                greetingProperty.arraySize = greetings.Length;
                for (int i = 0; i < greetings.Length; i++)
                    greetingProperty.GetArrayElementAtIndex(i).objectReferenceValue = greetings[i];
                serialized.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Gardener Kawaii FBX animation loop is configured in Garden_Moves.");
            }
            finally
            {
                if (openedHere && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
        finally
        {
            configuring = false;
        }
    }

    private static AnimationClip FindClip(string path)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        return null;
    }

}
