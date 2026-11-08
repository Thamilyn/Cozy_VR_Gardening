using Oculus.Interaction;
using UnityEngine;

/// <summary>Carry and tip one visible scoop over a planted seed using Meta hand/controller grabs.</summary>
[DisallowMultipleComponent]
public sealed class GardenShovel : MonoBehaviour
{
    [SerializeField, Tooltip("Centre of the blade in the existing Shovel prefab's local space.")]
    private Vector3 localScoopPoint = new(0f, 0.019f, 0.195f);
    [SerializeField] private Vector3 soilLoadSize = new(0.075f, 0.035f, 0.09f);
    [SerializeField, Tooltip("Brown patch of the existing Soil texture atlas; the remaining atlas area is black.")]
    private Rect soilTextureRegion = new(0.02f, 0.02f, 0.58f, 0.96f);
    [SerializeField, Min(0.01f), Tooltip("Blade reach around the scoop point, in metres.")]
    private float scoopRadius = 0.05f;
    [SerializeField, Min(0.005f), Tooltip("A short, gentle stroke near SoilPot loads the blade. No digging or button press is needed.")]
    private float minimumScoopTravel = 0.015f;
    [SerializeField, Tooltip("Normal pointing out of the loaded face of the blade, in shovel local space.")]
    private Vector3 localBladeUp = Vector3.up;
    [SerializeField, Range(30f, 80f)] private float pourTiltDegrees = 55f;
    [SerializeField, Min(0.1f)] private float pourHoldSeconds = 0.35f;
    [SerializeField, Min(0.1f)] private float recoveryDelaySeconds = 2f;
    private Grabbable grabbable;
    private Rigidbody body;
    private GardenSoilSource[] sources;
    private GardenPot[] pots;
    private GameObject soilLoad;
    private MaterialPropertyBlock soilLoadProperties;
    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private float droppedSeconds;
    private SeedsController seeds;
    private GardenPot pourTarget;
    private float pouringSeconds;
    private GardenSoilSource scoopSource;
    private Vector3 previousScoopPoint;
    private bool hasPreviousScoopPoint;
    private float scoopTravel;
    private float scoopStartedAt;
    private float nextTargetRefresh;
    private GardenObjectLabel nameLabel;

    public bool HasSoil => soilLoad != null && soilLoad.activeSelf;
    public Vector3 ScoopPoint => transform.TransformPoint(localScoopPoint);

    private void Awake()
    {
        GardenGrabSetup.ConfigureGrabbable(gameObject);
        grabbable = GetComponent<Grabbable>();
        grabbable.MaxGrabPoints = 1;
        body = GetComponent<Rigidbody>();
        body.mass = 0.2f;
        nameLabel = GardenObjectLabel.Create(gameObject, "Shovel", Vector3.up * 0.12f);
    }

    private void Start()
    {
        startingPosition = transform.position;
        startingRotation = transform.rotation;
        RefreshTargets();
    }

    private void OnEnable()
    {
        if (grabbable == null) grabbable = GetComponent<Grabbable>();
        if (grabbable != null) grabbable.WhenPointerEventRaised += OnPointerEvent;
        RefreshTargets();
        ResetScoop();
    }

    private void OnPointerEvent(PointerEvent pointerEvent)
    {
        if (pointerEvent.Type == PointerEventType.Select || pointerEvent.Type == PointerEventType.Unselect ||
            pointerEvent.Type == PointerEventType.Cancel)
        {
            ResetPour();
            ResetScoop();
        }
    }

    private void Update()
    {
        if (GardenMenuController.GardenSuspended)
        {
            ResetPour();
            ResetScoop();
            return;
        }
        bool held = grabbable.SelectingPointsCount > 0;
        if (!held)
        {
            ResetPour();
            ResetScoop();
            RecoverIfDropped();
            return;
        }
        droppedSeconds = 0f;
        if (!GronnyTutorial.Allows(GardenTutorialAction.FillSoil))
        {
            ResetPour();
            ResetScoop();
            return;
        }

        if (sources == null || pots == null || Time.unscaledTime >= nextTargetRefresh) RefreshTargets();
        if (HasSoil)
        {
            ResetScoop();
            UpdatePour();
        }
        else
        {
            ResetPour();
            UpdateScoop();
        }
    }

    private void RefreshTargets()
    {
        sources = FindObjectsByType<GardenSoilSource>(FindObjectsSortMode.None);
        pots = FindObjectsByType<GardenPot>(FindObjectsSortMode.None);
        seeds = FindFirstObjectByType<SeedsController>();
        nextTargetRefresh = Time.unscaledTime + 0.5f;
    }

