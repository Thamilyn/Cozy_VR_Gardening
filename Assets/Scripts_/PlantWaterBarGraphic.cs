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
        AddRoundedRectangle(vertices, track, 14f, color);
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
            AddRoundedRectangle(vertices, segment, 7f, waterColor);
        }
    }

    private static void AddRoundedRectangle(VertexHelper vertices, Rect rect, float radius, Color tint)
    {
        if (rect.width <= 0f || rect.height <= 0f) return;
        radius = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * 0.5f);
        int centre = vertices.currentVertCount;
        vertices.AddVert(rect.center, tint, Vector2.zero);
        const int cornerSegments = 6;
        // Clockwise perimeter; no textures, masks or per-plant materials are required.
        for (int corner = 0; corner < 4; corner++)
        {
            Vector2 origin = new(corner < 2 ? rect.xMax - radius : rect.xMin + radius,
                corner == 0 || corner == 3 ? rect.yMax - radius : rect.yMin + radius);
            for (int step = 0; step <= cornerSegments; step++)
            {
                float angle = (90f - corner * 90f - step * 90f / cornerSegments) * Mathf.Deg2Rad;
                Vector2 position = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vertices.AddVert(position, tint, Vector2.zero);
            }
        }
        int count = 4 * (cornerSegments + 1);
        for (int i = 0; i < count; i++)
            vertices.AddTriangle(centre, centre + 1 + i, centre + 1 + (i + 1) % count);
    }
}
