using UnityEngine;
using UnityEngine.Rendering;

/// <summary>A fixed visual horizon. The original flat ground remains responsible for physics.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class GardenDistantGround : MonoBehaviour
{
    [SerializeField] private MeshRenderer originalGround;
    [SerializeField] private Material landscapeMaterial;
    [SerializeField] private Shader flowerLightingShader;
    [SerializeField] private Shader[] pastelNatureShaders;
    [SerializeField] private Material pastelRockMaterial;
    [Header("Ghibli nature pack")]
    [SerializeField] private GameObject landmarkTree;
    [SerializeField] private GameObject[] trees;
    [SerializeField] private GameObject[] shrubs;
    [Header("Continuous short meadow")]
    [SerializeField] private Material shortGrassMaterial;
    [SerializeField, Range(0.05f, 0.18f)] private float grassHeight = 0.12f;
    [SerializeField, Range(16f, 160f)] private float grassDensity = 128f;
    [SerializeField] private GameObject[] flowers;
    [SerializeField] private GameObject[] rocks;
    [SerializeField] private GameObject fence;
    [SerializeField, Min(1f)] private float flatRadius = 22f;
    [SerializeField, Min(2f)] private float outerRadius = 42f;
    [SerializeField, Min(0f)] private float edgeDrop = 20f;
    [SerializeField, Range(24, 128)] private int angularSegments = 96;
    [SerializeField, Range(4, 24)] private int curvedRings = 16;

    private GameObject visual;
    private Mesh mesh;
    private readonly System.Collections.Generic.List<Mesh> decorationMeshes = new();
    private readonly System.Collections.Generic.List<Material> grassMaterials = new();
    private readonly System.Collections.Generic.Dictionary<Material, Material> sceneryMaterials = new();
    private MeshRenderer hiddenGround;
    private bool originalWasHidden;
    private bool ownsGroundVisibility;

    private void OnEnable() => Rebuild();

    [ContextMenu("Rebuild distant ground")]
    public void Rebuild()
    {
        Release();
        if (originalGround == null || originalGround.sharedMaterial == null) return;

        // Keep the entire existing collider footprint flat, even if the floor is resized.
        Bounds floorBounds = originalGround.bounds;
        Vector3 centre = new(floorBounds.center.x, floorBounds.max.y, floorBounds.center.z);
        float minimumFlatRadius = new Vector2(floorBounds.extents.x, floorBounds.extents.z).magnitude + 1f;
        float startRadius = Mathf.Max(flatRadius, minimumFlatRadius);
        float endRadius = Mathf.Max(outerRadius, startRadius + 1f);
        int segments = Mathf.Clamp(angularSegments, 24, 128);
        int rings = Mathf.Clamp(curvedRings, 4, 24);
        mesh = CreateMesh(centre, startRadius, endRadius, segments, rings);

        visual = new GameObject("Distant ground visual") { hideFlags = HideFlags.HideAndDontSave };
        // World anchored: never parent to the headset or a moving player origin.
        visual.transform.SetPositionAndRotation(centre, Quaternion.identity);
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = landscapeMaterial != null ? landscapeMaterial : originalGround.sharedMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = originalGround.receiveShadows;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        originalWasHidden = originalGround.forceRenderingOff;
        hiddenGround = originalGround;
        ownsGroundVisibility = true;
        // Nonserialized override: leave the authored renderer and ProBuilder mesh intact.
        originalGround.forceRenderingOff = true;
        if (landscapeMaterial != null)
        {
            for (int sector = 0; sector < 8; sector++)
            {
                Mesh decoration = GardenLandscapeMesh.Create(sector);
                decorationMeshes.Add(decoration);
                GameObject group = new("Landscape sector " + sector) { hideFlags = HideFlags.HideAndDontSave };
                group.transform.SetParent(visual.transform, false);
                group.AddComponent<MeshFilter>().sharedMesh = decoration;
                MeshRenderer foliage = group.AddComponent<MeshRenderer>();
                foliage.sharedMaterial = landscapeMaterial;
                foliage.shadowCastingMode = ShadowCastingMode.Off;
                foliage.receiveShadows = false;
                foliage.lightProbeUsage = LightProbeUsage.Off;
                foliage.reflectionProbeUsage = ReflectionProbeUsage.Off;
                foliage.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            }
        }
        GardenShortGrass.Create(visual.transform, shortGrassMaterial, startRadius, endRadius,
            Mathf.Max(0f, edgeDrop), grassHeight, grassDensity, decorationMeshes, grassMaterials);
        GardenNatureScenery.Create(visual.transform, landmarkTree, trees, shrubs,
            flowers, rocks, fence, sceneryMaterials, flowerLightingShader, pastelNatureShaders, pastelRockMaterial);
    }

    private Mesh CreateMesh(Vector3 centre, float startRadius, float endRadius, int segments, int rings)
    {
        int ringCount = rings + 1;
        Vector3[] vertices = new Vector3[1 + ringCount * segments];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uv = new Vector2[vertices.Length];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[segments * 3 + rings * segments * 6];
        normals[0] = Vector3.up;
        Color grass = new(1.5f, 1.8f, 1.3f);
        colors[0] = grass;
        Vector3 localCentre = originalGround.transform.InverseTransformPoint(centre);
        uv[0] = new Vector2(localCentre.x, localCentre.z);
        float drop = Mathf.Max(0f, edgeDrop);

        for (int ring = 0; ring < ringCount; ring++)
        {
            float t = Mathf.Sin((float)ring / rings * Mathf.PI * 0.5f);
            float radius = Mathf.Lerp(startRadius, endRadius, t);
            float height = SurfaceHeight(radius, startRadius, endRadius, drop, out float slope);
            for (int sector = 0; sector < segments; sector++)
            {
                float angle = sector * (2f * Mathf.PI / segments);
                float x = Mathf.Cos(angle);
                float z = Mathf.Sin(angle);
                int index = 1 + ring * segments + sector;
                vertices[index] = new Vector3(x * radius, height, z * radius);
                colors[index] = Color.Lerp(grass, new Color(1.2f, 1.4f, 1f), t * t * 0.45f);
                normals[index] = new Vector3(-slope * x, 1f, -slope * z).normalized;
                // Continue the existing ProBuilder floor's planar texture density.
                Vector3 floorLocal = originalGround.transform.InverseTransformPoint(centre + vertices[index]);
                uv[index] = new Vector2(floorLocal.x, floorLocal.z);
            }
        }

        int cursor = 0;
        for (int sector = 0; sector < segments; sector++)
        {
            int next = (sector + 1) % segments;
            triangles[cursor++] = 0;
            triangles[cursor++] = 1 + next;
            triangles[cursor++] = 1 + sector;
            for (int ring = 0; ring < rings; ring++)
            {
                int inner = 1 + ring * segments;
                int outer = inner + segments;
                triangles[cursor++] = inner + sector;
                triangles[cursor++] = inner + next;
                triangles[cursor++] = outer + sector;
                triangles[cursor++] = inner + next;
                triangles[cursor++] = outer + next;
                triangles[cursor++] = outer + sector;
            }
        }

        Mesh result = new() { name = "Garden distant ground", hideFlags = HideFlags.HideAndDontSave };
        result.vertices = vertices;
        result.normals = normals;
        result.uv = uv;
        result.colors = colors;
        result.triangles = triangles;
        result.RecalculateBounds();
        result.RecalculateTangents();
        result.UploadMeshData(true);
        return result;
    }

    private void OnDisable() => Release();
    private void OnDestroy() => Release();

    internal static float SurfaceHeight(float radius, float flat, float outer, float drop, out float slope)
    {
        float t = Mathf.Clamp01((radius - flat) / (outer - flat));
        float cosine = Mathf.Sqrt(Mathf.Max(0f, 1f - t * t));
        slope = -drop * t / ((outer - flat) * Mathf.Max(0.001f, cosine));
        return drop * (cosine - 1f);
    }

    private void Release()
    {
        if (ownsGroundVisibility && hiddenGround != null)
            hiddenGround.forceRenderingOff = originalWasHidden;
        ownsGroundVisibility = false;
        hiddenGround = null;
        DestroyGenerated(visual);
        DestroyGenerated(mesh);
        foreach (Mesh decoration in decorationMeshes) DestroyGenerated(decoration);
        decorationMeshes.Clear();
        foreach (Material material in grassMaterials) DestroyGenerated(material);
        grassMaterials.Clear();
        foreach (Material material in sceneryMaterials.Values) DestroyGenerated(material);
        sceneryMaterials.Clear();
        visual = null;
        mesh = null;
    }

    private static void DestroyGenerated(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
