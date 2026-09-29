using System;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

/// <summary>Grabbable seed. Tomato stages use replaceable prefab assets.</summary>
[DisallowMultipleComponent]
public sealed class SeedItem : MonoBehaviour
{
    [SerializeField] private SeedCrop crop;
    [SerializeField] private string seedId;
    [SerializeField] private Renderer seedRenderer;
    [Tooltip("Six ordered prefab slots: seed, sprout, young plant, flowering, green fruit, ripe fruit. Slot 0 may be empty to use SeedVisual.")]
    [SerializeField] private GameObject[] tomatoStagePrefabs = new GameObject[6];

    private Grabbable _grabbable;
    private bool _wasHeld;
    private bool _everHeld;
    private GameObject _sproutVisual;
    private GameObject _matureVisual;
    private readonly GameObject[] _tomatoStageInstances = new GameObject[6];

    public SeedCrop Crop => crop;
    public string SeedId => seedId;
    public bool IsPlanted { get; private set; }

    private void Awake()
    {
        if (string.IsNullOrEmpty(seedId)) seedId = Guid.NewGuid().ToString("N");
        GardenGrabSetup.ConfigureGrabbable(gameObject);
        _grabbable = GetComponent<Grabbable>();
        if (seedRenderer == null) seedRenderer = GetComponent<Renderer>();
        SetColor(seedRenderer, crop switch
        {
            SeedCrop.Tomato => new Color(0.7f, 0.25f, 0.16f),
            SeedCrop.Radish => new Color(0.8f, 0.18f, 0.45f),
            _ => new Color(0.65f, 0.8f, 0.24f)
        });
    }

    private void Update()
    {
        if (IsPlanted || _grabbable == null) return;
        bool held = _grabbable.SelectingPointsCount > 0;
        if (held) _everHeld = true;
        if (_wasHeld && !held && _everHeld)
        {
            SeedsController controller = FindFirstObjectByType<SeedsController>();
            if (controller != null) controller.TryPlant(this);
        }
        _wasHeld = held;
    }

    public void AssignId(string id) { if (!string.IsNullOrEmpty(id)) seedId = id; }

    public void PlantIn(GardenPot pot)
    {
        if (IsPlanted || pot == null) return;
        IsPlanted = true;
        transform.SetParent(pot.transform, false);
        transform.localPosition = pot.LocalPlantingPoint;
        transform.localRotation = Quaternion.identity;
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.isKinematic = true; }
        if (_grabbable != null) _grabbable.enabled = false;
        foreach (GrabInteractable interactable in GetComponentsInChildren<GrabInteractable>(true)) interactable.enabled = false;
        foreach (HandGrabInteractable interactable in GetComponentsInChildren<HandGrabInteractable>(true)) interactable.enabled = false;
        foreach (Collider collider in GetComponentsInChildren<Collider>(true)) collider.enabled = false;
    }

    public void ShowStage(GrowthStage stage)
    {
        if (crop == SeedCrop.Tomato)
        {
            int active = stage switch
            {
                GrowthStage.Sprout => 1,
                GrowthStage.YoungPlant => 2,
                GrowthStage.Flowering => 3,
                GrowthStage.GreenFruit => 4,
                GrowthStage.Mature => 5,
                _ => 0
            };
            for (int i = 0; i < _tomatoStageInstances.Length; i++)
            {
                if (i == active && _tomatoStageInstances[i] == null &&
                    tomatoStagePrefabs != null && i < tomatoStagePrefabs.Length && tomatoStagePrefabs[i] != null)
                {
                    _tomatoStageInstances[i] = Instantiate(tomatoStagePrefabs[i], transform);
                    _tomatoStageInstances[i].name = tomatoStagePrefabs[i].name;
                    _tomatoStageInstances[i].transform.localPosition = Vector3.zero;
                    _tomatoStageInstances[i].transform.localRotation = Quaternion.identity;
                }
                if (_tomatoStageInstances[i] != null) _tomatoStageInstances[i].SetActive(i == active);
            }
            if (seedRenderer != null) seedRenderer.enabled = active == 0 && _tomatoStageInstances[0] == null;
            return;
        }
        if (_sproutVisual == null) _sproutVisual = BuildPlant(false);
        if (_matureVisual == null) _matureVisual = BuildPlant(true);
        if (seedRenderer != null) seedRenderer.enabled = stage == GrowthStage.Seed;
        _sproutVisual.SetActive(stage == GrowthStage.Sprout);
        _matureVisual.SetActive(stage == GrowthStage.Mature);
    }

    private GameObject BuildPlant(bool mature)
    {
        GameObject root = new(mature ? "Mature plant placeholder" : "Sprout placeholder");
        root.transform.SetParent(transform, false);
        float height = mature ? 0.25f : 0.10f;
        AddPart(root.transform, PrimitiveType.Cylinder, "Stem", new Vector3(0f, height * 0.5f, 0f),
            new Vector3(0.012f, height * 0.5f, 0.012f), new Color(0.12f, 0.5f, 0.16f));
        Color leaf = crop == SeedCrop.Lettuce ? new Color(0.38f, 0.78f, 0.27f) : new Color(0.2f, 0.65f, 0.23f);
        float width = mature ? (crop == SeedCrop.Lettuce ? 0.17f : 0.11f) : 0.055f;
        AddPart(root.transform, PrimitiveType.Sphere, "Left leaf", new Vector3(-width * 0.55f, height * 0.77f, 0f),
            new Vector3(width, 0.025f, width * 0.5f), leaf);
        AddPart(root.transform, PrimitiveType.Sphere, "Right leaf", new Vector3(width * 0.55f, height * 0.85f, 0f),
            new Vector3(width, 0.025f, width * 0.5f), leaf);
        if (mature && crop != SeedCrop.Lettuce)
        {
            Color produce = crop == SeedCrop.Tomato ? new Color(0.9f, 0.13f, 0.08f) : new Color(0.85f, 0.16f, 0.45f);
            AddPart(root.transform, PrimitiveType.Sphere, crop == SeedCrop.Tomato ? "Tomato" : "Radish",
                new Vector3(0.025f, crop == SeedCrop.Tomato ? 0.15f : 0.035f, 0f),
                Vector3.one * (crop == SeedCrop.Tomato ? 0.075f : 0.065f), produce);
        }
        root.SetActive(false);
        return root;
    }

    private static void AddPart(Transform parent, PrimitiveType shape, string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) { collider.enabled = false; Destroy(collider); }
        SetColor(part.GetComponent<Renderer>(), color);
    }

    private static void SetColor(Renderer renderer, Color color)
    {
        if (renderer == null) return;
        MaterialPropertyBlock block = new();
        renderer.GetPropertyBlock(block);
        block.SetTexture("_BaseMap", Texture2D.whiteTexture);
        block.SetTexture("_MainTex", Texture2D.whiteTexture);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        renderer.SetPropertyBlock(block);
    }
}
