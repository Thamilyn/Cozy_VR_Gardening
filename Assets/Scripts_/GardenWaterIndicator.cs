using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared, non-interactive water bar and short label for garden objects.</summary>
[DisallowMultipleComponent]
public sealed class GardenWaterIndicator : MonoBehaviour
{
    public static readonly Color WaterColor = new(0f, 0.68f, 0.87f, 1f);
    public static readonly Color LowColor = new(1f, 0.68f, 0.23f, 1f);
    public static readonly Color AlertColor = new(1f, 0.38f, 0.3f, 1f);
    private PlantWaterBarGraphic bar;
    private Text status;

    public RectTransform RectTransform { get; private set; }
    public Canvas Canvas { get; private set; }

    public static GardenWaterIndicator Create(Transform parent, string label, int layer, int statusFontSize = 28)
    {
        GameObject root = new(label, typeof(RectTransform), typeof(Canvas), typeof(GardenWaterIndicator));
        root.layer = layer;
        root.transform.SetParent(parent, false);
        GardenWaterIndicator view = root.GetComponent<GardenWaterIndicator>();
        view.RectTransform = root.GetComponent<RectTransform>();
        view.RectTransform.sizeDelta = new Vector2(360f, 180f);
        view.Canvas = root.GetComponent<Canvas>();
        view.Canvas.renderMode = RenderMode.WorldSpace;
        view.Canvas.overrideSorting = false;

        // Visual feedback only: no raycaster, collider or Meta interaction component.
        GameObject track = new("Water fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(PlantWaterBarGraphic));
        track.layer = layer;
        RectTransform rect = track.GetComponent<RectTransform>();
        rect.SetParent(root.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(300f, 64f);
        view.bar = track.GetComponent<PlantWaterBarGraphic>();
        view.bar.color = Color.white;
        view.bar.raycastTarget = false;

        GameObject textRoot = new("Water status", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
        textRoot.layer = layer;
        view.status = textRoot.GetComponent<Text>();
        view.status.rectTransform.SetParent(root.transform, false);
        view.status.rectTransform.anchorMin = view.status.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        view.status.rectTransform.anchoredPosition = new Vector2(0f, -60f);
        view.status.rectTransform.sizeDelta = new Vector2(360f, 48f);
        view.status.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        view.status.fontSize = statusFontSize;
        view.status.alignment = TextAnchor.MiddleCenter;
        view.status.color = Color.white;
        view.status.raycastTarget = false;
        view.status.horizontalOverflow = HorizontalWrapMode.Wrap;
        view.status.verticalOverflow = VerticalWrapMode.Truncate;
        Shadow shadow = textRoot.GetComponent<Shadow>();
        shadow.effectColor = new Color(0.02f, 0.13f, 0.17f, 0.9f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
        return view;
    }

    public void SetLevel(float value, Color fillColor, string message, Color textColor)
    {
        bar.SetProgress(value, fillColor);
        status.text = message;
        status.color = textColor;
    }
}
