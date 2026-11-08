using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
public static class StemTopologyDiagnosis
{
    public static object Main()
    {
        var fruit = Object.FindFirstObjectByType<TomatoFruit>();
        var filter = fruit.GetComponentInChildren<MeshFilter>();
        var mesh = filter.sharedMesh;
        var vertices = mesh.vertices;
        var triangles = mesh.GetTriangles(1);
        var parents = Enumerable.Range(0, vertices.Length).ToArray();
        int Root(int i) { while (parents[i] != i) i = parents[i]; return i; }
        void Join(int a, int b) { parents[Root(a)] = Root(b); }
        var byPosition = new Dictionary<Vector3, int>();
        foreach (int index in triangles.Distinct())
        {
            if (byPosition.TryGetValue(vertices[index], out int other)) Join(index, other);
            else byPosition.Add(vertices[index], index);
        }
        for (int i = 0; i < triangles.Length; i += 3) { Join(triangles[i], triangles[i + 1]); Join(triangles[i], triangles[i + 2]); }
        var groups = new List<object>();
        foreach (var group in triangles.Distinct().GroupBy(Root))
        {
            int[] indices = group.ToArray();
            var bounds = new Bounds(vertices[indices[0]], Vector3.zero);
            foreach (int index in indices) bounds.Encapsulate(vertices[index]);
            groups.Add(new { count = indices.Length, min = bounds.min.ToString("F5"), max = bounds.max.ToString("F5"),
                worldCenter = filter.transform.TransformPoint(bounds.center).ToString("F5") });
        }
        TomatoFruitMesh.TryGetFruitBounds(filter, filter.GetComponent<MeshRenderer>(), out Bounds fruitBounds);
        Vector3 mean = Vector3.zero;
        foreach (int index in triangles.Distinct()) mean += vertices[index];
        mean /= triangles.Distinct().Count();
        Vector3 axis = (mean - fruitBounds.center).normalized;
        var tips = triangles.Distinct().Select(index => new { index, projection = Vector3.Dot(vertices[index] - fruitBounds.center, axis) })
            .OrderByDescending(item => item.projection).Take(16).Select(item => new { item.index, item.projection,
                point = vertices[item.index].ToString("F5"), world = filter.transform.TransformPoint(vertices[item.index]).ToString("F5") }).ToArray();
        return new { groups, fruitBounds = fruitBounds.ToString("F5"), greenMean = mean.ToString("F5"),
            tips,
            up = filter.transform.up.ToString("F5"), right = filter.transform.right.ToString("F5"), forward = filter.transform.forward.ToString("F5") };
    }
}
