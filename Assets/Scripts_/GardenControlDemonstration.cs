using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Standalone Meta visuals with configurable pose/press timing; no tracked hands or interactors.</summary>
[DisallowMultipleComponent]
public sealed class GardenControlDemonstration : MonoBehaviour
{
    private GronnyAudioCatalogue catalogue;
    private GardenInputMode mode;
    private GameObject anchor;
    private GameObject model;
    private Transform motion;
    private Transform animationRoot;
    private Transform[] joints;
    private Quaternion[][] poses;
    private Vector3[] restPositions;
    private TextMesh caption;
    private LineRenderer pointer;
    private Material visualMaterial;
    private GronnyAudioCatalogue.Cue cue;
    private AnimationClip pressClip;
    private GameObject buttonHighlight;

    public void Configure(GronnyAudioCatalogue settings) => catalogue = settings;

    public void SetInputMode(GardenInputMode inputMode)
    {
        mode = inputMode;
        // A mode change cancels any demonstration, including a general watering instruction.
        Hide();
    }

    public void Show(GronnyAudioCatalogue.Cue instruction)
    {
        Hide();
        if (instruction.demo == GardenControlDemo.None || mode == GardenInputMode.Unknown) return;
        Transform viewer = Camera.main != null ? Camera.main.transform : null;
        if (viewer == null)
        {
            GameObject eye = GameObject.Find("CenterEyeAnchor");
            if (eye != null) viewer = eye.transform;
        }
        if (viewer == null) return;
        cue = instruction;
        bool left = cue.demo == GardenControlDemo.Calendar || cue.demo == GardenControlDemo.Move;
        var hand = left ? catalogue.leftHand : catalogue.rightHand;
        GameObject prefab = mode == GardenInputMode.Hands ? hand.prefab :
            left ? catalogue.leftController : catalogue.rightController;
        if (prefab == null) { cue = null; return; }

        anchor = new GameObject("Gronny control demonstration");
        anchor.SetActive(false);
        // World-fixed placement avoids a hand model following every head turn.
        Vector3 forward = Vector3.ProjectOnPlane(viewer.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        Quaternion facing = Quaternion.LookRotation(forward, Vector3.up);
        Vector3 offset = catalogue.viewOffset;
        if (left) offset.x = -Mathf.Abs(offset.x);
        Vector3 position = viewer.position + facing * offset;
        // The movement gesture must read in profile from the eye, including its side offset.
        if (mode == GardenInputMode.Hands && cue.demo == GardenControlDemo.Move)
            facing = Quaternion.LookRotation(position - viewer.position, Vector3.up);
        anchor.transform.SetPositionAndRotation(position, facing);
        GameObject moving = new("Motion");
        motion = moving.transform;
        motion.SetParent(anchor.transform, false);
        model = Instantiate(prefab, motion);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(mode == GardenInputMode.Hands
            ? catalogue.handEulerAngles : catalogue.controllerEulerAngles);
        // Imported FBX roots include the centimetres-to-metres conversion.
        model.transform.localScale = prefab.transform.localScale * catalogue.visualScale;
        foreach (MonoBehaviour behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))
            behaviour.enabled = false;
        foreach (Animator animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        foreach (Collider collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Rigidbody body in model.GetComponentsInChildren<Rigidbody>(true))
        { body.isKinematic = true; body.detectCollisions = false; }
        foreach (Transform child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 2;

        if (visualMaterial == null)
        {
            visualMaterial = new Material(catalogue.demonstrationMaterial);
            visualMaterial.SetColor("_BaseColor", new Color(0.4f, 0.85f, 0.9f, 1f));
        }
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            // Keep Meta's ghost wire material and the controller's readable button colours.
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        if (mode == GardenInputMode.Hands)
        {
            animationRoot = model.transform.Find(hand.animationRoot);
            if (animationRoot == null)
            {
                Debug.LogWarning("Gronny hand animation root is missing: " + hand.animationRoot, this);
                Hide();
                return;
            }
            CachePoses(hand);
        }
        else
        {
            animationRoot = model.transform;
            pressClip = cue.demo switch
            {
                GardenControlDemo.Move => catalogue.controllerStick,
                GardenControlDemo.Grab => catalogue.controllerGrip,
                GardenControlDemo.Calendar => catalogue.controllerY,
                GardenControlDemo.CalendarSelect => catalogue.controllerTrigger,
                _ => null
            };
            Transform target = string.IsNullOrEmpty(cue.controllerTarget) ? null : model.transform.Find(cue.controllerTarget);
            if (target != null)
                buttonHighlight = MakeSymbol(PrimitiveType.Sphere, "Example pressed control", target,
                    Vector3.zero, Vector3.one * 0.8f);
        }
        GameObject text = new("Step caption");
        text.transform.SetParent(anchor.transform, false);
        text.transform.localPosition = new Vector3(0f, -0.20f, 0f);
        caption = text.AddComponent<TextMesh>();
        caption.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        caption.GetComponent<MeshRenderer>().sharedMaterial = caption.font.material;
        caption.anchor = TextAnchor.MiddleCenter;
        caption.alignment = TextAlignment.Center;
        caption.characterSize = 0.008f;
        caption.fontSize = 48;
        caption.color = Color.white;
        if (cue.demo == GardenControlDemo.CalendarSelect) CreatePointer();
        if (cue.demo == GardenControlDemo.Water || cue.demo == GardenControlDemo.Upright) CreateCanSymbol();
        foreach (Transform child in anchor.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 2;
        anchor.SetActive(true);
        Tick(0f);
    }

    private void CachePoses(GronnyAudioCatalogue.HandVisual hand)
    {
        joints = animationRoot.GetComponentsInChildren<Transform>(true);
        poses = new Quaternion[5][];
        restPositions = new Vector3[joints.Length];
        poses[0] = new Quaternion[joints.Length];
        for (int j = 0; j < joints.Length; j++)
        {
            poses[0][j] = joints[j].localRotation;
            restPositions[j] = joints[j].localPosition;
        }
        AnimationClip[] clips = { null, hand.pinch, hand.curled, hand.point, hand.thumbFree };
        for (int p = 1; p < poses.Length; p++)
        {
            for (int j = 0; j < joints.Length; j++) joints[j].localRotation = poses[0][j];
            if (clips[p] != null) clips[p].SampleAnimation(animationRoot.gameObject, 0f);
            poses[p] = new Quaternion[joints.Length];
            for (int j = 0; j < joints.Length; j++) poses[p][j] = joints[j].localRotation;
        }
        if (cue.demo == GardenControlDemo.Move)
        {
            // Meta's mid-fist pose opens the fingers; the activation tap only moves the thumb.
            for (int j = 0; j < joints.Length; j++)
                if (!joints[j].name.Contains("_thumb"))
                    poses[(int)GardenDemoPose.Curled][j] = poses[(int)GardenDemoPose.ThumbFree][j];
        }
    }

    public void Tick(float audioSeconds)
    {
        if (anchor == null || cue == null || cue.steps == null || cue.steps.Length == 0) return;
        int index = 0;
        for (int i = 1; i < cue.steps.Length; i++)
            if (audioSeconds >= cue.steps[i].at) index = i;
        GronnyAudioCatalogue.Step step = cue.steps[index];
        GronnyAudioCatalogue.Step previous = index > 0 ? cue.steps[index - 1] : step;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((audioSeconds - step.at) / catalogue.blendSeconds));
        motion.localPosition = Vector3.Lerp(previous.position, step.position, t);
        motion.localRotation = Quaternion.Slerp(Quaternion.Euler(previous.rotation), Quaternion.Euler(step.rotation), t);
        caption.text = step.label;
        if (mode == GardenInputMode.Hands && poses != null)
        {
            for (int j = 0; j < joints.Length; j++)
            {
                joints[j].localPosition = restPositions[j];
                joints[j].localRotation = Quaternion.Slerp(poses[(int)previous.pose][j], poses[(int)step.pose][j], t);
            }
            // The swipe direction is a timed, configurable thumb rotation, independent of frame rate.
            if (cue.demo == GardenControlDemo.Move && step.pose == GardenDemoPose.ThumbFree)
                foreach (Transform joint in joints)
                    if (joint.name.EndsWith("thumb1"))
                        joint.localRotation *= Quaternion.Euler(Vector3.Lerp(previous.thumbRotation, step.thumbRotation, t));
        }
        else if (pressClip != null)
        {
            float pressed = Mathf.Lerp(previous.press, step.press, t);
            pressClip.SampleAnimation(animationRoot.gameObject, pressed * pressClip.length);
        }
        if (pointer != null)
        {
            pointer.SetPosition(0, motion.localPosition);
            pointer.SetPosition(1, motion.localPosition + Vector3.forward * 0.35f);
        }
        if (buttonHighlight != null) buttonHighlight.SetActive(step.press > 0f);
    }

    private void CreatePointer()
    {
        GameObject ray = new("Example selection ray");
        ray.transform.SetParent(anchor.transform, false);
        pointer = ray.AddComponent<LineRenderer>();
        pointer.useWorldSpace = false;
        pointer.positionCount = 2;
        pointer.startWidth = pointer.endWidth = 0.003f;
        pointer.sharedMaterial = visualMaterial;
        GameObject button = MakeSymbol(PrimitiveType.Cube, "Example button", anchor.transform,
            new Vector3(0f, 0f, 0.35f), new Vector3(0.13f, 0.07f, 0.01f));
        button.transform.localRotation = Quaternion.identity;
    }

    private void CreateCanSymbol()
    {
        MakeSymbol(PrimitiveType.Cylinder, "Example can body", motion,
            new Vector3(0.07f, -0.03f, 0.03f), new Vector3(0.08f, 0.05f, 0.08f));
        GameObject spout = MakeSymbol(PrimitiveType.Cube, "Example spout", motion,
            new Vector3(0.13f, 0f, 0.03f), new Vector3(0.09f, 0.02f, 0.02f));
        spout.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
    }

    private GameObject MakeSymbol(PrimitiveType primitive, string name, Transform parent, Vector3 position, Vector3 scale)
    {
        GameObject symbol = GameObject.CreatePrimitive(primitive);
        symbol.name = name;
        symbol.layer = 2;
        symbol.transform.SetParent(parent, false);
        symbol.transform.localPosition = position;
        symbol.transform.localScale = scale;
        Collider collider = symbol.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        Renderer renderer = symbol.GetComponent<Renderer>();
        renderer.sharedMaterial = visualMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return symbol;
    }

    public void Hide()
    {
        if (anchor != null) { anchor.SetActive(false); Destroy(anchor); }
        anchor = null;
        model = null;
        cue = null;
        pointer = null;
        pressClip = null;
        buttonHighlight = null;
        poses = null;
        joints = null;
    }

    private void OnDisable() => Hide();
    private void OnDestroy()
    {
        Hide();
        if (visualMaterial != null) Destroy(visualMaterial);
    }
}
