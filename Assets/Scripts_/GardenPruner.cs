using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

/// <summary>One-handed pruning shears: squeeze to animate the blades and snip a ripe stalk.</summary>
[DisallowMultipleComponent]
public sealed class GardenPruner : MonoBehaviour
{
    [SerializeField, Range(1f, 45f)] private float closingAngle = 22f;
    [SerializeField, Min(0.01f)] private float animationDuration = 0.12f;
    [Tooltip("Maximum distance in metres between a stalk and the cutting edges.")]
    [SerializeField, Min(0.001f)] private float cuttingRadius = 0.025f;
    [Tooltip("A deliberate squeeze closes the blades without requiring a fully clenched tracked hand.")]
    [SerializeField, Range(0.5f, 1f)] private float squeezeToCut = 0.75f;
    [SerializeField, Range(0.05f, 0.45f)] private float squeezeToOpen = 0.3f;
    [Tooltip("Brief alignment memory during closing, to tolerate small hand-tracking movements.")]
    [SerializeField, Range(0f, 0.3f)] private float alignmentGraceSeconds = 0.18f;
    [Tooltip("Extra hand-grab tolerance around the handles, in metres. Does not change solid collisions or cutting reach.")]
    [SerializeField, Range(0.005f, 0.03f)] private float handleGrabPadding = 0.018f;

    private Transform bottomPart;
    private Transform topPart;
    private Transform model;
    private Quaternion bottomOpenRotation;
    private Quaternion topOpenRotation;
    private float bottomClosingSign;
    private float topClosingSign;
    private Vector3 cuttingStart;
    private Vector3 cuttingEnd;
    private GrabInteractable[] controllerGrabs;
    private PrunerHandGrab handGestures;
    private Grabbable grabbable;
    private float closure;
    private int holdingInteractor = -1;
    private bool cutArmed;
    private TomatoFruit cuttingFruit;
    private TomatoFruit[] strokeFruits;
    private TomatoFruit alignedDuringClosing;
    private float alignedAt = float.NegativeInfinity;
    private float nextFruitScan;

