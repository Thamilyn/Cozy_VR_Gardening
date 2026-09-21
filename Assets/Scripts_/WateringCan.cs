using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Turns a grabbable object into a watering can with its own finite water supply.
/// The can pours when held and tilted, and only Plants-tagged objects receive water.
/// </summary>
[DisallowMultipleComponent]
public sealed class WateringCan : MonoBehaviour
{
    private const string PlantTag = "Plants";

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
    [Tooltip("Button.Two is B on the right Touch controller and Y on the left Touch controller.")]
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

    public float CurrentWaterLitres => _currentWaterLitres;
    public float CapacityLitres => capacityLitres;
    public float WaterNormalized => capacityLitres > 0f ? _currentWaterLitres / capacityLitres : 0f;
    public bool IsPouring { get; private set; }

    private void Awake()
    {
        _grabbable = GetComponent<Grabbable>();
        EnsureSpout();
        EnsureWaterStream();

        _currentWaterLitres = startFull ? capacityLitres : 0f;
        SetStreamVisible(false);
    }

    private void Update()
    {
        bool isHeld = _grabbable != null && _grabbable.SelectingPointsCount > 0;

        if (OVRInput.GetDown(refillButton) && (!requireCanToBeHeldForRefill || isHeld))
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
        GameObject plant = FindTaggedParent(hitCollider.transform, PlantTag);
        if (plant == null)
        {
            _lastWateredPlant = null;
            return;
        }

        PlantWaterReceiver receiver = plant.GetComponent<PlantWaterReceiver>();
        if (receiver == null)
        {
            receiver = plant.AddComponent<PlantWaterReceiver>();
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
