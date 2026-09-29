using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Locomotion;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Turns a grabbable object into a watering can with its own finite water supply.
/// The can pours when held and tilted. GardenPot receivers get the measured amount in litres.
/// </summary>
[DisallowMultipleComponent]
public sealed class WateringCan : MonoBehaviour
{
    private const string PlantTag = "Plants";
    private const string HeldLayerName = "GardenHeldObject";

    [Header("Water supply")]
    [SerializeField, Min(0.01f)] private float capacityLitres = 2f;
    [SerializeField, Min(0.001f)] private float litresPerSecond = 0.2f;
    [SerializeField] private bool startFull = true;

    [Header("Pouring")]
    [SerializeField, Range(-1f, 1f)]
    [Tooltip("The can pours when the dot product between its up direction and world up is at or below this value.")]
    private float pourTiltThreshold = 0.35f;
    [SerializeField, Min(0.1f)] private float maximumPourDistance = 2f;
    [SerializeField, Min(0.001f)] private float streamRadius = 0.025f;
    [SerializeField, Min(0.001f)] private float streamWidth = 0.015f;
    [SerializeField] private Color streamColor = new(0.25f, 0.7f, 1f, 0.85f);

    [Header("Refill input")]
    [SerializeField]
    [Tooltip("Button.Two is B on the right Touch controller. Y on the left opens the calendar.")]
    private OVRInput.Button refillButton = OVRInput.Button.Two;
    [SerializeField] private bool requireCanToBeHeldForRefill = true;

    [Header("Optional references")]
    [SerializeField] private Transform spout;
    [SerializeField] private LineRenderer waterStream;

    [Header("Events")]
    [SerializeField] private UnityEvent<float> onWaterLevelChanged = new();
    [SerializeField] private UnityEvent<GameObject> onPlantWatered = new();
    [SerializeField] private UnityEvent onRefilled = new();

    private readonly RaycastHit[] _raycastHits = new RaycastHit[16];
    private Grabbable _grabbable;
    private Material _runtimeStreamMaterial;
    private float _currentWaterLitres;
    private GameObject _lastWateredPlant;
    private readonly Dictionary<GameObject, int> _restingColliderLayers = new();
    private bool _usingHeldLayer;

    public float CurrentWaterLitres => _currentWaterLitres;
    public float CapacityLitres => capacityLitres;
    public float WaterNormalized => capacityLitres > 0f ? _currentWaterLitres / capacityLitres : 0f;
    public bool IsPouring { get; private set; }

    private void Awake()
    {
        EnsureGardenCanColliders();
        // Set the resting state before Meta saves it when the can is first grabbed.
        GardenGrabSetup.ConfigureGrabbable(gameObject);
        _grabbable = GetComponent<Grabbable>();
        _grabbable.ForceKinematicDisabled = true;
        foreach (HandGrabInteractable handGrab in GetComponentsInChildren<HandGrabInteractable>(true))
        {
            // Keep a pinch held when one finger relaxes while another still holds.
            handGrab.InjectPinchGrabRules(GrabbingRule.DefaultPinchRule);
            handGrab.Slippiness = 0f;
        }
        EnsureSpout();
        EnsureWaterStream();

        _currentWaterLitres = startFull ? capacityLitres : 0f;
        SetStreamVisible(false);
    }

    private void Update()
    {
        bool isHeld = _grabbable != null && _grabbable.SelectingPointsCount > 0;
        UpdateHeldCollisionState(isHeld);

        if (OVRInput.GetDown(refillButton, OVRInput.Controller.RTouch) &&
            (!requireCanToBeHeldForRefill || isHeld))
        {
            Refill();
        }

        bool tiltedToPour = Vector3.Dot(transform.up, Vector3.up) <= pourTiltThreshold;
        IsPouring = isHeld && tiltedToPour && _currentWaterLitres > 0f;

        if (!IsPouring)
        {
            _lastWateredPlant = null;
            SetStreamVisible(false);
            return;
        }

        float pouredLitres = Mathf.Min(_currentWaterLitres, litresPerSecond * Time.deltaTime);
        _currentWaterLitres -= pouredLitres;
        onWaterLevelChanged.Invoke(WaterNormalized);

        Vector3 streamStart = spout.position;
        Vector3 streamEnd = streamStart + Vector3.down * maximumPourDistance;
        if (TryGetFirstExternalHit(streamStart, out RaycastHit hit))
        {
            streamEnd = hit.point;
            DeliverWater(hit.collider, pouredLitres);
        }
        else
        {
            _lastWateredPlant = null;
        }

        DrawWaterStream(streamStart, streamEnd);
    }

    private void OnEnable()
    {
        if (_grabbable != null)
            _grabbable.WhenPointerEventRaised += HandleGrabEvent;
    }

