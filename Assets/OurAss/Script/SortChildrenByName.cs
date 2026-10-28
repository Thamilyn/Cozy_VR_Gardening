
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Readme;

public static class SortChildrenByName
{
    [MenuItem("GameObject/Sort Children By Name")]
    private static void Sort()
    {
        Transform parent = Selection.activeTransform;
        if (parent == null) return;

        var children = parent.Cast<Transform>()
            .OrderBy(t => t.name,
                System.StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Undo.RegisterChildrenOrderUndo(parent, "Sort Children");

        for (int i = 0; i < children.Length; i++)
        {
            children[i].SetSiblingIndex(i);
        }
    }
}
