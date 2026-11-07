using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

/// <summary>Ripe fruit follows the plant until grabbed and counts only after release in the tray.</summary>
[DisallowMultipleComponent]
public sealed class TomatoFruit : MonoBehaviour
{
    public const float DiameterMetres = 0.065f;

    [SerializeField, Min(0.1f)] private float recoveryDelaySeconds = 2f;
    [SerializeField, Min(0.1f)] private float cutRecoveryDelaySeconds = 8f;

    private SeedItem source;
    private Grabbable grabbable;
    private Rigidbody body;
    private TomatoHarvestBasket basket;
    private bool picked;
    private bool stored;
    private float nextBasketSearch;
    private MeshRenderer outline;
    private bool requireNewGrab;
    private GardenObjectLabel harvestLabel;
    private Transform plantAttachment;
    private Vector3 attachedLocalPosition;
    private Quaternion attachedLocalRotation;
    private Vector3 attachedLocalScale;
    private float recoveryRemaining = -1f;
    private float looseRecoveryDelay;
    private Vector3 stemLocalPosition;

    // This component is installed by SeedItem only at the ripe growth stage.
    public bool CanCutStem => isActiveAndEnabled && source != null && source.IsPlanted &&
        source.Crop == SeedCrop.Tomato && !picked && !stored && grabbable != null &&
        grabbable.SelectingPointsCount == 0;
    public Vector3 StemPosition => transform.TransformPoint(stemLocalPosition);

