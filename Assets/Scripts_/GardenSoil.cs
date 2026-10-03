using UnityEngine;

/// <summary>Visual state of the Ground mesh in a pot. Water is measured in millilitres.</summary>
[DisallowMultipleComponent]
public sealed class GardenSoil : MonoBehaviour
{
    [SerializeField] private Renderer groundRenderer;
    [SerializeField, Min(0f)] private float wetThresholdMillilitres = 100f;
    [SerializeField] private Color dryColor = new(0.62f, 0.43f, 0.25f, 1f);
    [SerializeField] private Color wetColor = new(0.23f, 0.14f, 0.09f, 1f);
    [SerializeField] private Color waterloggedColor = new(0.12f, 0.45f, 0.65f, 1f);
    [Header("Waterlogged surface")]
    [SerializeField, Tooltip("Optional override; otherwise uses Resources/GardenWater/WaterPuddle.")]
    private Material puddleMaterial;
    [SerializeField, Min(0.0001f)] private float puddleSurfaceOffset = 0.0015f;
    [SerializeField, Min(0.1f)] private float puddleFadeSeconds = 0.8f;
    [SerializeField] private float receivedMillilitres;
    private bool isWaterlogged;
    private Mesh puddleMesh;
    private MeshRenderer puddleRenderer;
    private MaterialPropertyBlock soilProperties;
    private MaterialPropertyBlock puddleProperties;
    private float visiblePuddle;
    private bool puddleCreationAttempted;
    private static readonly int WaterAmountId = Shader.PropertyToID("_WaterAmount");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public float WetThresholdMillilitres => wetThresholdMillilitres;
    public float ReceivedMillilitres => receivedMillilitres;
    public bool IsWet => receivedMillilitres >= wetThresholdMillilitres;

    private void Awake()
    {
        if (groundRenderer == null) groundRenderer = GetComponent<Renderer>();
        ApplyColor();
    }

    public void SetReceivedMillilitres(float millilitres)
    {
        SetWaterFeedback(millilitres, false);
    }

    public void SetWaterFeedback(float millilitres, bool waterlogged)
    {
        receivedMillilitres = Mathf.Max(0f, millilitres);
        isWaterlogged = waterlogged;
        if (waterlogged && Application.isPlaying) EnsurePuddle();
        ApplyColor();
    }

    private void LateUpdate()
    {
        if (puddleRenderer == null) return;
        visiblePuddle = Mathf.MoveTowards(visiblePuddle, isWaterlogged ? 1f : 0f,
            Time.deltaTime / Mathf.Max(0.1f, puddleFadeSeconds));
        puddleRenderer.enabled = visiblePuddle > 0f;
        if (!puddleRenderer.enabled) return;

        // Ground has a very small Y scale. Convert the surface offset from metres to local units.
        float verticalScale = transform.TransformVector(Vector3.up).magnitude;
        puddleRenderer.transform.localPosition = Vector3.up *
            (puddleSurfaceOffset / Mathf.Max(0.000001f, verticalScale));
        puddleProperties.SetFloat(WaterAmountId, visiblePuddle);
        Color tint = waterloggedColor;
        tint.a = 0.65f;
        puddleProperties.SetColor(BaseColorId, tint);
        puddleRenderer.SetPropertyBlock(puddleProperties);
    }

    private void EnsurePuddle()
    {
        if (puddleCreationAttempted) return;
        puddleCreationAttempted = true;
        MeshFilter surface = GetComponent<MeshFilter>();
        Material material = puddleMaterial != null ? puddleMaterial :
            Resources.Load<Material>("GardenWater/WaterPuddle");
        if (surface == null || surface.sharedMesh == null || material == null)
        {
            Debug.LogWarning("Water puddle needs the soil mesh and the GardenWater/WaterPuddle material.", this);
            return;
        }

        Bounds bounds = surface.sharedMesh.bounds;
        Vector3 centre = new(bounds.center.x, bounds.max.y, bounds.center.z);
        float halfWidth = bounds.extents.x * 0.9f;
        float halfDepth = bounds.extents.z * 0.9f;
        puddleMesh = new Mesh { name = "Garden water puddle", hideFlags = HideFlags.DontSave };
        puddleMesh.vertices = new[]
        {
            centre + new Vector3(-halfWidth, 0f, -halfDepth),
            centre + new Vector3(-halfWidth, 0f, halfDepth),
            centre + new Vector3(halfWidth, 0f, halfDepth),
            centre + new Vector3(halfWidth, 0f, -halfDepth)
        };
        puddleMesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        puddleMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        puddleMesh.RecalculateNormals();
        puddleMesh.RecalculateBounds();

        // A visual-only surface: no collider to intercept pouring, seed release or Meta grabs.
        GameObject puddle = new("Waterlogged surface");
        puddle.layer = gameObject.layer;
        puddle.transform.SetParent(transform, false);
        puddle.AddComponent<MeshFilter>().sharedMesh = puddleMesh;
        puddleRenderer = puddle.AddComponent<MeshRenderer>();
        puddleRenderer.sharedMaterial = material;
        puddleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        puddleRenderer.receiveShadows = false;
        puddleRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        puddleRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        puddleRenderer.enabled = false;
        puddleProperties = new MaterialPropertyBlock();
    }

    private void ApplyColor()
    {
        if (groundRenderer == null) return;
        Color color = isWaterlogged && puddleRenderer == null ? waterloggedColor : IsWet ? wetColor : dryColor;
        soilProperties ??= new MaterialPropertyBlock();
        groundRenderer.GetPropertyBlock(soilProperties);
        soilProperties.SetColor(BaseColorId, color);
        soilProperties.SetColor("_Color", color);
        groundRenderer.SetPropertyBlock(soilProperties);
    }

    private void OnDisable()
    {
        if (puddleRenderer != null) puddleRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (puddleRenderer != null) Destroy(puddleRenderer.gameObject);
        if (puddleMesh != null) Destroy(puddleMesh);
    }

    private void OnValidate()
    {
        wetThresholdMillilitres = Mathf.Max(0f, wetThresholdMillilitres);
        puddleSurfaceOffset = Mathf.Max(0.0001f, puddleSurfaceOffset);
        puddleFadeSeconds = Mathf.Max(0.1f, puddleFadeSeconds);
        if (groundRenderer == null) groundRenderer = GetComponent<Renderer>();
        ApplyColor();
    }
}
