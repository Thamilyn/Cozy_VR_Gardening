using System.Collections.Generic;
using UnityEngine;

/// <summary>World-space water bar and a short care prompt for one planted tomato.</summary>
[DisallowMultipleComponent]
public sealed class PlantWaterProgress : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float barWidthMetres = 0.22f;
    [Tooltip("Clearance above the active plant mesh, including the labels below the bar.")]
    [SerializeField, Min(0.1f)] private float heightAbovePlant = 0.14f;
    [SerializeField] private Color fillingColor = GardenWaterIndicator.WaterColor;
    [SerializeField] private Color excessColor = GardenWaterIndicator.AlertColor;
    [SerializeField] private Color drainedColor = GardenWaterIndicator.LowColor;
    private SeedsController seeds;
    private SeedsController.PlantedSeed plant;
    private CalendarSystem calendar;
    private Canvas canvas;
    private RectTransform content;
    private GardenWaterIndicator indicator;
    private Transform viewer;
    private GrowthStage visibleStage;
    private GameObject visibleVisual;
    private readonly List<Renderer> plantRenderers = new();

    public void Initialize(SeedsController controller, SeedsController.PlantedSeed plantedSeed)
    {
        if (calendar != null) calendar.Changed -= Refresh;
        seeds = controller;
        plant = plantedSeed;
        calendar = seeds != null ? seeds.Calendar : null;
        if (calendar == null || plant == null || plant.seed == null || plant.pot == null)
        {
            enabled = false;
            return;
        }
        if (canvas == null) CreateView();
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        viewer = rig != null ? rig.centerEyeAnchor : Camera.main != null ? Camera.main.transform : null;
        visibleStage = plant.stage;
        CachePlantRenderers();
        enabled = true;
        // Remove first because enabling an initialized component also subscribes in OnEnable.
        calendar.Changed -= Refresh;
        calendar.Changed += Refresh;
        Refresh();
    }

    private void CreateView()
    {
        indicator = GardenWaterIndicator.Create(transform, "Plant water progress", gameObject.layer);
        content = indicator.RectTransform;
        canvas = indicator.Canvas;
    }

    private void Refresh()
    {
        if (canvas == null) return;
        CalendarSystem.PlantSave saved = seeds != null ? seeds.GetState(plant) : null;
        GardenWaterSupply supply = GardenWaterSupply.Active;
        canvas.enabled = isActiveAndEnabled && saved != null && (!saved.harvested || supply != null);
        if (!canvas.enabled) return;
        if (visibleStage != plant.stage || visibleVisual != plant.seed.StageVisual)
        {
            visibleStage = plant.stage;
            CachePlantRenderers();
        }
        float target = seeds.TargetMillilitres(plant);
        float limit = seeds.ExcessLimit(plant);
        bool ripe = saved.tomatoPhase >= 5;
        bool excess = !ripe && target > 0f && saved.retainedWaterMillilitres > limit;
        bool drained = !ripe && !excess && target > 0f && saved.phaseWaterMillilitres > limit;
        Color tint = excess ? excessColor : drained ? drainedColor : fillingColor;
        // Full means the phase target was met; excess water still changes the bar's color.
        string message = saved.harvested ? "Harvest complete"
            : supply != null && supply.Exhausted ? "Try again"
            : ripe ? "Ready to harvest"
            : calendar.IsAdvancing ? "Watering paused"
            : target <= 0f ? "Water target unavailable"
            : excess ? "Stop watering"
            : saved.retainedWaterMillilitres >= target ? "Enough water" : "Water the soil";
        indicator.SetLevel(ripe ? 1f : target > 0f ? saved.retainedWaterMillilitres / target : 0f,
            tint, message, excess || drained ? tint : Color.white);
    }

    private void LateUpdate()
    {
        Refresh();
        if (canvas == null || !canvas.enabled) return;
        if (plant.seed == null || plant.pot == null)
        {
            canvas.enabled = false;
            return;
        }
        Vector3 position = plant.pot.PlantingPoint;
        float top = position.y;
        Transform visualRoot = plant.seed.StageVisual != null ? plant.seed.StageVisual.transform : plant.seed.transform;
        foreach (Renderer renderer in plantRenderers)
            if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy &&
                renderer.transform.IsChildOf(visualRoot))
                top = Mathf.Max(top, renderer.bounds.max.y);
        position.y = top + heightAbovePlant;
        content.position = position;
        // Keep a readable world size even when the pot or seed has been scaled.
        float scale = barWidthMetres / 300f;
        Vector3 parentScale = transform.lossyScale;
        content.localScale = new Vector3(scale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
            scale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), scale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z)));
        if (viewer == null && Camera.main != null) viewer = Camera.main.transform;
        if (viewer == null) return;
        Vector3 direction = position - viewer.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f) content.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    private void CachePlantRenderers()
    {
        visibleVisual = plant.seed.StageVisual;
        GameObject visual = visibleVisual != null ? visibleVisual : plant.seed.gameObject;
        visual.GetComponentsInChildren(false, plantRenderers);
    }

    private void OnEnable()
    {
        if (calendar == null) return;
        calendar.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (calendar != null) calendar.Changed -= Refresh;
        if (canvas != null) canvas.enabled = false;
    }

    private void OnDestroy()
    {
        if (calendar != null) calendar.Changed -= Refresh;
        if (content != null) Destroy(content.gameObject);
    }

    private void OnValidate()
    {
        barWidthMetres = Mathf.Max(0.1f, barWidthMetres);
        heightAbovePlant = Mathf.Max(barWidthMetres * 0.43f + 0.03f, heightAbovePlant);
    }
}