    private void OnDisable()
    {
        if (_grabbable != null)
            _grabbable.WhenPointerEventRaised -= HandleGrabEvent;
        UpdateHeldCollisionState(false);
        SetStreamVisible(false);
    }

    private void HandleGrabEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select || evt.Type == PointerEventType.Unselect ||
            evt.Type == PointerEventType.Cancel)
            UpdateHeldCollisionState(_grabbable.SelectingPointsCount > 0);
    }

    private void UpdateHeldCollisionState(bool isHeld)
    {
        if (isHeld == _usingHeldLayer) return;

        if (isHeld)
        {
            int heldLayer = LayerMask.NameToLayer(HeldLayerName);
            if (heldLayer < 0)
            {
                Debug.LogError($"Add the {HeldLayerName} physics layer to prevent held tools moving the VR player.", this);
                return;
            }

            // Meta's locomotor uses capsule/sphere casts, including a ground cast
            // below the head. IgnoreCollision alone cannot exclude a held can
            // from those casts; otherwise it can become the player's floor.
            int playerMask = ~(1 << heldLayer);
            foreach (Oculus.Interaction.Locomotion.CharacterController player in
                     FindObjectsByType<Oculus.Interaction.Locomotion.CharacterController>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                player.LayerMask = player.LayerMask.value & playerMask;
            foreach (WallPenetrationTunneling walls in FindObjectsByType<WallPenetrationTunneling>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                walls.LayerMask = walls.LayerMask.value & playerMask;

            Rigidbody body = GetComponent<Rigidbody>();
            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            {
                if (collider.attachedRigidbody != body) continue;
                GameObject colliderObject = collider.gameObject;
                if (!_restingColliderLayers.ContainsKey(colliderObject))
                    _restingColliderLayers.Add(colliderObject, colliderObject.layer);
                colliderObject.layer = heldLayer;
            }
        }
        else
        {
            foreach (KeyValuePair<GameObject, int> entry in _restingColliderLayers)
                if (entry.Key != null) entry.Key.layer = entry.Value;
            _restingColliderLayers.Clear();
        }
        _usingHeldLayer = isHeld;
    }

    /// <summary>
    /// Restores the bottle to full capacity. This can also be bound to a Unity UI button.
    /// </summary>
    public void Refill()
    {
        _currentWaterLitres = capacityLitres;
        onWaterLevelChanged.Invoke(WaterNormalized);
        onRefilled.Invoke();
    }

    private bool TryGetFirstExternalHit(Vector3 origin, out RaycastHit closestHit)
    {
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            streamRadius,
            Vector3.down,
            _raycastHits,
            maximumPourDistance,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide);

        float closestDistance = float.PositiveInfinity;
        closestHit = default;
        bool foundHit = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit candidate = _raycastHits[i];
            if (candidate.collider == null || candidate.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (candidate.distance < closestDistance)
            {
                closestDistance = candidate.distance;
                closestHit = candidate;
                foundHit = true;
            }
        }

        return foundHit;
    }

    private void DeliverWater(Collider hitCollider, float amountLitres)
    {
        GardenPot pot = hitCollider.GetComponentInParent<GardenPot>();
        GameObject plant = pot != null ? pot.gameObject : FindTaggedParent(hitCollider.transform, PlantTag);
        PlantWaterReceiver receiver = pot != null
            ? pot.GetComponent<PlantWaterReceiver>()
            : plant != null ? plant.GetComponent<PlantWaterReceiver>() : null;
        if (plant == null || receiver == null)
        {
            _lastWateredPlant = null;
            return;
        }

        receiver.ReceiveWater(amountLitres);

        if (_lastWateredPlant != plant)
        {
            _lastWateredPlant = plant;
            onPlantWatered.Invoke(plant);
        }
    }

    private static GameObject FindTaggedParent(Transform candidate, string requiredTag)
    {
        while (candidate != null)
        {
            if (candidate.CompareTag(requiredTag))
            {
                return candidate.gameObject;
            }

            candidate = candidate.parent;
        }

        return null;
    }

    private void EnsureSpout()
    {
        if (spout != null)
        {
            return;
        }

        GameObject spoutObject = new("Water Spout");
        spoutObject.transform.SetParent(transform, false);
        spout = spoutObject.transform;

        if (TryGetGardenCanMesh(out MeshFilter canMesh, out float modelScale, out float frontSign))
        {
            // Outlet centre measured from WateringCup.fbx, just outside the rose face.
            // Mesh bounds account for the importer's units and mirrored forward axis.
            spout.position = canMesh.transform.TransformPoint(
                new Vector3(0f, 0.388f, frontSign * 0.409f) * modelScale);
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            spout.localPosition = Vector3.up * 0.25f;
            return;
        }

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 up = transform.up;
        Vector3 extents = combinedBounds.extents;
        float extentAlongUp =
            Mathf.Abs(up.x) * extents.x +
            Mathf.Abs(up.y) * extents.y +
            Mathf.Abs(up.z) * extents.z;
        spout.position = combinedBounds.center + up * extentAlongUp;
    }

    private bool TryGetGardenCanMesh(out MeshFilter canMesh, out float modelScale, out float frontSign)
    {
        canMesh = GetComponent<MeshFilter>();
        modelScale = 1f;
        frontSign = 1f;
        // This model has a separate upper handle; the placeholder bottle does not.
        if (canMesh == null || canMesh.sharedMesh == null || transform.Find("Handle") == null)
            return false;

        Bounds bounds = canMesh.sharedMesh.bounds;
        float front = Mathf.Abs(bounds.max.z) > Mathf.Abs(bounds.min.z) ? bounds.max.z : bounds.min.z;
        frontSign = Mathf.Sign(front);
        modelScale = Mathf.Abs(front) / 0.46441045f;
        return modelScale > 0f;
    }

    private void EnsureGardenCanColliders()
    {
        if (!TryGetGardenCanMesh(out MeshFilter canMesh, out float modelScale, out float frontSign))
            return;

        // A single hull over the whole model fills the space between the body,
        // handles and nozzle. Use separate shapes so it rests on visible geometry.
        BoxCollider bodyCollider = GetComponent<BoxCollider>();
        if (bodyCollider == null) bodyCollider = gameObject.AddComponent<BoxCollider>();
        bodyCollider.center = new Vector3(0f, 0.1885f, 0f) * modelScale;
        bodyCollider.size = new Vector3(0.3645f, 0.377f, 0.4347f) * modelScale;
        bodyCollider.isTrigger = false;

        if (transform.Find("Spout Physics") != null) return;
        GameObject nozzle = new("Spout Physics");
        nozzle.transform.SetParent(canMesh.transform, false);
        Vector3 start = new Vector3(0f, 0.075f, frontSign * 0.2f) * modelScale;
        Vector3 end = new Vector3(0f, 0.345f, frontSign * 0.387f) * modelScale;
        nozzle.transform.localPosition = (start + end) * 0.5f;
        nozzle.transform.localRotation = Quaternion.FromToRotation(Vector3.up, end - start);
        CapsuleCollider tube = nozzle.AddComponent<CapsuleCollider>();
        tube.radius = 0.025f * modelScale;
        tube.height = Vector3.Distance(start, end) + tube.radius * 2f;

        GameObject rose = new("Rose Physics");
        rose.transform.SetParent(canMesh.transform, false);
        rose.transform.localPosition = new Vector3(0f, 0.368f, frontSign * 0.405f) * modelScale;
        rose.transform.localRotation = Quaternion.FromToRotation(Vector3.up,
            new Vector3(0f, 0.8f, frontSign * 0.6f));
        BoxCollider roseCollider = rose.AddComponent<BoxCollider>();
        roseCollider.size = new Vector3(0.15f, 0.025f, 0.125f) * modelScale;
    }

    private void EnsureWaterStream()
    {
        if (waterStream == null)
        {
            GameObject streamObject = new("Water Stream");
            streamObject.transform.SetParent(transform, false);
            waterStream = streamObject.AddComponent<LineRenderer>();
        }

        waterStream.useWorldSpace = true;
        waterStream.positionCount = 2;
        waterStream.startWidth = streamWidth;
        waterStream.endWidth = streamWidth * 0.65f;
        waterStream.startColor = streamColor;
        waterStream.endColor = new Color(streamColor.r, streamColor.g, streamColor.b, 0.35f);
        waterStream.numCapVertices = 4;

        if (waterStream.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _runtimeStreamMaterial = new Material(shader)
                {
                    name = "Water Stream (Runtime)",
                    hideFlags = HideFlags.DontSave
                };
                waterStream.sharedMaterial = _runtimeStreamMaterial;
            }
        }
    }

    private void DrawWaterStream(Vector3 start, Vector3 end)
    {
        SetStreamVisible(true);
        waterStream.SetPosition(0, start);
        waterStream.SetPosition(1, end);
    }

    private void SetStreamVisible(bool visible)
    {
        if (waterStream != null)
        {
            waterStream.enabled = visible;
        }
    }

    private void OnDestroy()
    {
        if (_runtimeStreamMaterial != null)
        {
            Destroy(_runtimeStreamMaterial);
        }
    }

    private void OnValidate()
    {
        capacityLitres = Mathf.Max(0.01f, capacityLitres);
        litresPerSecond = Mathf.Max(0.001f, litresPerSecond);
        maximumPourDistance = Mathf.Max(0.1f, maximumPourDistance);
        streamRadius = Mathf.Max(0.001f, streamRadius);
        streamWidth = Mathf.Max(0.001f, streamWidth);
        _currentWaterLitres = Mathf.Clamp(_currentWaterLitres, 0f, capacityLitres);
    }
}
