using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public static class StemGeometryDiagnosis
{
    public static object Main()
    {
        var fruit = Object.FindFirstObjectByType<TomatoFruit>();
        var filter = fruit.GetComponentInChildren<MeshFilter>();
        var renderer = filter.GetComponent<MeshRenderer>();
        var vertices = filter.sharedMesh.vertices;
        var groups = new List<object>();
        foreach (var material in renderer.sharedMaterials.Select((value, index) => new { value, index }))
        {
            int[] indices = filter.sharedMesh.GetTriangles(material.index).Distinct().ToArray();
            Bounds bounds = new(filter.transform.TransformPoint(vertices[indices[0]]), Vector3.zero);
            foreach (int index in indices) bounds.Encapsulate(filter.transform.TransformPoint(vertices[index]));
            groups.Add(new { material = material.value.name, count = indices.Length,
                min = bounds.min.ToString("F5"), max = bounds.max.ToString("F5") });
        }
        var cameraObject = new GameObject("Temporary stem diagnosis camera");
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        markerMaterial.color = Color.magenta;
        marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;
        marker.transform.position = fruit.StemPosition;
        marker.transform.localScale = Vector3.one * 0.007f;
        marker.layer = 31;
        var parts = fruit.GetComponentsInChildren<Transform>(true);
        int[] layers = parts.Select(part => part.gameObject.layer).ToArray();
        var texture = new RenderTexture(720, 720, 24);
        var pixels = new Texture2D(720, 720, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            foreach (var part in parts) part.gameObject.layer = 31;
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 0.08f;
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.16f, 0.17f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 2f;
            camera.targetTexture = texture;
            foreach (var direction in new[] { Vector3.back, Vector3.right, Vector3.up })
            {
                camera.transform.SetPositionAndRotation(fruit.transform.position + direction * 0.4f,
                    Quaternion.LookRotation(-direction, direction == Vector3.up ? Vector3.forward : Vector3.up));
                camera.Render();
                RenderTexture.active = texture;
                pixels.ReadPixels(new Rect(0, 0, 720, 720), 0, 0);
                pixels.Apply();
                File.WriteAllBytes("Docs/Validation/StemCutVideo/StemTarget-" + direction.ToString("F0") + ".png", pixels.EncodeToPNG());
            }
        }
        finally
        {
            for (int i = 0; i < parts.Length; i++) parts[i].gameObject.layer = layers[i];
            RenderTexture.active = previous;
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(marker);
            Object.DestroyImmediate(markerMaterial);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(pixels);
        }
        return new { fruit = fruit.transform.position.ToString("F5"), stem = fruit.StemPosition.ToString("F5"), groups };
    }
}
