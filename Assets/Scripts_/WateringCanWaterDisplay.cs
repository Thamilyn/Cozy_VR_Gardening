using UnityEngine;

/// <summary>Shows the actual water left in a can, following its mesh while staying readable.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(WateringCan))]
public sealed class WateringCanWaterDisplay : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float barWidthMetres = 0.18f;
    [SerializeField, Min(0.08f)] private float heightAboveCan = 0.1f;
    private WateringCan can;
    private GardenWaterIndicator indicator;
    private Renderer[] canRenderers;
    private Transform viewer;

    private void Awake()
    {
        can = GetComponent<WateringCan>();
        canRenderers = GetComponentsInChildren<Renderer>(true);
        indicator = GardenWaterIndicator.Create(transform, "Watering can water level", gameObject.layer);
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        viewer = rig != null ? rig.centerEyeAnchor : Camera.main != null ? Camera.main.transform : null;
    }

    private void LateUpdate()
    {
        indicator.Canvas.enabled = can != null && can.isActiveAndEnabled;
        if (!indicator.Canvas.enabled) return;

        float level = can.WaterNormalized;
        bool empty = can.CurrentWaterLitres <= 0.000001f;
        bool low = !empty && level <= 0.25f;
        Color tint = empty ? GardenWaterIndicator.AlertColor
            : low ? GardenWaterIndicator.LowColor : GardenWaterIndicator.WaterColor;
        string message = empty ? "Can: empty" : low ? "Can: low water"
            : level >= 0.999f ? "Can: full" : "Can: water remaining";
        indicator.SetLevel(level, tint, message, empty || low ? tint : Color.white);

        Vector3 position = transform.position;
        float top = position.y;
        foreach (Renderer renderer in canRenderers)
        {
            // Exclude the world-space stream so pouring cannot move the indicator.
            if ((renderer is MeshRenderer || renderer is SkinnedMeshRenderer) && renderer.enabled &&
                renderer.gameObject.activeInHierarchy)
                top = Mathf.Max(top, renderer.bounds.max.y);
        }
        position.y = top + heightAboveCan;
        RectTransform content = indicator.RectTransform;
        content.position = position;
        float scale = barWidthMetres / 300f;
        Vector3 parentScale = transform.lossyScale;
        content.localScale = new Vector3(scale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
            scale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), scale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z)));
        if (viewer == null && Camera.main != null) viewer = Camera.main.transform;
        if (viewer == null) return;
        Vector3 direction = position - viewer.position;
        if (direction.sqrMagnitude > 0.001f) content.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    private void OnDisable()
    {
        if (indicator != null) indicator.Canvas.enabled = false;
    }

    private void OnDestroy()
    {
        if (indicator != null) Destroy(indicator.gameObject);
    }

    private void OnValidate()
    {
        barWidthMetres = Mathf.Max(0.1f, barWidthMetres);
        heightAboveCan = Mathf.Max(barWidthMetres * 0.28f + 0.03f, heightAboveCan);
    }
}
