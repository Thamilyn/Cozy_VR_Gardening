using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Restores the teacher's harvest Animator, Humanoid clip settings and scene references.</summary>
internal static class GardenerMixamoSetup
{
    private const string ScenePath = "Assets/Scenes/Garden_Moves.unity";
    private const string ControllerName = "Gardener Kawaii Greeting";
    private const string IdlePath = "Assets/Vroid/Animations/Happy Idle.fbx";
    private const string ClapPath = "Assets/Vroid/Animations/Clapping.fbx";
    private const string AnimatorPath = "Assets/Vroid/Animations/GronnyHarvest.controller";
    private static readonly string[] AnimationPaths =
    {
        ClapPath,
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
            var paths = new string[AnimationPaths.Length + 1];
            paths[0] = IdlePath;
            AnimationPaths.CopyTo(paths, 1);
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
                    bool loop = path != ClapPath;
                    if (clip.loopTime != loop) needsUpdate = true;
                    clip.loopTime = loop;
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

            var harvestAnimator = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorPath);
            if (harvestAnimator == null)
            {
                Debug.LogError("Gardener Animator Controller is missing: " + AnimatorPath);
                return;
            }
            var states = harvestAnimator.layers[0].stateMachine.states;
            foreach (var child in states)
            {
                string path = child.state.name == "Happy Idle" ? IdlePath :
                    child.state.name == "Clapping" ? ClapPath :
                    child.state.name == "Standing Greeting" ? AnimationPaths[1] : null;
                var clip = path != null ? FindClip(path) : null;
                if (clip == null)
                {
                    Debug.LogError("Gardener Animator clip is missing for state: " + child.state.name);
                    return;
                }
                child.state.motion = clip;
            }
            EditorUtility.SetDirty(harvestAnimator);
            AssetDatabase.SaveAssets();

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
                var animatorProperty = serialized.FindProperty("animationController");
                if (animatorProperty.objectReferenceValue == harvestAnimator) return;
                animatorProperty.objectReferenceValue = harvestAnimator;
                serialized.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Gardener Happy Idle, gaze greeting and one-shot Clapping are configured in Garden_Moves.");
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
