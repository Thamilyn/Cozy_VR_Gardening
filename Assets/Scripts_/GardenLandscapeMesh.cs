using System.Collections.Generic;
using UnityEngine;

/// <summary>A smooth, constant-width trail with grass shoulders, generated once in eight sectors.</summary>
internal static class GardenLandscapeMesh
{
    private const float StartZ = 3f;
    private const float EndZ = 18f;
    private const int RowsPerSector = 16;
    private static readonly float[] CrossSection = { -1.2f, -1f, -0.7f, 0f, 0.7f, 1f, 1.2f };

    public static void SamplePath(float t, out Vector3 centre, out Vector3 tangent, out float halfWidth)
    {
        t = Mathf.Clamp01(t);
        float u = 1f - t;
        // The control points describe a single broad S; z stays linear for scenery clearance queries.
        float x = u * u * u * 6f + 3f * u * u * t * 8f - 3f * u * t * t * 3f - 2f * t * t * t;
        float dx = 6f * u * u - 66f * u * t + 3f * t * t;
        centre = new Vector3(x, 0.012f, Mathf.Lerp(StartZ, EndZ, t));
        tangent = new Vector3(dx, 0f, EndZ - StartZ).normalized;
        halfWidth = Mathf.Lerp(0.95f, 0.72f, Mathf.SmoothStep(0f, 1f, t));
    }

    public static float PathCentre(float z)
    {
        SamplePath(Mathf.InverseLerp(StartZ, EndZ, z), out Vector3 centre, out _, out _);
        return centre.x;
    }

    public static Mesh Create(int sector)
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Color> colors = new();
        List<Vector3> normals = new();
        int columns = CrossSection.Length;
        for (int row = 0; row <= RowsPerSector; row++)
        {
            float t = (sector + (float)row / RowsPerSector) / 8f;
            SamplePath(t, out Vector3 centre, out Vector3 tangent, out float halfWidth);
            Vector3 side = new(tangent.z, 0f, -tangent.x);
            // Blend both ends into the meadow rather than ending in an exposed rectangular cut.
            float startFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.06f, t));
            float endFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.94f, 1f, t));
            float endGrass = 1f - startFade * (1f - endFade);
            for (int column = 0; column < columns; column++)
            {
                float across = CrossSection[column];
                vertices.Add(centre + side * (across * halfWidth));
                float shoulder = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 1.2f, Mathf.Abs(across)));
                float grassWeight = Mathf.Max(shoulder, endGrass);
                float edgeShade = Mathf.Lerp(1f, 0.90f, Mathf.Abs(across) / 1.2f);
                Color dirt = new Color(0.55f, 0.38f, 0.17f, 0f) * edgeShade;
                Color color = dirt;
                // Negative alpha marks a path surface; -1 is soil and 0 is the grass shoulder.
                color.a = grassWeight - 1f;
                colors.Add(color);
                normals.Add(Vector3.up);
            }
        }
        for (int row = 0; row < RowsPerSector; row++)
        for (int column = 0; column < columns - 1; column++)
        {
            int index = row * columns + column;
            triangles.Add(index); triangles.Add(index + columns); triangles.Add(index + 1);
            triangles.Add(index + 1); triangles.Add(index + columns); triangles.Add(index + columns + 1);
        }
        Mesh mesh = new() { name = "Meadow path sector " + sector, hideFlags = HideFlags.HideAndDontSave };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.UploadMeshData(true);
        return mesh;
    }
}
