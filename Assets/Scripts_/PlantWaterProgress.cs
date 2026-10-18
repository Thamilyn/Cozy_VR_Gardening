using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>World-space water measurements bound to the existing state of one planted tomato.</summary>
[DisallowMultipleComponent]
public sealed class PlantWaterProgress : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float barWidthMetres = 0.22f;
    [Tooltip("Clearance above the active plant mesh, including the labels below the bar.")]
    [SerializeField, Min(0.1f)] private float heightAbovePlant = 0.14f;
    [SerializeField] private Color fillingColor = new(0f, 0.68f, 0.87f, 1f);
    [SerializeField] private Color excessColor = new(1f, 0.38f, 0.3f, 1f);
    [SerializeField] private Color drainedColor = new(1f, 0.68f, 0.23f, 1f);
    private SeedsController seeds;
    private SeedsController.PlantedSeed plant;
    private CalendarSystem calendar;
    private Canvas canvas;
    private RectTransform content;
    private PlantWaterBarGraphic bar;
    private Text amount;
    private Text status;
    private Text accounting;
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
        GameObject root = new("Plant water progress", typeof(RectTransform), typeof(Canvas));
        root.layer = gameObject.layer;
        root.transform.SetParent(transform, false);
        content = root.GetComponent<RectTransform>();
        content.sizeDelta = new Vector2(360f, 240f);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = false;
        // Visual feedback only: no raycaster, collider or Meta interaction component.
        GameObject track = new("Water fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(PlantWaterBarGraphic));
        track.layer = gameObject.layer;
        RectTransform rect = track.GetComponent<RectTransform>();
        rect.SetParent(content, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(300f, 64f);
        bar = track.GetComponent<PlantWaterBarGraphic>();
        bar.color = Color.white;
        bar.raycastTarget = false;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        amount = CreateLabel("Water amount", 60f, 38f, 28, font);
        status = CreateLabel("Water status", -55f, 30f, 22, font);
        accounting = CreateLabel("Water accounting", -100f, 58f, 18, font);
    }

    private Text CreateLabel(string label, float y, float height, int size, Font font)
    {
        GameObject root = new(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
        root.layer = gameObject.layer;
        Text text = root.GetComponent<Text>();
        text.rectTransform.SetParent(content, false);
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchoredPosition = new Vector2(0f, y);
        text.rectTransform.sizeDelta = new Vector2(360f, height);
        text.font = font;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        Shadow shadow = root.GetComponent<Shadow>();
        shadow.effectColor = new Color(0.02f, 0.13f, 0.17f, 0.9f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
        return text;
    }

    private void Refresh()
    {
        if (canvas == null) return;
        CalendarSystem.PlantSave saved = seeds != null ? seeds.GetState(plant) : null;
        canvas.enabled = isActiveAndEnabled && saved != null && !saved.harvested;
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
        // Full means the phase target was met. Never cap the recorded numeric quantity on overflow.
        bar.SetProgress(ripe ? 1f : target > 0f ? saved.retainedWaterMillilitres / target : 0f, tint);
        amount.text = ripe ? "Total: " + saved.waterMillilitres.ToString("0.0") + " ml"
            : saved.retainedWaterMillilitres.ToString("0.0") + " / " + target.ToString("0.0") + " ml";
        status.color = excess || drained ? tint : Color.white;
        status.text = ripe ? "READY TO HARVEST"
            : calendar.IsAdvancing ? "WATERING PAUSED"
            : target <= 0f ? "WATER TARGET UNAVAILABLE"
            : excess ? "TOO MUCH WATER"
            : drained ? "EXCESS WATER DRAINED"
            : saved.retainedWaterMillilitres >= target ? "WATER IN RANGE" : "WATER BELOW TARGET";
        accounting.text = ripe ? "No more watering needed."
            : "Pour: " + saved.currentPourMillilitres.ToString("0.0") + " ml | Stage: " + saved.phaseWaterMillilitres.ToString("0.0") + " ml\n"
                + "Limit: " + limit.ToString("0.0") + " ml | Total: " + saved.waterMillilitres.ToString("0.0") + " ml";
    }

    private void LateUpdate()
    {
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