    private void Awake()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "BottomPart") bottomPart = child;
            if (child.name == "TopPart") topPart = child;
        }
        if (bottomPart == null || topPart == null || bottomPart.parent != topPart.parent ||
            !TryPreparePart(bottomPart, out Bounds bottomBounds) ||
            !TryPreparePart(topPart, out Bounds topBounds))
        {
            Debug.LogError("Pruner needs sibling BottomPart and TopPart meshes with hinge pivots.", this);
            enabled = false;
            return;
        }

        model = bottomPart.parent;
        bottomOpenRotation = bottomPart.localRotation;
        topOpenRotation = topPart.localRotation;
        // The imported handles extend away from the blade side. Their signs also
        // account for the FBX importer's handedness conversion without changing the asset.
        bottomClosingSign = Mathf.Sign(bottomBounds.center.x);
        topClosingSign = Mathf.Sign(topBounds.center.x);
        float reach = Mathf.Min(
            model.InverseTransformPoint(bottomPart.TransformPoint(Vector3.forward * bottomBounds.max.z)).z,
            model.InverseTransformPoint(topPart.TransformPoint(Vector3.forward * topBounds.max.z)).z);
        // The closed edges meet slightly to one side of the hinge's forward axis.
        // These proportions follow this model's blades and preserve its imported scale.
        cuttingStart = new Vector3(-bottomClosingSign * 0.05f, 0f, 0.25f) * reach;
        cuttingEnd = new Vector3(-bottomClosingSign * 0.2f, 0f, 0.9f) * reach;

        PrepareHandleGrabVolume(bottomBounds, topBounds);
        GardenGrabSetup.ConfigureGrabbable(gameObject);
        grabbable = GetComponent<Grabbable>();
        grabbable.MaxGrabPoints = 1;
        GetComponent<Rigidbody>().mass = 0.18f;
        controllerGrabs = GetComponentsInChildren<GrabInteractable>(true);
        handGestures = GetComponent<PrunerHandGrab>();
        if (handGestures == null) handGestures = gameObject.AddComponent<PrunerHandGrab>();
        ApplyBladePose();
        GardenObjectLabel.Create(gameObject, "Pruner", Vector3.up * 0.12f,
            () => GronnyTutorial.RequestsPruner);
    }

    private static bool TryPreparePart(Transform part, out Bounds bounds)
    {
        bounds = default;
        MeshFilter mesh = part.GetComponent<MeshFilter>();
        if (mesh == null || mesh.sharedMesh == null) return false;
        bounds = mesh.sharedMesh.bounds;
        if (part.GetComponent<Collider>() == null)
        {
            // Separate hulls follow the two blades instead of a single stationary box.
            MeshCollider collider = part.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh.sharedMesh;
            collider.convex = true;
        }
        return true;
    }

    internal static void ConfigureHandGrab(HandGrabInteractable handGrab)
    {
        // Both natural pickup gestures use Meta's selection. PrunerHandGrab keeps
        // the tool held when the fingers open again to operate the blades.
        handGrab.InjectSupportedGrabTypes(GrabTypeFlags.Pinch | GrabTypeFlags.Palm);
        handGrab.InjectPinchGrabRules(GrabbingRule.DefaultPinchRule);
        GrabbingRule palmRule = GrabbingRule.DefaultPalmRule;
        palmRule[HandFinger.Index] = FingerRequirement.Optional;
        palmRule[HandFinger.Ring] = FingerRequirement.Optional;
        handGrab.InjectPalmGrabRules(palmRule);
    }

    private void PrepareHandleGrabVolume(Bounds bottomBounds, Bounds topBounds)
    {
        const string volumeName = "Pruner handle pickup";
        if (transform.Find(volumeName) != null) return;

        // Thin moving mesh hulls are difficult to overlap with tracked palm/pinch
        // volumes. Bridge only the handles, leaving physical hulls and blades alone.
        Bounds handles = HandleBoundsInRoot(bottomPart, bottomBounds);
        handles.Encapsulate(HandleBoundsInRoot(topPart, topBounds));
        Vector3 scale = transform.lossyScale;
        handles.Expand(new Vector3(
            2f * handleGrabPadding / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
            2f * handleGrabPadding / Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
            2f * handleGrabPadding / Mathf.Max(Mathf.Abs(scale.z), 0.0001f)));

        GameObject pickup = new GameObject(volumeName);
        pickup.layer = gameObject.layer;
        pickup.transform.SetParent(transform, false);
        BoxCollider volume = pickup.AddComponent<BoxCollider>();
        volume.center = handles.center;
        volume.size = handles.size;
        volume.isTrigger = true;
        // Created before Meta's Start caches the body's colliders. No extra body
        // or custom hand detector is needed, and the table still uses solid hulls.
    }

    private Bounds HandleBoundsInRoot(Transform part, Bounds meshBounds)
    {
        Vector3 minimum = meshBounds.min;
        Vector3 maximum = meshBounds.max;
        maximum.z = Mathf.Min(0f, maximum.z); // Handles are behind this model's hinge.
        Bounds bounds = new Bounds(transform.InverseTransformPoint(part.TransformPoint(minimum)), Vector3.zero);
        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 point = new Vector3(
                (corner & 1) == 0 ? minimum.x : maximum.x,
                (corner & 2) == 0 ? minimum.y : maximum.y,
                (corner & 4) == 0 ? minimum.z : maximum.z);
            bounds.Encapsulate(transform.InverseTransformPoint(part.TransformPoint(point)));
        }
        return bounds;
    }

    private void LateUpdate()
    {
        bool held = TryGetSqueeze(out float squeeze, out int interactor);
        UpdateBlades(held, squeeze, interactor, Time.deltaTime);
    }

    private void UpdateBlades(bool held, float squeeze, int interactor, float deltaTime)
    {
        if (!held || interactor != holdingInteractor)
        {
            ResetCut();
            holdingInteractor = interactor;
        }

        float desiredClosure = held ? Mathf.InverseLerp(squeezeToOpen, squeezeToCut, squeeze) : 0f;
        closure = Mathf.MoveTowards(closure, desiredClosure,
            deltaTime / Mathf.Max(0.01f, animationDuration));
        ApplyBladePose();
        if (GardenMenuController.GardenSuspended)
        {
            ResetCut();
            return;
        }
        // Require opening after grabbing, changing hands, pausing or losing tracking.
        if (held && squeeze <= squeezeToOpen && closure <= 0.25f)
        {
            // Refresh while open as fruit may ripen after the tool is picked up.
            // Keep scene discovery out of the per-frame aiming/closing path.
            if (!cutArmed || Time.unscaledTime >= nextFruitScan)
            {
                strokeFruits = FindObjectsByType<TomatoFruit>(FindObjectsSortMode.None);
                nextFruitScan = Time.unscaledTime + 0.5f;
            }
            cutArmed = true;
            alignedDuringClosing = null;
            alignedAt = float.NegativeInfinity;
        }
        if (!held || !cutArmed) return;
        TomatoFruit aligned = FindNearestStalk(cuttingRadius);
        if (aligned != null)
        {
            aligned.ShowCutAlignment();
            if (squeeze > squeezeToOpen)
            {
                alignedDuringClosing = aligned;
                alignedAt = Time.unscaledTime;
            }
        }
        if (squeeze < squeezeToCut || closure < 0.9f) return;
        cutArmed = false;
        TomatoFruit target = aligned;
        float graceRadius = cuttingRadius + 0.01f;
        // Allow a centimetre of drift while the blades close. Moving closed blades
        // onto a new stalk still cannot cut: this stroke has already been consumed.
        if (target == null && alignmentGraceSeconds > 0f && alignedDuringClosing != null && alignedDuringClosing.CanCutStem &&
            Time.unscaledTime - alignedAt <= alignmentGraceSeconds &&
            SquaredDistanceToStalk(alignedDuringClosing) <= graceRadius * graceRadius)
            target = alignedDuringClosing;
        if (target == null) return;
        cuttingFruit = target;
        try { target.TryCutStem(this); }
        finally { cuttingFruit = null; }
    }

    private bool TryGetSqueeze(out float squeeze, out int interactorId)
    {
        squeeze = 0f;
        interactorId = -1;
        if (grabbable == null || grabbable.SelectingPointsCount == 0 ||
            (OVRManager.instance != null && !OVRManager.instance.isUserPresent)) return false;

        foreach (GrabInteractable grab in controllerGrabs)
        {
            if (grab == null || !grab.isActiveAndEnabled) continue;
            foreach (GrabInteractor interactor in grab.SelectingInteractors)
            {
                ControllerRef controller = interactor.GetComponentInParent<ControllerRef>();
                if (controller == null || !controller.IsConnected || !controller.IsPoseValid) continue;
                squeeze = Mathf.Clamp01(controller.ControllerInput.Trigger);
                interactorId = interactor.Identifier;
                return true;
            }
        }
        return handGestures != null && handGestures.isActiveAndEnabled &&
            handGestures.TryGetSqueeze(out squeeze, out interactorId);
    }

    private void ApplyBladePose()
    {
        bottomPart.localRotation = bottomOpenRotation *
            Quaternion.AngleAxis(bottomClosingSign * closingAngle * closure, Vector3.up);
        topPart.localRotation = topOpenRotation *
            Quaternion.AngleAxis(topClosingSign * closingAngle * closure, Vector3.up);
    }

    private TomatoFruit FindNearestStalk(float radius)
    {
        if (strokeFruits == null) return null;
        TomatoFruit nearest = null;
        float nearestDistance = radius * radius;
        foreach (TomatoFruit fruit in strokeFruits)
        {
            if (fruit == null || fruit.gameObject.scene != gameObject.scene || !fruit.CanCutStem) continue;
            float distance = SquaredDistanceToStalk(fruit);
            if (distance > nearestDistance) continue;
            nearestDistance = distance;
            nearest = fruit;
        }
        return nearest;
    }

    private float SquaredDistanceToStalk(TomatoFruit fruit)
    {
        Vector3 start = model.TransformPoint(cuttingStart);
        Vector3 segment = model.TransformPoint(cuttingEnd) - start;
        if (segment.sqrMagnitude < 0.000001f) return float.PositiveInfinity;
        Vector3 closest = start + segment * Mathf.Clamp01(
            Vector3.Dot(fruit.StemPosition - start, segment) / segment.sqrMagnitude);
        return fruit.IsOutsideFruit(closest) ? (fruit.StemPosition - closest).sqrMagnitude : float.PositiveInfinity;
    }

    internal bool IsCuttingStem(TomatoFruit fruit)
    {
        // Authorize only the stalk selected by this completed closing stroke.
        return isActiveAndEnabled && fruit != null && cuttingFruit == fruit &&
            grabbable != null && grabbable.SelectingPointsCount > 0 && closure >= 0.9f;
    }

    private void ResetCut()
    {
        holdingInteractor = -1;
        cutArmed = false;
        strokeFruits = null;
        alignedDuringClosing = null;
        alignedAt = float.NegativeInfinity;
    }

    private void OnDisable()
    {
        ResetCut();
        closure = 0f;
        if (model != null) ApplyBladePose();
    }

    private void OnDrawGizmosSelected()
    {
        if (model == null) return;
        Vector3 start = model.TransformPoint(cuttingStart);
        Vector3 end = model.TransformPoint(cuttingEnd);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, cuttingRadius);
        Gizmos.DrawWireSphere(end, cuttingRadius);
    }
}
