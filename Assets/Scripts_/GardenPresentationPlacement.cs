using System.Collections.Generic;
using UnityEngine;

/// <summary>Moves tutorial visuals into free space instead of drawing them through the garden.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(100)]
public sealed class GardenPresentationPlacement : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float clearance = 0.04f;
    [SerializeField, Min(0f), Tooltip("Total lift from the original placement; never accumulates between frames.")]
    private float maximumLift = 0.35f;
    [SerializeField, Min(0f), Tooltip("Maximum height of the visual centre above the eyes at placement time.")]
    private float maximumHeightAboveView = 0.25f;
    [SerializeField] private LayerMask obstacleLayers = ~0;

    private static readonly List<GardenPresentationPlacement> presentations = new();
    private readonly Collider[] overlaps = new Collider[64];
    private readonly RaycastHit[] sightHits = new RaycastHit[64];
    private readonly Vector3[] corners = new Vector3[4];
    private Transform content;
    private Transform viewer;
    private Transform playerRig;
    private Collider interactionCollider;
    private Canvas[] canvases;
    private Renderer[] renderers;
    private RectTransform[] rectangles;
    private bool[] canvasStates;
    private bool[] rendererStates;
    private bool colliderState;
    private bool suppressed;
    private float retryAt;
    private Vector3 referencePosition;
    private float maximumCenterHeight;
    private bool hasViewHeight;
    private bool keepStatic;
    private bool positionLocked;
    private Vector3 lockedPosition;
    private Quaternion lockedRotation;

    public void Configure(Transform presentation, Transform view, Collider targetCollider = null,
        bool keepStatic = false)
    {
        // Restore the saved visual state before replacing the cached components/states.
        SetSuppressed(false);
        content = presentation;
        viewer = view;
        interactionCollider = targetCollider;
        // Interaction targets must never turn a relocating visual into a physical platform.
        if (interactionCollider != null) interactionCollider.isTrigger = true;
        this.keepStatic = keepStatic;
        // The calendar Canvas is on this root, while its hideable content is a child.
        canvases = GetComponentsInChildren<Canvas>(true);
        renderers = content.GetComponentsInChildren<Renderer>(true);
        rectangles = content.GetComponentsInChildren<RectTransform>(true);
        canvasStates = new bool[canvases.Length];
        rendererStates = new bool[renderers.Length];
        ResetReferencePose();
        Register();
        ResolveNow();
    }

    public void ResetReferencePose()
    {
        if (viewer == null && Camera.main != null) viewer = Camera.main.transform;
        referencePosition = transform.position;
        hasViewHeight = viewer != null;
        maximumCenterHeight = hasViewHeight ? viewer.position.y + maximumHeightAboveView : float.PositiveInfinity;
        // Authored instruction boards already have a scene position. They must not use an
        // untracked eye height or transient occlusion to relocate themselves or hide their Canvas.
        positionLocked = keepStatic;
        if (positionLocked)
        {
            lockedPosition = transform.position;
            lockedRotation = transform.rotation;
        }
    }

    private void OnEnable() => Register();

    private void Register()
    {
        if (!presentations.Contains(this)) presentations.Add(this);
    }

    private void OnDisable()
    {
        presentations.Remove(this);
        SetSuppressed(false);
    }

    private void OnDestroy() => presentations.Remove(this);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPresentations() => presentations.Clear();

    private void LateUpdate()
    {
        if (suppressed && interactionCollider != null) interactionCollider.enabled = false;
        if (suppressed && Time.unscaledTime < retryAt) return;
        ResolveNow();
    }

    public bool ResolveNow()
    {
        if (content == null || !content.gameObject.activeInHierarchy) return false;
        if (positionLocked)
        {
            // A fixed instruction board must not follow the can, the player or its parent.
            transform.SetPositionAndRotation(lockedPosition, lockedRotation);
            SetSuppressed(false);
            return true;
        }
        if (viewer == null && Camera.main != null) viewer = Camera.main.transform;
        if (!hasViewHeight && viewer != null)
        {
            maximumCenterHeight = viewer.position.y + maximumHeightAboveView;
            hasViewHeight = true;
        }
        if (playerRig == null && viewer != null)
            playerRig = viewer.GetComponentInParent<OVRCameraRig>()?.transform;
        if (!ReadBounds(out Bounds bounds)) return false;

        Vector3 originalPosition = transform.position;
        bool clear = TryLift(bounds, Vector3.zero, out Vector3 offset);
        // Try nearer positions if a wall or tall obstacle leaves no room above the original one.
        if (!clear && viewer != null)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(bounds.center - viewer.position, Vector3.up);
            float distance = horizontal.magnitude;
            if (distance > 0.01f)
            {
                for (float fraction = 0.75f; fraction >= 0.25f && !clear; fraction -= 0.25f)
                {
                    float nearerDistance = Mathf.Max(0.65f, distance * fraction);
                    if (nearerDistance >= distance) continue;
                    Vector3 nearer = horizontal.normalized * (nearerDistance - distance);
                    clear = TryLift(bounds, nearer, out offset);
                }
            }
        }

        if (!clear && viewer != null)
            clear = TryBesideViewer(bounds, out offset);

        if (clear)
        {
            // Interpolating from an intersecting pose would leave the panel inside the table.
            transform.position = originalPosition + offset;
        }
        else
        {
            retryAt = Time.unscaledTime + 0.25f;
        }
        // If all candidates are blocked, wait for free space rather than show an intersection.
        SetSuppressed(!clear);
        return clear;
    }

    private bool TryBesideViewer(Bounds bounds, out Vector3 offset)
    {
        offset = Vector3.zero;
        Vector3 forward = Vector3.ProjectOnPlane(viewer.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        float preferredSide = Vector3.Dot(bounds.center - viewer.position, right) < 0f ? -1f : 1f;

        // A calendar and its selection demo need separate slots at a readable height.
        // Search both sides before giving up; lifting alone cannot separate them under the ceiling.
        for (int distanceIndex = 0; distanceIndex < 2; distanceIndex++)
        {
            float distance = distanceIndex == 0 ? 0.75f : 1f;
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float separation = sideIndex == 0 ? 0.55f : 0.8f;
                for (int directionIndex = 0; directionIndex < 2; directionIndex++)
                {
                    float side = directionIndex == 0 ? preferredSide : -preferredSide;
                    Vector3 target = viewer.position + forward * distance + right * (separation * side);
                    Vector3 candidateOffset = target - bounds.center;
                    candidateOffset.y = 0f;
                    if (TryLift(bounds, candidateOffset, out offset)) return true;
                }
            }
        }
        return false;
    }

    private bool ReadBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (RectTransform rectangle in rectangles)
        {
            if (rectangle == null || !rectangle.gameObject.activeInHierarchy) continue;
            rectangle.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                if (!found) { bounds = new Bounds(corner, Vector3.zero); found = true; }
                else bounds.Encapsulate(corner);
            }
        }
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer visual = renderers[i];
            if (visual == null || !visual.gameObject.activeInHierarchy ||
                !(suppressed ? rendererStates[i] : visual.enabled)) continue;
            if (!found) { bounds = visual.bounds; found = true; }
            else bounds.Encapsulate(visual.bounds);
        }
        if (found) bounds.Expand(clearance * 2f);
        return found;
    }

    private bool TryLift(Bounds bounds, Vector3 initialOffset, out Vector3 offset)
    {
        offset = initialOffset;
        float allowedLift = Mathf.Min(referencePosition.y + maximumLift - transform.position.y,
            maximumCenterHeight - bounds.center.y);
        // Animated bounds or a moved parent can already be above the ceiling.
        offset.y = Mathf.Min(0f, allowedLift);
        for (int attempt = 0; attempt < 8 && offset.y <= allowedLift; attempt++)
        {
            Bounds candidate = bounds;
            candidate.center += offset;
            if (IsFree(candidate, out float lift)) return true;
            offset.y += Mathf.Max(0.05f, lift);
        }
        return false;
    }

    private bool IsFree(Bounds candidate, out float lift)
    {
        lift = 0f;
        int count = Physics.OverlapBoxNonAlloc(candidate.center, candidate.extents, overlaps,
            Quaternion.identity, obstacleLayers, QueryTriggerInteraction.Ignore);
        // A full buffer cannot prove that the candidate is free.
        if (count == overlaps.Length) { lift = maximumLift + 1f; return false; }
        bool blocked = false;
        for (int i = 0; i < count; i++)
        {
            Collider obstacle = overlaps[i];
            if (Ignore(obstacle)) continue;
            blocked = true;
            lift = Mathf.Max(lift, obstacle.bounds.max.y - candidate.min.y + clearance);
        }
        foreach (GardenPresentationPlacement other in presentations)
        {
            if (other == null || other == this || other.suppressed ||
                other.content == null || !other.content.gameObject.activeInHierarchy ||
                !other.ReadBounds(out Bounds otherBounds) || !candidate.Intersects(otherBounds)) continue;
            blocked = true;
            lift = Mathf.Max(lift, otherBounds.max.y - candidate.min.y + clearance);
        }
        if (viewer != null)
        {
            if (Vector3.Distance(candidate.ClosestPoint(viewer.position), viewer.position) < 0.4f)
                blocked = true;
            // Do not relocate the presentation to an empty space behind a wall or the table.
            Vector3 sight = candidate.center - viewer.position;
            int hits = Physics.RaycastNonAlloc(viewer.position, sight.normalized, sightHits,
                sight.magnitude, obstacleLayers, QueryTriggerInteraction.Ignore);
            if (hits == sightHits.Length) { lift = maximumLift + 1f; return false; }
            for (int i = 0; i < hits; i++)
            {
                if (Ignore(sightHits[i].collider)) continue;
                blocked = true;
                lift = Mathf.Max(lift, sightHits[i].collider.bounds.max.y - candidate.min.y + clearance);
            }
        }
        return !blocked;
    }

    private bool Ignore(Collider obstacle) => obstacle == null ||
        obstacle.transform.IsChildOf(transform) ||
        // Other presentations use their current visual bounds, independent of physics sync timing.
        obstacle.GetComponentInParent<GardenPresentationPlacement>() != null ||
        (playerRig != null && obstacle.transform.IsChildOf(playerRig));

    private void SetSuppressed(bool value)
    {
        if (suppressed == value)
        {
            if (suppressed && interactionCollider != null) interactionCollider.enabled = false;
            return;
        }
        suppressed = value;
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] == null) continue;
            if (value) canvasStates[i] = canvases[i].enabled;
            canvases[i].enabled = !value && canvasStates[i];
        }
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            if (value) rendererStates[i] = renderers[i].enabled;
            renderers[i].enabled = !value && rendererStates[i];
        }
        if (interactionCollider != null)
        {
            if (value) colliderState = interactionCollider.enabled;
            interactionCollider.enabled = !value && colliderState;
        }
    }
}
