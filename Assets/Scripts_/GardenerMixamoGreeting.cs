using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UniVRM10;

/// <summary>Cycles through looping Humanoid FBX motions when the player looks at the teacher.</summary>
[DisallowMultipleComponent]
public sealed class GardenerMixamoGreeting : MonoBehaviour
{
    private const string TeacherName = "Gardener Kawaii - Profesor de jardineria";
    private const string EyeAnchorName = "CenterEyeAnchor";

    [Header("Mixamo clips (Humanoid)")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip[] greetingClips = new AnimationClip[2];

    [Header("Gaze")]
    [SerializeField, Range(3f, 30f)] private float gazeAngleDegrees = 12f;
    [SerializeField, Min(0.1f), Tooltip("Seconds the player must look at the teacher before a greeting starts.")]
    private float gazeDuration = 0.8f;
    [SerializeField, Min(0.5f)] private float maximumDistance = 4f;
    [SerializeField, Min(0.1f), Tooltip("Seconds looking away before another greeting can be triggered.")]
    private float lookAwayToReset = 0.5f;
    [SerializeField, Min(0.01f)] private float crossFadeDuration = 0.25f;

    [Header("Parpadeo VRM")]
    [SerializeField, Min(0.1f), Tooltip("Tiempo minimo entre parpadeos, en segundos.")]
    private float minimumBlinkInterval = 2.5f;
    [SerializeField, Min(0.1f), Tooltip("Tiempo maximo entre parpadeos, en segundos.")]
    private float maximumBlinkInterval = 5.5f;
    [SerializeField, Min(0.01f)] private float blinkCloseDuration = 0.08f;
    [SerializeField, Min(0f)] private float blinkHoldDuration = 0.03f;
    [SerializeField, Min(0.01f)] private float blinkOpenDuration = 0.14f;

    private readonly RaycastHit[] occlusionHits = new RaycastHit[16];
    private Transform teacher;
    private Transform viewer;
    private Transform head;
    private Animator animator;
    private Vrm10Instance vrmInstance;
    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private AnimationClipPlayable greetingPlayable;
    private float gazeHeld;
    private float gazeLost;
    private float greetingWeight;
    private float targetGreetingWeight;
    private int lastGreetingIndex = -1;
    private bool gazeArmed = true;
    private bool originalApplyRootMotion;
    private bool rootMotionChanged;
    private float nextBlinkAt;
    private float blinkStartedAt = -1f;

    private void Start()
    {
        var teacherObject = GameObject.Find(TeacherName);
        var eyeObject = GameObject.Find(EyeAnchorName);
        teacher = teacherObject != null ? teacherObject.transform : null;
        viewer = eyeObject != null ? eyeObject.transform : Camera.main != null ? Camera.main.transform : null;
        animator = teacher != null ? teacher.GetComponentInChildren<Animator>(true) : null;
        vrmInstance = teacher != null ? teacher.GetComponentInChildren<Vrm10Instance>(true) : null;
        if (vrmInstance != null && vrmInstance.Vrm != null &&
            vrmInstance.Vrm.Expression != null && vrmInstance.Vrm.Expression.Blink != null)
            ScheduleNextBlink(Time.time);
        else
            vrmInstance = null;

        if (animator == null || animator.avatar == null || !animator.avatar.isHuman ||
            idleClip == null || greetingClips == null || greetingClips.Length == 0)
        {
            Debug.LogWarning("Gardener greeting needs a humanoid VRM, one idle clip and at least one Mixamo greeting.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < greetingClips.Length; i++)
        {
            if (greetingClips[i] != null) continue;
            Debug.LogWarning("Gardener greeting is missing a Mixamo clip at index " + i + ".", this);
            enabled = false;
            return;
        }

        head = animator.GetBoneTransform(HumanBodyBones.Head);
        if (head == null)
        {
            Debug.LogWarning("Gardener greeting could not find the teacher's head bone.", this);
            enabled = false;
            return;
        }

        originalApplyRootMotion = animator.applyRootMotion;
        animator.applyRootMotion = false;
        rootMotionChanged = true;
        graph = PlayableGraph.Create("Gardener Kawaii Mixamo Greetings");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        mixer = AnimationMixerPlayable.Create(graph, 2);
        var idlePlayable = AnimationClipPlayable.Create(graph, idleClip);
        idlePlayable.SetApplyFootIK(true);
        graph.Connect(idlePlayable, 0, mixer, 0);
        mixer.SetInputWeight(0, 1f);
        mixer.SetInputWeight(1, 0f);
        var output = AnimationPlayableOutput.Create(graph, "Gardener animation", animator);
        output.SetSourcePlayable(mixer);
        graph.Play();
    }

    private void Update()
    {
        UpdateBlink();
        if (!graph.IsValid()) return;
        if (viewer == null && Camera.main != null) viewer = Camera.main.transform;

        bool looking = viewer != null && IsLookingAtTeacher();
        if (looking)
        {
            gazeLost = 0f;
            gazeHeld += Time.deltaTime;
            if (gazeArmed && gazeHeld >= gazeDuration)
            {
                PlayNextGreeting();
                gazeArmed = false;
            }
        }
        else
        {
            gazeHeld = 0f;
            gazeLost += Time.deltaTime;
            if (gazeLost >= lookAwayToReset)
            {
                gazeArmed = true;
                targetGreetingWeight = 0f;
            }
        }

        greetingWeight = Mathf.MoveTowards(greetingWeight, targetGreetingWeight,
            Time.deltaTime / crossFadeDuration);
        mixer.SetInputWeight(0, 1f - greetingWeight);
        mixer.SetInputWeight(1, greetingWeight);
    }

    private void ScheduleNextBlink(float now)
    {
        nextBlinkAt = now + Random.Range(minimumBlinkInterval,
            Mathf.Max(minimumBlinkInterval, maximumBlinkInterval));
    }

    private void UpdateBlink()
    {
        if (vrmInstance == null) return;

        float now = Time.time;
        if (blinkStartedAt < 0f)
        {
            if (now < nextBlinkAt) return;
            blinkStartedAt = now;
        }

        float elapsed = now - blinkStartedAt;
        float close = Mathf.Max(0.01f, blinkCloseDuration);
        float hold = Mathf.Max(0f, blinkHoldDuration);
        float open = Mathf.Max(0.01f, blinkOpenDuration);
        float weight;
        if (elapsed < close)
            weight = elapsed / close;
        else if (elapsed < close + hold)
            weight = 1f;
        else if (elapsed < close + hold + open)
            weight = 1f - (elapsed - close - hold) / open;
        else
        {
            weight = 0f;
            blinkStartedAt = -1f;
            ScheduleNextBlink(now);
        }

        vrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Blink, weight);
    }

    private bool IsLookingAtTeacher()
    {
        Vector3 toHead = head.position - viewer.position;
        float distance = toHead.magnitude;
        if (distance < 0.1f || distance > maximumDistance) return false;

        Vector3 direction = toHead / distance;
        if (Vector3.Dot(viewer.forward, direction) < Mathf.Cos(gazeAngleDegrees * Mathf.Deg2Rad))
            return false;

        int hitCount = Physics.RaycastNonAlloc(viewer.position, direction, occlusionHits,
            distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            Transform hit = occlusionHits[i].transform;
            if (hit != null && !hit.IsChildOf(teacher) && !hit.IsChildOf(viewer.root))
                return false;
        }
        return true;
    }

    private void PlayNextGreeting()
    {
        lastGreetingIndex = (lastGreetingIndex + 1) % greetingClips.Length;
        if (greetingPlayable.IsValid())
        {
            mixer.DisconnectInput(1);
            graph.DestroyPlayable(greetingPlayable);
        }

        greetingPlayable = AnimationClipPlayable.Create(graph, greetingClips[lastGreetingIndex]);
        greetingPlayable.SetApplyFootIK(true);
        graph.Connect(greetingPlayable, 0, mixer, 1);
        greetingPlayable.SetTime(0);
        mixer.SetInputWeight(1, greetingWeight);
        targetGreetingWeight = 1f;
    }

    [ContextMenu("Test next greeting in Play mode")]
    private void TestGreeting()
    {
        if (Application.isPlaying && graph.IsValid()) PlayNextGreeting();
    }

    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
        if (rootMotionChanged && animator != null) animator.applyRootMotion = originalApplyRootMotion;
    }
}
