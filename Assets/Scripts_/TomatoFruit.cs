using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

/// <summary>Ripe fruit must be snipped before a Meta grab; tray delivery records the crop.</summary>
[DisallowMultipleComponent]
public sealed class TomatoFruit : MonoBehaviour
{
    public const float DiameterMetres = 0.065f;

    [SerializeField, Min(0.1f)] private float recoveryDelaySeconds = 2f;
    [SerializeField, Min(0.1f)] private float cutRecoveryDelaySeconds = 8f;

    private SeedItem source;
    private SeedsController controller;
    private Grabbable grabbable;
    private Rigidbody body;
    private TomatoHarvestBasket basket;
    private bool picked;
    private bool stemCut;
    private bool stored;
    private bool waitingAtRecoveryPoint;
    private float nextBasketSearch;
    private MeshRenderer outline;
    private bool requireNewGrab;
    private GardenObjectLabel harvestLabel;
    private Transform plantAttachment;
    private Vector3 attachedLocalPosition;
    private Quaternion attachedLocalRotation;
    private float recoveryRemaining = -1f;
    private float looseRecoveryDelay;
    private Vector3 stemLocalPosition;
    private SphereCollider fruitCollider;
    private LineRenderer stemGuide;
    private Material stemGuideMaterial;
    private Transform guideViewer;
    private float alignmentUntil = float.NegativeInfinity;
    private readonly Vector3[] guidePositions = new Vector3[24];
    private readonly Vector2[] guideCircle = new Vector2[24];

    public SeedItem Source => source;
    public bool IsStemCut => stemCut;
    public bool IsPickedUp => picked;
    public bool IsRipe
    {
        get
        {
            CalendarSystem calendar = controller != null ? controller.Calendar : null;
            CalendarSystem.PlantSave state = source != null && calendar != null
                ? calendar.FindPlant(source.SeedId) : null;
            return isActiveAndEnabled && grabbable != null && source != null && source.IsPlanted && source.Crop == SeedCrop.Tomato &&
                calendar != null && !calendar.IsAdvancing && state != null && state.soilCovered &&
                state.tomatoPhase == 5 && state.stage == GrowthStage.Mature.ToString() && !state.harvested;
        }
    }
    public bool CanCutStem => IsRipe && !stemCut && !stored && grabbable != null &&
        grabbable.SelectingPointsCount == 0;
    public bool CanStore => IsRipe && stemCut && picked && !stored && grabbable != null &&
        grabbable.SelectingPointsCount == 0 && !requireNewGrab && !GardenMenuController.GardenSuspended;
    public Vector3 StemPosition => transform.TransformPoint(stemLocalPosition);

    public void Initialize(SeedItem seed)
    {
        source = seed;
        controller = FindFirstObjectByType<SeedsController>();
        MeshFilter mesh = GetComponentInChildren<MeshFilter>();
        MeshRenderer renderer = mesh != null ? mesh.GetComponent<MeshRenderer>() : null;
        if (mesh == null || mesh.sharedMesh == null || renderer == null)
        {
            Debug.LogError("Harvestable tomato needs its own mesh and renderer.", this);
            return;
        }
        if (!TomatoFruitMesh.TryGetFruitBounds(mesh, renderer, out Bounds bounds))
        {
            Debug.LogError("Harvestable tomato needs a Tomato material distinct from its stalk.", this);
            return;
        }
        if (!TomatoFruitMesh.TryGetAttachmentPoint(mesh, renderer, bounds, out Vector3 attachment))
        {
            Debug.LogError("Harvestable tomato needs readable stalk geometry for its cutting point.", this);
            return;
        }
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
        fruitCollider = collider;
        GardenGrabSetup.ConfigureGrabbable(gameObject);
        grabbable = GetComponent<Grabbable>();
        grabbable.MaxGrabPoints = 1;
        grabbable.WhenPointerEventRaised += OnPointerEvent;
        body = GetComponent<Rigidbody>();
        body.mass = 0.08f;
        body.useGravity = false;
        body.isKinematic = true;
        grabbable.ForceKinematicDisabled = true;
        SetGrabEnabled(false);
        CreateOutline(mesh, renderer);
        harvestLabel = GardenObjectLabel.Create(gameObject, "Cut the stem", Vector3.up * 0.12f);
        CreateStemGuide();
    }

