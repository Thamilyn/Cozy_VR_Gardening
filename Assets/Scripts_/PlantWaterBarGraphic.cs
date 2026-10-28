using UnityEngine;
using UnityEngine.UI;

/// <summary>A white rounded track with cyan water segments, using the shared UI material.</summary>
[DisallowMultipleComponent]
public sealed class PlantWaterBarGraphic : MaskableGraphic
{
    private float progress;
    private Color waterColor = new(0f, 0.68f, 0.87f, 1f);

    public float Progress => progress;

    public void SetProgress(float value, Color tint)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(progress, value) && waterColor == tint) return;
        progress = value;
        waterColor = tint;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        Rect track = GetPixelAdjustedRect();
        GardenRoundedPanel.AddRoundedRectangle(vertices, track, 14f, color);
        const float inset = 5f;
        const int segmentCount = 12;
        const float gap = 4f;
        float width = Mathf.Max(0f, (track.width - inset * 2f - gap * (segmentCount - 1)) / segmentCount);
        for (int i = 0; i < segmentCount; i++)
        {
            float filled = Mathf.Clamp01(progress * segmentCount - i);
            if (filled <= 0f) break;
            Rect segment = new(track.x + inset + i * (width + gap), track.y + inset,
                width * filled, Mathf.Max(0f, track.height - inset * 2f));
            GardenRoundedPanel.AddRoundedRectangle(vertices, segment, 7f, waterColor);
        }
    }
}
