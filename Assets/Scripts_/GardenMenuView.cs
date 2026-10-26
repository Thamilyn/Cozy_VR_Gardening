using System;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Small world-space UI using the same Meta ray/PointableCanvas path as the calendar.</summary>
public sealed class GardenMenuView
{
    public GameObject Root { get; }
    public Transform Content { get; }
    public BoxCollider Collider { get; }
    private readonly Font font;
    private readonly Vector2 size;
    private readonly CanvasGroup inputGroup;

    public GardenMenuView(string name, Transform parent, Vector2 dimensions)
    {
        size = dimensions;
        Root = new GameObject(name, typeof(RectTransform));
        Root.SetActive(false);
        Root.transform.SetParent(parent, false);
        var rect = (RectTransform)Root.transform;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one * 0.001f;
        Canvas canvas = Root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        // Keep garden instruction canvases from drawing their text over menu controls.
        canvas.sortingOrder = 100;
        inputGroup = Root.AddComponent<CanvasGroup>();
        Root.AddComponent<GraphicRaycaster>();
        Collider = Root.AddComponent<BoxCollider>();
        Collider.isTrigger = true;
        Collider.size = new Vector3(size.x, size.y, 1f);
        Collider.center = Vector3.forward;
        ColliderSurface surface = Root.AddComponent<ColliderSurface>();
        surface.InjectAllColliderSurface(Collider);
        PointableCanvas pointable = Root.AddComponent<PointableCanvas>();
        pointable.InjectAllPointableCanvas(canvas);
        RayInteractable ray = Root.AddComponent<RayInteractable>();
        ray.InjectAllRayInteractable(surface);
        ray.InjectOptionalPointableElement(pointable);
        Content = new GameObject("Content", typeof(RectTransform)).transform;
        Content.SetParent(Root.transform, false);
        ((RectTransform)Content).sizeDelta = size;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public void SetInputEnabled(bool enabled) => inputGroup.interactable = enabled;

    public void Clear()
    {
        // Deactivate immediately: Destroy completes at frame end and old callbacks must not remain clickable.
        foreach (Transform child in Content)
        {
            child.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(child.gameObject);
        }
        Image background = Image(Content, "Background", Vector2.zero, size, new Color(0.045f, 0.09f, 0.075f, 1f));
        background.raycastTarget = true;
    }

    public Text Label(string text, Vector2 position, Vector2 dimensions, int fontSize = 28)
    {
        var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        RectTransform rect = (RectTransform)label.transform;
        rect.SetParent(Content, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        Text result = label.GetComponent<Text>();
        result.font = font;
        result.fontSize = fontSize;
        result.alignment = TextAnchor.MiddleCenter;
        result.color = Color.white;
        result.raycastTarget = false;
        result.text = text;
        return result;
    }

    public Button Button(string text, Vector2 position, Vector2 dimensions, Action selected)
    {
        Image image = Image(Content, text, position, dimensions, new Color(0.15f, 0.31f, 0.23f));
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(0.75f, 0.95f, 0.85f);
        colors.pressedColor = new Color(0.55f, 0.8f, 0.65f);
        button.colors = colors;
        // Spatial ray focus replaces navigation between UI controls.
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        Text label = Label(text, Vector2.zero, dimensions - new Vector2(16f, 8f));
        label.transform.SetParent(image.transform, false);
        button.onClick.AddListener(() => selected());
        return button;
    }

    public void SetViewer(Transform viewer)
    {
        if (viewer == null) return;
        Root.GetComponent<Canvas>().worldCamera = viewer.GetComponent<Camera>() ?? Camera.main;
    }

    private static Image Image(Transform parent, string name, Vector2 position, Vector2 dimensions, Color color)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }
}
