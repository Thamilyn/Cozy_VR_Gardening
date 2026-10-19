using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

/// <summary>Ripe fruit follows the plant until grabbed and counts only after release in the tray.</summary>
[DisallowMultipleComponent]
public sealed class TomatoFruit : MonoBehaviour
{
    public const float DiameterMetres = 0.065f;

    private SeedItem source;
    private Grabbable grabbable;
    private Rigidbody body;
    private TomatoHarvestBasket basket;
    private bool picked;
    private bool stored;
    private float nextBasketSearch;
    private MeshRenderer outline;

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
        if (stored || picked || pointerEvent.Type != PointerEventType.Select) return;
        picked = true;
        // Detach before Meta begins its grab transform, so its initial pose uses the
        // fruit's independent hierarchy rather than the scaled plant hierarchy.
        transform.SetParent(null, true);
        body.useGravity = true;
        if (outline != null) outline.enabled = false;
    }

    private void Update()
    {
        if (stored || grabbable == null) return;
        bool held = grabbable.SelectingPointsCount > 0;
        if (!picked || held) return;
        if (basket == null && Time.unscaledTime >= nextBasketSearch)
        {
            basket = FindFirstObjectByType<TomatoHarvestBasket>();
            nextBasketSearch = Time.unscaledTime + 1f;
        }
        if (basket != null && basket.Contains(transform.position)) basket.Collect(this, source);
    }

    public void StoreIn(Transform tray)
    {
        stored = true;
        if (outline != null) outline.enabled = false;
        grabbable.enabled = false;
        foreach (GrabInteractable grab in GetComponentsInChildren<GrabInteractable>()) grab.enabled = false;
        foreach (HandGrabInteractable grab in GetComponentsInChildren<HandGrabInteractable>()) grab.enabled = false;
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        body.useGravity = false;
        body.isKinematic = true;
        transform.SetParent(tray, true);
        transform.position = tray.position + Vector3.up * 0.045f;
    }

    private void OnDestroy()
    {
        if (grabbable != null) grabbable.WhenPointerEventRaised -= OnPointerEvent;
    }
}