    internal bool IsOutsideFruit(Vector3 bladePoint)
    {
        if (fruitCollider == null) return false;
        Vector3 scale = transform.lossyScale;
        float radius = fruitCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)) + 0.002f;
        return (bladePoint - transform.TransformPoint(fruitCollider.center)).sqrMagnitude > radius * radius;
    }

    internal void ShowCutAlignment() => alignmentUntil = Time.unscaledTime + 0.12f;

    private void CreateStemGuide()
    {
        Shader shader = Resources.Load<Shader>("GardenHarvest/StemCutGuide");
        if (shader == null)
        {
            Debug.LogWarning("Missing stem cutting guide shader.", this);
            return;
        }
        stemGuideMaterial = new Material(shader);
        GameObject guide = new("Stem cutting guide");
        guide.layer = 2;
        guide.transform.SetParent(transform, false);
        stemGuide = guide.AddComponent<LineRenderer>();
        stemGuide.sharedMaterial = stemGuideMaterial;
        stemGuide.useWorldSpace = true;
        stemGuide.loop = true;
        stemGuide.positionCount = guidePositions.Length;
        stemGuide.startWidth = stemGuide.endWidth = 0.0018f;
        stemGuide.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        stemGuide.receiveShadows = false;
        for (int i = 0; i < guideCircle.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / guideCircle.Length;
            guideCircle[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
        guide.SetActive(false);
    }

    private void UpdateStemGuide()
    {
        if (stemGuide == null) return;
        bool visible = !GardenMenuController.GardenSuspended && CanCutStem;
        stemGuide.gameObject.SetActive(visible);
        if (!visible) return;
        if (guideViewer == null && Camera.main != null) guideViewer = Camera.main.transform;
        Vector3 right = guideViewer != null ? guideViewer.right : Vector3.right;
        Vector3 up = guideViewer != null ? guideViewer.up : Vector3.up;
        Vector3 centre = StemPosition;
        for (int i = 0; i < guidePositions.Length; i++)
            guidePositions[i] = centre + (right * guideCircle[i].x + up * guideCircle[i].y) * 0.012f;
        stemGuide.SetPositions(guidePositions);
        bool aligned = Time.unscaledTime <= alignmentUntil;
        Color color = aligned ? new Color(0.25f, 1f, 0.45f, 0.95f) : new Color(1f, 0.8f, 0.15f, 0.9f);
        stemGuide.startColor = stemGuide.endColor = color;
        if (harvestLabel != null) harvestLabel.SetCaption(aligned ? "Close Pruner" : "Cut the stem");
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
        if (stored || !stemCut || !IsRipe || GardenMenuController.GardenSuspended ||
            pointerEvent.Type != PointerEventType.Select) return;
        recoveryRemaining = -1f;
        waitingAtRecoveryPoint = false;
        looseRecoveryDelay = recoveryDelaySeconds;
        body.useGravity = true;
        picked = true;
        if (outline != null) outline.enabled = false;
        if (harvestLabel != null) harvestLabel.enabled = false;
    }

    internal bool TryCutStem(GardenPruner pruner)
    {
        if (!CanCutStem || GardenMenuController.GardenSuspended || pruner == null ||
            !pruner.IsCuttingStem(this)) return false;
        stemCut = true;
        if (stemGuide != null) stemGuide.gameObject.SetActive(false);
        DetachFromPlant();
        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        looseRecoveryDelay = Mathf.Max(0.1f, cutRecoveryDelaySeconds);
        recoveryRemaining = looseRecoveryDelay;
        SetGrabEnabled(true);
        return true;
    }

    private void DetachFromPlant()
    {
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

    private void SetGrabEnabled(bool available)
    {
        grabbable.enabled = available;
        foreach (GrabInteractable grab in GetComponentsInChildren<GrabInteractable>(true)) grab.enabled = available;
        foreach (HandGrabInteractable grab in GetComponentsInChildren<HandGrabInteractable>(true)) grab.enabled = available;
    }

    private void Update()
    {
        UpdateStemGuide();
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
        if (!stemCut || waitingAtRecoveryPoint) return;
        if (picked && !requireNewGrab)
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
        if (recoveryRemaining <= 0f) RecoverLooseFruit();
    }

    private void RecoverLooseFruit()
    {
        if (plantAttachment == null) return;
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        body.useGravity = false;
        body.isKinematic = true;
        // Return within reach, but never reconnect a severed stalk or require another cut.
        // Keep the independent world scale established when detaching from the scaled pot.
        Vector3 recoveryPosition = plantAttachment.TransformPoint(attachedLocalPosition);
        Vector3 awayFromPlant = Vector3.ProjectOnPlane(recoveryPosition - plantAttachment.position, Vector3.up);
        if (awayFromPlant.sqrMagnitude < 0.000001f) awayFromPlant = Vector3.right;
        recoveryPosition += awayFromPlant.normalized * DiameterMetres * 1.5f;
        transform.SetPositionAndRotation(recoveryPosition,
            plantAttachment.rotation * attachedLocalRotation);
        body.position = transform.position;
        body.rotation = transform.rotation;
        GetComponent<GrabFreeTransformer>()?.Initialize(grabbable);
        waitingAtRecoveryPoint = true;
        requireNewGrab = false;
        recoveryRemaining = -1f;
        if (outline != null) outline.enabled = !picked;
    }

    public void StoreIn(Transform tray)
    {
        if (!CanStore || tray == null) return;
        stored = true;
        if (harvestLabel != null) harvestLabel.enabled = false;
        if (outline != null) outline.enabled = false;
        SetGrabEnabled(false);
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
        if (stemGuideMaterial != null) Destroy(stemGuideMaterial);
    }
}
