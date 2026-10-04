using UnityEngine;
using UniVRM10;

/// <summary>Runs the teacher's idle, gaze greeting and one-shot harvest celebration through the Animator.</summary>
[DisallowMultipleComponent]
public sealed class GardenerMixamoGreeting : MonoBehaviour
{
    private const string TeacherName = "Gardener Kawaii - Profesor de jardineria";
    private const string EyeAnchorName = "CenterEyeAnchor";

    private static readonly int ClapHash = Animator.StringToHash("Clap");
    private static readonly int GreetHash = Animator.StringToHash("Greet");
    private static readonly int ClappingStateHash = Animator.StringToHash("Base Layer.Clapping");

    [Header("Mixamo Animator (Humanoid)")]
    [SerializeField] private RuntimeAnimatorController animationController;

    [Header("Gaze")]
    [SerializeField, Range(3f, 30f)] private float gazeAngleDegrees = 12f;
    [SerializeField, Min(0.1f), Tooltip("Seconds the player must look at the teacher before a greeting starts.")]
    private float gazeDuration = 0.8f;
    [SerializeField, Min(0.5f)] private float maximumDistance = 4f;
    [SerializeField, Min(0.1f), Tooltip("Seconds looking away before another greeting can be triggered.")]
    private float lookAwayToReset = 0.5f;

    [Header("VRM blinking")]
    [SerializeField, Min(0.1f), Tooltip("Minimum time between blinks, in seconds.")]
    private float minimumBlinkInterval = 2.5f;
    [SerializeField, Min(0.1f), Tooltip("Maximum time between blinks, in seconds.")]
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
    private RuntimeAnimatorController originalController;
    private float gazeHeld;
    private float gazeLost;
    private bool gazeArmed = true;
    private bool animatorConfigured;
    private bool clapRequested;
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
            animationController == null)
        {
            Debug.LogWarning("Gardener needs a humanoid VRM and its harvest Animator Controller.", this);
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
        originalController = animator.runtimeAnimatorController;
        animator.runtimeAnimatorController = animationController;
        if (!HasParameter(ClapHash, AnimatorControllerParameterType.Trigger) ||
            !HasParameter(GreetHash, AnimatorControllerParameterType.Bool) ||
            !animator.HasState(0, ClappingStateHash))
        {
            Debug.LogWarning("Gardener Animator needs Clap (Trigger), Greet (Bool) and a Clapping state.", this);
            animator.runtimeAnimatorController = originalController;
            enabled = false;
            return;
        }
        animatorConfigured = true;
        animator.SetBool(GreetHash, false);
        animator.ResetTrigger(ClapHash);
    }

    private bool HasParameter(int hash, AnimatorControllerParameterType type)
    {
        foreach (var parameter in animator.parameters)
            if (parameter.nameHash == hash && parameter.type == type) return true;
        return false;
    }

    private bool IsClapping()
    {
        return animator.GetCurrentAnimatorStateInfo(0).fullPathHash == ClappingStateHash ||
            (animator.IsInTransition(0) &&
             animator.GetNextAnimatorStateInfo(0).fullPathHash == ClappingStateHash);
    }

    public void CelebrateHarvest()
    {
        if (!isActiveAndEnabled || !animatorConfigured || animator == null ||
            clapRequested || IsClapping()) return;

        // Suspend gaze greetings until the player looks away, including during the return to idle.
        animator.SetBool(GreetHash, false);
        animator.ResetTrigger(ClapHash);
        animator.SetTrigger(ClapHash);
        clapRequested = true;
        gazeArmed = false;
        gazeHeld = 0f;
        gazeLost = 0f;
    }

    private void Update()
    {
        UpdateBlink();
        if (!animatorConfigured || animator == null) return;
        bool clapping = IsClapping();
        if (clapping) clapRequested = false;
        if (clapRequested || clapping) return;
        if (viewer == null && Camera.main != null) viewer = Camera.main.transform;

        bool looking = viewer != null && IsLookingAtTeacher();
        if (looking)
        {
            gazeLost = 0f;
            gazeHeld += Time.deltaTime;
            if (gazeArmed && gazeHeld >= gazeDuration)
            {
                animator.SetBool(GreetHash, true);
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
                animator.SetBool(GreetHash, false);
            }
        }
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

    [ContextMenu("Test harvest clapping in Play mode")]
    private void TestClapping()
    {
        if (Application.isPlaying) CelebrateHarvest();
    }

    private void OnDisable()
    {
        if (!animatorConfigured || animator == null) return;
        animator.SetBool(GreetHash, false);
        animator.ResetTrigger(ClapHash);
        clapRequested = false;
        gazeArmed = true;
        gazeHeld = gazeLost = 0f;
    }

    private void OnDestroy()
    {
        if (animatorConfigured && animator != null)
            animator.runtimeAnimatorController = originalController;
        if (rootMotionChanged && animator != null) animator.applyRootMotion = originalApplyRootMotion;
    }
}
