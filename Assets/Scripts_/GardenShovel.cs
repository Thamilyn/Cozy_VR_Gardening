using Oculus.Interaction;
using UnityEngine;

/// <summary>Carry one visible scoop from SoilPot to an empty pot using Meta hand/controller grabs.</summary>
[DisallowMultipleComponent]
public sealed class GardenShovel : MonoBehaviour
{
    [SerializeField, Tooltip("Centre of the blade in the existing Shovel prefab's local space.")]
    private Vector3 localScoopPoint = new(0f, 0.05f, -0.025f);
    [SerializeField] private Vector3 soilLoadSize = new(0.075f, 0.035f, 0.09f);
    [SerializeField, Min(0.1f)] private float recoveryDelaySeconds = 2f;
    private Grabbable grabbable;
    private Rigidbody body;
    private GardenSoilSource[] sources;
    private GardenPot[] pots;
    private GameObject soilLoad;
    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private float droppedSeconds;

    public bool HasSoil => soilLoad != null && soilLoad.activeSelf;
    public Vector3 ScoopPoint => transform.TransformPoint(localScoopPoint);

    private void Awake()
    {
        GardenGrabSetup.ConfigureGrabbable(gameObject);
        grabbable = GetComponent<Grabbable>();
        grabbable.MaxGrabPoints = 1;
        body = GetComponent<Rigidbody>();
        body.mass = 0.2f;
        GardenObjectLabel.Create(gameObject, "Shovel", Vector3.up * 0.12f);
    }

    private void Start()
    {
        startingPosition = transform.position;
        startingRotation = transform.rotation;
        sources = FindObjectsByType<GardenSoilSource>(FindObjectsSortMode.None);
        pots = FindObjectsByType<GardenPot>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        if (GardenMenuController.GardenSuspended) return;
        bool held = grabbable.SelectingPointsCount > 0;
        if (!held)
        {
            RecoverIfDropped();
            return;
        }
        droppedSeconds = 0f;
        if (!GronnyTutorial.Allows(GardenTutorialAction.FillSoil)) return;

        if (HasSoil)
        {
            foreach (GardenPot pot in pots)
            {
                if (pot == null || !pot.isActiveAndEnabled) continue;
                soilLoad.SetActive(false);
                return;
            }
        }
        else
        {
            foreach (GardenSoilSource source in sources)
            {
                if (source == null || !source.ContainsScoopPoint(ScoopPoint)) continue;
                ShowSoilLoad(source.SoilMaterial);
                return;
            }
        }
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
            soilLoad.transform.localPosition = localScoopPoint;
            Vector3 scale = transform.lossyScale;
            soilLoad.transform.localScale = new Vector3(
                soilLoadSize.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                soilLoadSize.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                soilLoadSize.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
        }
        soilLoad.GetComponent<Renderer>().sharedMaterial = material;
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
