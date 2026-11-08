using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Shared meadow patches: native LOD selects geometry and the GPU fits blades to the ground.</summary>
internal static class GardenShortGrass
{
    private const float ChunkSize = 6f;

    public static void Create(Transform parent, Material material, float flat, float outer, float drop,
        float height, float density, List<Mesh> ownedMeshes, List<Material> ownedMaterials)
    {
        if (material == null) return;
        density = Mathf.Clamp(density, 16f, 160f);
        Dictionary<int, Patch> patches = new();
        int extent = Mathf.CeilToInt(outer / ChunkSize);
        for (int x = -extent; x < extent; x++)
        for (int z = -extent; z < extent; z++)
        {
            Vector3 origin = new((x + 0.5f) * ChunkSize, 0f, (z + 0.5f) * ChunkSize);
            float centreRadius = new Vector2(origin.x, origin.z).magnitude;
            if (centreRadius > outer + ChunkSize * 0.71f) continue;
            int tier = centreRadius < 10.3f ? 2 : centreRadius < 30.3f ? 1 : 0;
            if (!patches.TryGetValue(tier, out Patch patch))
            {
                patch = BuildPatch(tier, material, parent.position, flat, outer, drop, height, density);
                patches.Add(tier, patch);
                ownedMeshes.Add(patch.near); ownedMeshes.Add(patch.far); ownedMaterials.Add(patch.material);
            }
            GameObject chunk = new("Short grass chunk " + x + "," + z) { hideFlags = HideFlags.HideAndDontSave };
            chunk.transform.SetParent(parent, false);
            chunk.transform.localPosition = origin;
            float minRadius = new Vector2(Mathf.Max(0f, Mathf.Abs(origin.x) - 3f), Mathf.Max(0f, Mathf.Abs(origin.z) - 3f)).magnitude;
            float maxRadius = new Vector2(Mathf.Abs(origin.x) + 3f, Mathf.Abs(origin.z) + 3f).magnitude;
            float bottom = GardenDistantGround.SurfaceHeight(maxRadius, flat, outer, drop, out _);
            float top = GardenDistantGround.SurfaceHeight(minRadius, flat, outer, drop, out _) + height * 1.25f;
            Bounds bounds = new(new Vector3(0f, (bottom + top) * 0.5f, 0f), new Vector3(6.12f, top - bottom + 0.16f, 6.12f));
            Renderer near = AddRenderer(chunk.transform, patch.near, patch.material, "Dense blades", bounds);
            Renderer far = AddRenderer(chunk.transform, patch.far, patch.material, "Distant blades", bounds);
            LODGroup lod = chunk.AddComponent<LODGroup>();
            lod.fadeMode = LODFadeMode.None;
            lod.SetLODs(new[] { new LOD(0.18f, new[] { near }), new LOD(0.008f, new[] { far }) });
            lod.RecalculateBounds();
        }
    }

    private static Patch BuildPatch(int tier, Material source, Vector3 centre, float flat, float outer,
        float drop, float height, float density)
    {
        float multiplier = tier == 2 ? 2.25f : tier == 1 ? 1f : 0.22f;
        int cells = Mathf.CeilToInt(ChunkSize * Mathf.Sqrt(density * multiplier));
        System.Random random = new(1741 + tier * 991);
        BladeMesh near = new();
        BladeMesh far = new();
        for (int i = 0; i < cells; i++)
        for (int j = 0; j < cells; j++)
        {
            Vector3 root = new((i + (float)random.NextDouble()) / cells * 6f - 3f, 0.002f,
                (j + (float)random.NextDouble()) / cells * 6f - 3f);
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            Vector3 side = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            float bladeHeight = height * Mathf.Lerp(0.65f, 1.25f, (float)random.NextDouble());
            float width = bladeHeight * Mathf.Lerp(0.075f, 0.105f, (float)random.NextDouble());
            Vector3 tip = root + Vector3.up * bladeHeight + new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * bladeHeight * 0.22f;
            bool stable = random.Next(2) == 0;
            float tint = Mathf.Lerp(0.86f, 1.12f, (float)random.NextDouble());
            Color32 color = new Color(tint * 0.9f, tint, tint * 0.85f, stable ? 1f : 0f);
            near.Add(root - side * width, root + side * width, tip, bladeHeight, color, root);
            if (stable) far.AddSimple(root - side * width, root + side * width, tip, bladeHeight, color, root);
        }
        Material material = new(source) { name = "Short meadow tier " + tier, hideFlags = HideFlags.HideAndDontSave, enableInstancing = true };
        material.SetVector("_GroundCentre", new Vector4(centre.x, centre.y, centre.z, flat));
        material.SetVector("_GroundShape", new Vector4(outer, drop, density, cells * cells / 36f));
        return new Patch { near = near.Build("Short meadow near " + tier), far = far.Build("Short meadow far " + tier), material = material };
    }

    private static Renderer AddRenderer(Transform parent, Mesh mesh, Material material, string name, Bounds bounds)
    {
        GameObject visual = new(name) { hideFlags = HideFlags.HideAndDontSave };
        visual.transform.SetParent(parent, false);
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.localBounds = bounds;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        return renderer;
    }

    private sealed class Patch { public Mesh near; public Mesh far; public Material material; }

    private sealed class BladeMesh
    {
        private readonly List<Vector3> vertices = new();
        private readonly List<Vector3> roots = new();
        private readonly List<Vector2> uv = new();
        private readonly List<Color32> colors = new();
        private readonly List<int> indices = new();

        public void Add(Vector3 a, Vector3 b, Vector3 tip, float height, Color32 color, Vector3 root)
        {
            int index = vertices.Count;
            Vector3 lean = tip - root - Vector3.up * height;
            Vector3 mid = root + Vector3.up * height * 0.58f + lean * 0.2f;
            Vector3 halfWidth = (b - a) * 0.5f;
            vertices.Add(a); vertices.Add(b); vertices.Add(mid - halfWidth * 0.62f); vertices.Add(mid + halfWidth * 0.62f); vertices.Add(tip);
            for (int i = 0; i < 5; i++) { roots.Add(root); colors.Add(color); }
            uv.Add(Vector2.zero); uv.Add(Vector2.zero);
            uv.Add(new Vector2(height * 0.58f, 0.58f)); uv.Add(new Vector2(height * 0.58f, 0.58f)); uv.Add(new Vector2(height, 1f));
            indices.Add(index); indices.Add(index + 2); indices.Add(index + 1);
            indices.Add(index + 1); indices.Add(index + 2); indices.Add(index + 3);
            indices.Add(index + 2); indices.Add(index + 4); indices.Add(index + 3);
        }

        public void AddSimple(Vector3 a, Vector3 b, Vector3 tip, float height, Color32 color, Vector3 root)
        {
            int index = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(tip);
            roots.Add(root); roots.Add(root); roots.Add(root);
            uv.Add(Vector2.zero); uv.Add(Vector2.zero); uv.Add(new Vector2(height, 1f));
            colors.Add(color); colors.Add(color); colors.Add(color);
            indices.Add(index); indices.Add(index + 2); indices.Add(index + 1);
        }

        public Mesh Build(string name)
        {
            Mesh mesh = new() { name = name, hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetUVs(1, roots);
            mesh.SetColors(colors); mesh.SetTriangles(indices, 0); mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