    public void Initialize(SeedItem seed)
    {
        source = seed;
        MeshFilter mesh = GetComponentInChildren<MeshFilter>();
        MeshRenderer renderer = mesh != null ? mesh.GetComponent<MeshRenderer>() : null;
        if (mesh == null || mesh.sharedMesh == null || renderer == null)
        {
            Debug.LogError("Harvestable tomato needs its own mesh and renderer.", this);
            return;
        }
        Bounds bounds = TomatoFruitMesh.TryGetFruitBounds(mesh, renderer, out Bounds fruitBounds)
            ? fruitBounds : mesh.sharedMesh.bounds;
        Vector3 attachment = TomatoFruitMesh.GetAttachmentPoint(mesh, renderer, bounds);
        Vector3 worldAttachment = mesh.transform.TransformPoint(attachment);
        Vector3 size = Vector3.Scale(bounds.size, mesh.transform.lossyScale);
        float diameter = Mathf.Max(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        if (diameter > 0.000001f)
        {
            transform.localScale *= DiameterMetres / diameter;
            transform.position += worldAttachment - mesh.transform.TransformPoint(attachment);
        }
        stemLocalPosition = transform.InverseTransformPoint(worldAttachment);
        looseRecoveryDelay = recoveryDelaySeconds;
        plantAttachment = transform.parent;
        attachedLocalPosition = transform.localPosition;
        attachedLocalRotation = transform.localRotation;
        attachedLocalScale = transform.localScale;
        MaterialPropertyBlock ripeColor = new();
        ripeColor.SetColor("_BaseColor", new Color(0.9f, 0.13f, 0.08f));
        ripeColor.SetColor("_Color", new Color(0.9f, 0.13f, 0.08f));
        Material[] materials = renderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
            if (TomatoFruitMesh.IsFruitMaterial(materials[i])) renderer.SetPropertyBlock(ripeColor, i);

        SphereCollider collider = GetComponent<SphereCollider>();
        if (collider == null) collider = gameObject.AddComponent<SphereCollider>();
        collider.enabled = true;
        collider.isTrigger = false;
        collider.center = transform.InverseTransformPoint(mesh.transform.TransformPoint(bounds.center));
        Vector3 worldSize = Vector3.Scale(bounds.size, mesh.transform.lossyScale);
        Vector3 rootScale = transform.lossyScale;
        collider.radius = Mathf.Max(Mathf.Abs(worldSize.x), Mathf.Abs(worldSize.y), Mathf.Abs(worldSize.z)) * 0.5f /
            Mathf.Max(Mathf.Abs(rootScale.x), Mathf.Abs(rootScale.y), Mathf.Abs(rootScale.z));
        GardenGrabSetup.ConfigureGrabbable(gameObject);
        grabbable = GetComponent<Grabbable>();
        grabbable.MaxGrabPoints = 1;
        grabbable.WhenPointerEventRaised += OnPointerEvent;
        body = GetComponent<Rigidbody>();
        body.mass = 0.08f;
        body.useGravity = false;
        body.isKinematic = true;
        grabbable.ForceKinematicDisabled = true;
        CreateOutline(mesh, renderer);
        harvestLabel = GardenObjectLabel.Create(gameObject, "Ready to harvest", Vector3.up * 0.12f);
    }

    private void CreateOutline(MeshFilter mesh, MeshRenderer renderer)
    {
        Material material = Resources.Load<Material>("GardenHarvest/TomatoOutline");
        if (material == null)
        {
            Debug.LogError("Missing harvest outline material.", this);
            return;
        }
        GameObject shell = new("Harvest outline");
        shell.transform.SetParent(mesh.transform, false);
        Mesh outlineMesh = TomatoFruitMesh.CreateOutlineMesh(shell, mesh, renderer);
        if (outlineMesh == null) { Destroy(shell); return; }
        shell.AddComponent<MeshFilter>().sharedMesh = outlineMesh;
        outline = shell.AddComponent<MeshRenderer>();
        outline.sharedMaterial = material;
        outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outline.receiveShadows = false;
        outline.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        outline.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private void OnPointerEvent(PointerEvent pointerEvent)
    {
        if (stored || pointerEvent.Type != PointerEventType.Select) return;
        recoveryRemaining = -1f;
        looseRecoveryDelay = recoveryDelaySeconds;
        if (picked) return;
        DetachFromPlant();
    }

    public bool TryCutStem()
    {
        if (!CanCutStem || GardenMenuController.GardenSuspended) return false;
        DetachFromPlant();
        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        looseRecoveryDelay = Mathf.Max(0.1f, cutRecoveryDelaySeconds);
        recoveryRemaining = looseRecoveryDelay;
        return true;
    }

    private void DetachFromPlant()
    {
        picked = true;
        // Detach before Meta begins its grab transform, so its initial pose uses the
        // fruit's independent hierarchy rather than the scaled plant hierarchy.
        transform.SetParent(null, true);
        // Meta cached a local-scale lock under the plant. Rebase that lock after
        // detaching, or the next Move restores the old local scale and enlarges the fruit.
        GetComponent<GrabFreeTransformer>()?.Initialize(grabbable);
        body.useGravity = true;
        if (outline != null) outline.enabled = false;
        if (harvestLabel != null) harvestLabel.enabled = false;
    }

    private void Update()
    {
        if (stored || grabbable == null) return;
        bool held = grabbable.SelectingPointsCount > 0;
        if (GardenMenuController.GardenSuspended)
        {
            if (picked && !held) requireNewGrab = true;
            return;
        }
        if (held)
        {
            requireNewGrab = false;
            recoveryRemaining = -1f;
            return;
        }
        if (!picked) return;
        if (!requireNewGrab)
        {
            if (basket == null && Time.unscaledTime >= nextBasketSearch)
            {
                basket = FindFirstObjectByType<TomatoHarvestBasket>();
                nextBasketSearch = Time.unscaledTime + 1f;
            }
            // Allow a released tomato to fall into the tray before recovering it.
            if (basket != null && basket.Contains(transform.position)) basket.Collect(this, source);
            if (stored) return;
        }
        if (recoveryRemaining < 0f) recoveryRemaining = Mathf.Max(0.1f, looseRecoveryDelay);
        recoveryRemaining -= Time.deltaTime;
        if (recoveryRemaining <= 0f) ReturnToPlant();
    }

    private void ReturnToPlant()
    {
        if (plantAttachment == null) return;
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        body.useGravity = false;
        body.isKinematic = true;
        // Restore the local pose so the fruit follows a pot moved since it was picked.
        transform.SetParent(plantAttachment, false);
        transform.SetLocalPositionAndRotation(attachedLocalPosition, attachedLocalRotation);
        transform.localScale = attachedLocalScale;
        body.position = transform.position;
        body.rotation = transform.rotation;
        GetComponent<GrabFreeTransformer>()?.Initialize(grabbable);
        picked = false;
        requireNewGrab = false;
        recoveryRemaining = -1f;
        if (outline != null) outline.enabled = true;
        if (harvestLabel != null) harvestLabel.enabled = true;
    }

    public void StoreIn(Transform tray)
    {
        stored = true;
        if (harvestLabel != null) harvestLabel.enabled = false;
        if (outline != null) outline.enabled = false;
        grabbable.enabled = false;
        foreach (GrabInteractable grab in GetComponentsInChildren<GrabInteractable>()) grab.enabled = false;
        foreach (HandGrabInteractable grab in GetComponentsInChildren<HandGrabInteractable>()) grab.enabled = false;
        foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        body.useGravity = false;
        body.isKinematic = true;
        // The visible tray can be offset from its root. Follow the tray, then seat
        // the model on its real floor rather than freezing it at the release height.
        transform.SetParent(tray, true);
        TomatoHarvestBasket harvestTray = tray.GetComponent<TomatoHarvestBasket>();
        if (harvestTray != null && harvestTray.TryGetRestingPoint(out Vector3 support)) RestOn(support);
    }

    private void RestOn(Vector3 support)
    {
        MeshFilter model = GetComponentInChildren<MeshFilter>();
        if (model == null || model.sharedMesh == null) return;
        float bottom = model.GetComponent<MeshRenderer>().bounds.min.y;
        if (model.sharedMesh.isReadable)
        {
            // Actual vertices avoid a gap caused by the rotated renderer bounds.
            bottom = float.PositiveInfinity;
            foreach (Vector3 vertex in model.sharedMesh.vertices)
                bottom = Mathf.Min(bottom, model.transform.TransformPoint(vertex).y);
        }
        Vector3 position = transform.position;
        position.x = support.x;
        position.z = support.z;
        position.y += support.y - bottom + 0.0005f;
        transform.position = position;
    }

    private void OnDestroy()
    {
        if (grabbable != null) grabbable.WhenPointerEventRaised -= OnPointerEvent;
    }
}
