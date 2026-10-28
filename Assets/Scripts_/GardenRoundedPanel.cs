using UnityEngine;
using UnityEngine.UI;

/// <summary>Rounds solid UI backgrounds while preserving their Image and interaction references.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Image))]
public sealed class GardenRoundedPanel : BaseMeshEffect
{
    [SerializeField, Min(0f)] private float cornerRadius = 24f;

    public static void Apply(Image image)
    {
        // Sprite images are illustrations or icons; keep their original geometry.
        if (image == null || image.overrideSprite != null || image.GetComponent<GardenRoundedPanel>() != null) return;
        image.gameObject.AddComponent<GardenRoundedPanel>();
    }

    public static void ApplyToHierarchy(Transform root)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true)) Apply(image);
    }

    public override void ModifyMesh(VertexHelper vertices)
    {
        if (!IsActive() || graphic is not Image image || image.overrideSprite != null) return;
        vertices.Clear();
        AddRoundedRectangle(vertices, image.GetPixelAdjustedRect(), cornerRadius, image.color);
    }

    internal static void AddRoundedRectangle(VertexHelper vertices, Rect rect, float radius, Color tint)
    {
        if (rect.width <= 0f || rect.height <= 0f) return;
        radius = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
        int centre = vertices.currentVertCount;
        vertices.AddVert(rect.center, tint, Vector2.zero);
        const int cornerSegments = 6;
        // Shared UI material and geometry only; no textures, masks or extra materials.
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
