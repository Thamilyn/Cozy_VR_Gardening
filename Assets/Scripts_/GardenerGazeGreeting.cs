using UnityEngine;

/// <summary>Waves once when the player's headset rests on the gardening teacher.</summary>
[DisallowMultipleComponent]
public sealed class GardenerGazeGreeting : MonoBehaviour
{
    private const string TeacherName = "Gardener Kawaii - Profesor de jardineria";
    private const string EyeAnchorName = "CenterEyeAnchor";

    [Header("Gaze")]
    [SerializeField, Range(3f, 30f)] private float gazeAngleDegrees = 12f;
    [SerializeField, Min(0.1f)] private float gazeDuration = 0.8f;
    [SerializeField, Min(0.5f)] private float maximumDistance = 4f;
    [SerializeField, Min(0.1f)] private float lookAwayToReset = 0.5f;
    [SerializeField, Min(0f)] private float greetingCooldown = 6f;

    [Header("Wave")]
    [SerializeField, Min(0.5f)] private float waveDuration = 2.4f;

    private readonly RaycastHit[] occlusionHits = new RaycastHit[16];
    private Transform teacher;
    private Transform viewer;
    private Transform head;
    private Transform leftUpperArm;
    private Transform leftLowerArm;
    private Transform rightUpperArm;
    private Transform rightLowerArm;
    private Transform rightHand;
    private Quaternion headRest;
    private Quaternion leftUpperRest;
    private Quaternion leftLowerRest;
    private Quaternion rightUpperRest;
    private Quaternion rightLowerRest;
    private Quaternion rightHandRest;
    private float gazeHeld;
    private float gazeLost;
    private float poseBlend;
    private float waveStartedAt;
    private float nextGreetingTime;
    private bool gazeArmed = true;
    private bool waving;
    private bool bonesReady;

    private void Start()
    {
        var teacherObject = GameObject.Find(TeacherName);
        var eyeObject = GameObject.Find(EyeAnchorName);
        teacher = teacherObject != null ? teacherObject.transform : null;
        viewer = eyeObject != null ? eyeObject.transform : Camera.main != null ? Camera.main.transform : null;

        if (teacher == null)
        {
            Debug.LogWarning("Gardener gaze greeting: teacher was not found in the scene.", this);
            enabled = false;
            return;
        }

        var animator = teacher.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
        {
            Debug.LogWarning("Gardener gaze greeting: the VRM needs a humanoid Animator.", this);
            enabled = false;
            return;
        }

        head = animator.GetBoneTransform(HumanBodyBones.Head);
        leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (head == null || leftUpperArm == null || leftLowerArm == null ||
            rightUpperArm == null || rightLowerArm == null || rightHand == null)
        {
            Debug.LogWarning("Gardener gaze greeting: the VRM is missing an arm or head bone.", this);
            enabled = false;
            return;
        }

        headRest = head.localRotation;
        leftUpperRest = leftUpperArm.localRotation;
        leftLowerRest = leftLowerArm.localRotation;
        rightUpperRest = rightUpperArm.localRotation;
        rightLowerRest = rightLowerArm.localRotation;
        rightHandRest = rightHand.localRotation;
        bonesReady = true;
    }

    private void Update()
    {
        if (viewer == null && Camera.main != null)
            viewer = Camera.main.transform;

        bool looking = viewer != null && IsLookingAtTeacher();
        if (looking)
        {
            gazeLost = 0f;
            gazeHeld += Time.deltaTime;
            if (gazeArmed && gazeHeld >= gazeDuration && Time.time >= nextGreetingTime)
            {
                BeginGreeting();
                gazeArmed = false;
            }
        }
        else
        {
            gazeHeld = 0f;
            gazeLost += Time.deltaTime;
            if (gazeLost >= lookAwayToReset)
                gazeArmed = true;
        }
    }

    private bool IsLookingAtTeacher()
    {
        Vector3 toHead = head.position - viewer.position;
        float distance = toHead.magnitude;
        if (distance < 0.1f || distance > maximumDistance)
            return false;

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

    private void BeginGreeting()
    {
        waving = true;
        waveStartedAt = Time.time;
        nextGreetingTime = Time.time + greetingCooldown;
    }

    [ContextMenu("Test greeting in Play mode")]
    private void TestGreeting()
    {
        if (Application.isPlaying && bonesReady)
            BeginGreeting();
    }

    private void LateUpdate()
    {
        if (!bonesReady || head == null || leftUpperArm == null || leftLowerArm == null ||
            rightUpperArm == null || rightLowerArm == null || rightHand == null)
            return;

        poseBlend = Mathf.MoveTowards(poseBlend, 1f, Time.deltaTime / 0.5f);
        float gesture = 0f;
        float waveTime = Time.time - waveStartedAt;
        if (waving)
        {
            if (waveTime >= waveDuration)
                waving = false;
            else
            {
                float easeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(waveTime / 0.35f));
                float easeOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((waveDuration - waveTime) / 0.45f));
                gesture = easeIn * easeOut;
            }
        }

        // The VRM's rest pose has horizontal arms. Relax both arms before lifting the right one.
        leftUpperArm.localRotation = leftUpperRest * Quaternion.Euler(0f, 0f, -80f * poseBlend);
        leftLowerArm.localRotation = leftLowerRest * Quaternion.Euler(0f, 0f, -8f * poseBlend);
        rightUpperArm.localRotation = rightUpperRest * Quaternion.Euler(0f, 0f,
            Mathf.Lerp(80f, -30f, gesture) * poseBlend);
        rightLowerArm.localRotation = rightLowerRest * Quaternion.Euler(0f, 0f,
            Mathf.Lerp(8f, -55f, gesture) * poseBlend);
        rightHand.localRotation = rightHandRest * Quaternion.Euler(0f, 0f,
            18f * gesture * Mathf.Sin(waveTime * Mathf.PI * 5f));
        head.localRotation = headRest * Quaternion.Euler(2f * gesture, 0f, 3f * gesture);
    }

    private void OnDisable()
    {
        if (!bonesReady || head == null || leftUpperArm == null || leftLowerArm == null ||
            rightUpperArm == null || rightLowerArm == null || rightHand == null)
            return;

        head.localRotation = headRest;
        leftUpperArm.localRotation = leftUpperRest;
        leftLowerArm.localRotation = leftLowerRest;
        rightUpperArm.localRotation = rightUpperRest;
        rightLowerArm.localRotation = rightLowerRest;
        rightHand.localRotation = rightHandRest;
    }
}