    private void UpdateScoop()
    {
        Vector3 point = ScoopPoint;
        if (!hasPreviousScoopPoint)
        {
            previousScoopPoint = point;
            hasPreviousScoopPoint = true;
            return;
        }
        float travel = Vector3.Distance(previousScoopPoint, point);
        GardenSoilSource target = null;
        // Large tracking jumps do not count as a gardening gesture.
        if (travel <= 0.25f)
            foreach (GardenSoilSource source in sources)
                if (source != null && source.IntersectsScoopPath(previousScoopPoint, point, scoopRadius))
                {
                    target = source;
                    break;
                }
        previousScoopPoint = point;
        if (target == null)
        {
            scoopSource = null;
            scoopTravel = 0f;
            return;
        }
        if (scoopSource != target || Time.unscaledTime - scoopStartedAt > 0.8f)
        {
            scoopSource = target;
            scoopTravel = 0f;
            scoopStartedAt = Time.unscaledTime;
        }
        scoopTravel += travel;
        if (scoopTravel < minimumScoopTravel) return;
        ShowSoilLoad(target.SoilMaterial);
        target.RecordScoop();
        ResetScoop();
    }

    private void ResetScoop()
    {
        scoopSource = null;
        hasPreviousScoopPoint = false;
        scoopTravel = 0f;
    }

    private void UpdatePour()
    {
        GardenPot target = null;
        if (seeds != null)
            foreach (GardenPot pot in pots)
                if (pot != null && seeds.NeedsSoilCover(pot) && pot.CanReceiveSoilAt(ScoopPoint, scoopRadius))
                {
                    target = pot;
                    break;
                }
        if (target == null) { ResetPour(); return; }
        if (pourTarget != target)
        {
            ResetPour();
            pourTarget = target;
        }

        float upright = Vector3.Dot(transform.TransformDirection(localBladeUp.normalized), Vector3.up);
        // The held tilt and continuous dwell express intent, even when the blade
        // enters already tilted. Requiring a level pose here can trap the load.
        if (upright > Mathf.Cos(pourTiltDegrees * Mathf.Deg2Rad))
        {
            pouringSeconds = 0f;
            return;
        }
        pouringSeconds += Time.deltaTime;
        if (pouringSeconds < pourHoldSeconds) return;
        if (seeds.TryCoverSeed(target))
        {
            soilLoad.SetActive(false);
            if (nameLabel != null) nameLabel.enabled = false;
        }
        ResetPour();
        ResetScoop();
    }

    private void ResetPour()
    {
        pourTarget = null;
        pouringSeconds = 0f;
    }

    private void OnDisable()
    {
        if (grabbable != null) grabbable.WhenPointerEventRaised -= OnPointerEvent;
        ResetPour();
        ResetScoop();
    }

    private void OnValidate()
    {
        if (localBladeUp.sqrMagnitude < 0.0001f) localBladeUp = Vector3.up;
        pourHoldSeconds = Mathf.Max(0.1f, pourHoldSeconds);
        pourTiltDegrees = Mathf.Clamp(pourTiltDegrees, 30f, 80f);
        scoopRadius = Mathf.Max(0.01f, scoopRadius);
        minimumScoopTravel = Mathf.Max(0.005f, minimumScoopTravel);
    }

    private void ShowSoilLoad(Material material)
    {
        if (soilLoad == null)
        {
            soilLoad = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            soilLoad.name = "Carried soil";
            Collider collider = soilLoad.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            soilLoad.transform.SetParent(transform, false);
            soilLoad.transform.position = ScoopPoint + transform.TransformDirection(localBladeUp.normalized) * soilLoadSize.y * 0.5f;
            Vector3 scale = transform.lossyScale;
            soilLoad.transform.localScale = new Vector3(
                soilLoadSize.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                soilLoadSize.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                soilLoadSize.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
        }
        Renderer renderer = soilLoad.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        soilLoadProperties ??= new MaterialPropertyBlock();
        Vector4 textureTransform = new(soilTextureRegion.width, soilTextureRegion.height,
            soilTextureRegion.x, soilTextureRegion.y);
        // Remap the scoop's sphere UVs without changing the material shared by SoilPot.
        soilLoadProperties.SetVector("_BaseMap_ST", textureTransform);
        soilLoadProperties.SetVector("_MainTex_ST", textureTransform);
        renderer.SetPropertyBlock(soilLoadProperties);
        soilLoad.SetActive(true);
    }

    private void RecoverIfDropped()
    {
        if (transform.position.y >= startingPosition.y - 0.4f &&
            Vector3.Distance(transform.position, startingPosition) < 2.5f)
        {
            droppedSeconds = 0f;
            return;
        }
        droppedSeconds += Time.deltaTime;
        if (droppedSeconds < recoveryDelaySeconds) return;
        droppedSeconds = 0f;
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(startingPosition, startingRotation);
        body.position = startingPosition;
        body.rotation = startingRotation;
    }
}
