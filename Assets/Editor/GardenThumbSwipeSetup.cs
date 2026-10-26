using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Authors a short thumb-pad stroke against the curled Meta index, without editing SDK assets.</summary>
public static class GardenThumbSwipeSetup
{
    private const string Folder = "Assets/Resources/GronnyAudio";

    [MenuItem("Garden/Gronny/Add visible thumb swipe")]
    public static void Configure()
    {
        var catalogue = AssetDatabase.LoadAssetAtPath<GronnyAudioCatalogue>(Folder + "/Tutorial.asset");
        if (catalogue == null) throw new InvalidOperationException("Existing Gronny catalogue is missing.");
        var cue = catalogue.Find("controls_move_hands");
        var forward = cue.steps.Where(s => s.label == "Swipe forward to step").ToArray();
        var backward = cue.steps.Where(s => s.label == "Swipe back to step back").ToArray();
        if (forward.Length != 2 || backward.Length != 2)
            throw new InvalidOperationException("Review the authored movement steps before adding swipe endpoints.");
        catalogue.leftHand.thumbSwipe = CreateClip(catalogue.leftHand, "Left");
        catalogue.rightHand.thumbSwipe = CreateClip(catalogue.rightHand, "Right");
        forward[0].thumbSwipeProgress = backward[1].thumbSwipeProgress = 0f;
        forward[1].thumbSwipeProgress = backward[0].thumbSwipeProgress = 1f;
        foreach (var step in forward.Concat(backward))
        {
            step.thumbSwipeWeight = 1f;
            step.thumbRotation = Vector3.zero;
        }
        EditorUtility.SetDirty(catalogue);
        AssetDatabase.SaveAssetIfDirty(catalogue);
        Debug.Log("Gronny thumb swipe connected; voice references and authored step times preserved.");
    }

    private static AnimationClip CreateClip(GronnyAudioCatalogue.HandVisual hand, string side)
    {
        string path = Folder + "/ThumbSwipe" + side + ".anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        var clip = new AnimationClip { name = "ThumbSwipe" + side, frameRate = 60f };
        GameObject preview = UnityEngine.Object.Instantiate(hand.prefab);
        preview.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            foreach (MonoBehaviour script in preview.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            Transform root = preview.transform.Find(hand.animationRoot);
            Transform[] bones = root.GetComponentsInChildren<Transform>(true);
            string prefix = side == "Left" ? "b_l_" : "b_r_";
            Transform index1 = bones.Single(t => t.name == prefix + "index1");
            Transform index2 = bones.Single(t => t.name == prefix + "index2");
            Transform index3 = bones.Single(t => t.name == prefix + "index3");
            Transform pad = bones.Single(t => t.name.EndsWith("thumb_finger_pad_marker"));
            Transform[] thumb = Enumerable.Range(0, 4).Select(i => bones.Single(t => t.name == prefix + "thumb" + i)).ToArray();
            hand.thumbFree.SampleAnimation(root.gameObject, 0f);
            Vector3 freePad = pad.position;
            Quaternion[] curledIndex = bones.Select(t => t.localRotation).ToArray();
            hand.curled.SampleAnimation(root.gameObject, 0f);
            for (int i = 0; i < bones.Length; i++)
                if (!bones[i].name.Contains("_thumb")) bones[i].localRotation = curledIndex[i];
            Quaternion[] seed = thumb.Select(t => t.localRotation).ToArray();
            Vector3 normal = Vector3.Cross(index2.position - index1.position, index3.position - index2.position).normalized;
            if (Vector3.Dot(normal, freePad - index2.position) < 0f) normal = -normal;
            var samples = new Quaternion[thumb.Length, 46];
            for (int frame = 0; frame < 46; frame++)
            {
                for (int j = 0; j < thumb.Length; j++) thumb[j].localRotation = seed[j];
                // Slide along the side of the proximal index, with the thumb pad at its
                // surface. The former example opened the thumb by nearly seven centimetres.
                float alongIndex = Mathf.Lerp(0.35f, 0.8f, frame / 45f);
                Vector3 target = Vector3.Lerp(index1.position, index2.position, alongIndex) + normal * 0.009f;
                for (int iteration = 0; iteration < 48; iteration++)
                {
                    for (int j = 2; j >= 0; j--)
                    {
                        Quaternion correction = Quaternion.FromToRotation(pad.position - thumb[j].position, target - thumb[j].position);
                        thumb[j].rotation = Quaternion.Slerp(Quaternion.identity, correction, 0.65f) * thumb[j].rotation;
                    }
                    if (Vector3.Distance(pad.position, target) < 0.00015f) break;
                }
                if (Vector3.Distance(pad.position, target) > 0.002f)
                    throw new InvalidOperationException(side + " thumb cannot reach the index surface at frame " + frame);
                for (int j = 0; j < thumb.Length; j++) samples[j, frame] = thumb[j].localRotation;
            }
            for (int j = 0; j < thumb.Length; j++)
            {
                for (int axis = 0; axis < 4; axis++)
                {
                    var keys = new Keyframe[46];
                    for (int frame = 0; frame < keys.Length; frame++)
                        keys[frame] = new Keyframe(0.75f * frame / 45f, samples[j, frame][axis]);
                    var curve = new AnimationCurve(keys);
                    for (int frame = 0; frame < keys.Length; frame++)
                    {
                        AnimationUtility.SetKeyLeftTangentMode(curve, frame, AnimationUtility.TangentMode.ClampedAuto);
                        AnimationUtility.SetKeyRightTangentMode(curve, frame, AnimationUtility.TangentMode.ClampedAuto);
                    }
                    var binding = EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(thumb[j], root),
                        typeof(Transform), "m_LocalRotation." + "xyzw"[axis]);
                    AnimationUtility.SetEditorCurve(clip, binding, curve);
                }
            }
            clip.EnsureQuaternionContinuity();
            if (existing == null) { AssetDatabase.CreateAsset(clip, path); return clip; }
            EditorUtility.CopySerialized(clip, existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            UnityEngine.Object.DestroyImmediate(clip);
            return existing;
        }
        finally { UnityEngine.Object.DestroyImmediate(preview); }
    }
}
