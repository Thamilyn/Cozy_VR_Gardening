using UnityEngine;

/// <summary>Visual state of the Ground mesh in a pot. Water is measured in millilitres.</summary>
[DisallowMultipleComponent]
public sealed class GardenSoil : MonoBehaviour
{
    [SerializeField] private Renderer groundRenderer;
    [SerializeField, Min(0f)] private float wetThresholdMillilitres = 100f;
    [SerializeField] private Color dryColor = new(0.62f, 0.43f, 0.25f, 1f);
    [SerializeField] private Color wetColor = new(0.23f, 0.14f, 0.09f, 1f);
    [SerializeField] private float receivedMillilitres;

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
        receivedMillilitres = Mathf.Max(0f, millilitres);
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (groundRenderer == null) return;
        Color color = IsWet ? wetColor : dryColor;
        MaterialPropertyBlock properties = new();
        groundRenderer.GetPropertyBlock(properties);
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);
        groundRenderer.SetPropertyBlock(properties);
    }

    private void OnValidate()
    {
        wetThresholdMillilitres = Mathf.Max(0f, wetThresholdMillilitres);
        if (groundRenderer == null) groundRenderer = GetComponent<Renderer>();
        ApplyColor();
    }
}
