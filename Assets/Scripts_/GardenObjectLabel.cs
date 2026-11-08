using UnityEngine;
using UnityEngine.UI;

/// <summary>A non-interactive, world-scale label for objects named by the spoken instructions.</summary>
[DisallowMultipleComponent]
public sealed class GardenObjectLabel : MonoBehaviour
{
    private GameObject label;
    private Vector3 offset;
    private Transform viewer;
    private Text captionText;
    private System.Func<bool> visibilityCondition;

    public void SetCaption(string caption)
    {
        if (captionText != null && captionText.text != caption) captionText.text = caption;
    }

    public static GardenObjectLabel Create(GameObject target, string caption, Vector3 offset,
        System.Func<bool> visibilityCondition = null)
    {
        GardenObjectLabel result = target.AddComponent<GardenObjectLabel>();
        result.offset = offset;
        result.visibilityCondition = visibilityCondition;
        result.Build(caption);
        return result;
    }

    private void Build(string caption)
    {
        // Independent of the target hierarchy: seeds and fruit have very different scales.
        // Keep the UI name outside GardenGrabSetup's prop-name prefixes (e.g. "Shovel ").
        label = new GameObject("Garden label: " + caption, typeof(RectTransform), typeof(Canvas), typeof(Image));
        label.layer = 2;
        RectTransform panel = (RectTransform)label.transform;
        panel.sizeDelta = new Vector2(400f, 80f);
        panel.localScale = Vector3.one * 0.0005f;
        label.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        Image background = label.GetComponent<Image>();
        background.color = new Color(0.025f, 0.07f, 0.05f, 0.95f);
        background.raycastTarget = false;
        GardenRoundedPanel.Apply(background);
        GameObject textObject = new("Caption", typeof(RectTransform), typeof(Text));
        textObject.layer = 2;
        RectTransform rect = (RectTransform)textObject.transform;
        rect.SetParent(panel, false);
        rect.sizeDelta = new Vector2(380f, 72f);
        Text text = textObject.GetComponent<Text>();
        captionText = text;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 30;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = caption;
        label.SetActive(false);
    }

    private void LateUpdate()
    {
        if (label == null) return;
        if (viewer == null && Camera.main != null) viewer = Camera.main.transform;
        bool visible = viewer != null && !GardenMenuController.GardenSuspended &&
            (visibilityCondition == null || visibilityCondition());
        label.SetActive(visible);
        if (!visible) return;
        label.transform.position = transform.position + offset;
        Vector3 direction = label.transform.position - viewer.position;
        if (direction.sqrMagnitude > 0.0001f)
            label.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    private void OnDisable() { if (label != null) label.SetActive(false); }
    private void OnDestroy() { if (label != null) Destroy(label); }
}
