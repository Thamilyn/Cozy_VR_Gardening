using Oculus.Interaction;
using Oculus.Interaction.Grab;
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
    [SerializeField, Min(0.001f)] private float cuttingRadius = 0.015f;

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

        GardenGrabSetup.ConfigureGrabbable(gameObject);
        grabbable = GetComponent<Grabbable>();
        grabbable.MaxGrabPoints = 1;
        GetComponent<Rigidbody>().mass = 0.18f;
        controllerGrabs = GetComponentsInChildren<GrabInteractable>(true);
        handGestures = GetComponent<PrunerHandGrab>();
        if (handGestures == null) handGestures = gameObject.AddComponent<PrunerHandGrab>();
        ApplyBladePose();
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
        // A normal palm grab picks up the shears; PrunerHandGrab owns the release gesture.
        handGrab.InjectSupportedGrabTypes(GrabTypeFlags.Palm);
        handGrab.InjectPalmGrabRules(Oculus.Interaction.GrabAPI.GrabbingRule.DefaultPalmRule);
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

        closure = Mathf.MoveTowards(closure, held ? squeeze : 0f,
            deltaTime / Mathf.Max(0.01f, animationDuration));
        ApplyBladePose();
        if (GardenMenuController.GardenSuspended)
        {
            ResetCut();
            return;
        }
        // Require opening after grabbing, changing hands, pausing or losing tracking.
        if (held && squeeze <= 0.25f && closure <= 0.25f) cutArmed = true;
        if (!held || !cutArmed || squeeze < 0.9f || closure < 0.9f) return;
        cutArmed = false;
        CutNearestStalk();
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

    private void CutNearestStalk()
    {
        Vector3 start = model.TransformPoint(cuttingStart);
        Vector3 end = model.TransformPoint(cuttingEnd);
        Vector3 segment = end - start;
        if (segment.sqrMagnitude < 0.000001f) return;

        TomatoFruit nearest = null;
        float nearestDistance = cuttingRadius * cuttingRadius;
        // A search only on a completed snip avoids scene scans or allocations every frame.
        foreach (TomatoFruit fruit in FindObjectsByType<TomatoFruit>(FindObjectsSortMode.None))
        {
            if (fruit.gameObject.scene != gameObject.scene || !fruit.CanCutStem) continue;
            Vector3 stalk = fruit.StemPosition;
            Vector3 closest = start + segment * Mathf.Clamp01(Vector3.Dot(stalk - start, segment) / segment.sqrMagnitude);
            float distance = (stalk - closest).sqrMagnitude;
            if (distance > nearestDistance) continue;
            nearestDistance = distance;
            nearest = fruit;
        }
        if (nearest != null) nearest.TryCutStem();
    }

    private void ResetCut()
    {
        holdingInteractor = -1;
        cutArmed = false;
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
