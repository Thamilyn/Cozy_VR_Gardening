using System;
using System.IO;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>One-time authoring tool. The generated assets are committed and editable without this tool.</summary>
public static class GardenCalendarBuilder
{
    private const string StageFolder = "Assets/Prefabs/TomatoStages";
    private const string IconFolder = "Assets/Prefabs/CalendarIcons";
    private const string PanelPath = "Assets/Prefabs/GardenCalendarVR.prefab";
    private const string ScenePath = "Assets/Scenes/Garden_Moves.unity";
    private static readonly int[] Days = { 0, 7, 45, 60, 80, 110 };
    private static readonly string[] Names =
    {
        "Seed", "Sprout / germination", "Young plant and growth",
        "Flowering", "Green tomatoes", "Ripe tomatoes and harvest"
    };
    private static readonly string[] AssetNames =
    {
        "01_Brote_PLACEHOLDER", "02_PlantaJoven_PLACEHOLDER", "03_Floracion_PLACEHOLDER",
        "04_TomatesVerdes_PLACEHOLDER", "05_TomatesMaduros_PLACEHOLDER"
    };

    [MenuItem("Garden/Build calendar and tomato placeholders")]
    public static void Build()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(StageFolder);
        EnsureFolder(IconFolder);
        Material stem = MaterialAsset("Placeholder_StemGreen", new Color(0.16f, 0.48f, 0.20f));
        Material leaf = MaterialAsset("Placeholder_LeafGreen", new Color(0.25f, 0.66f, 0.24f));
        Material flower = MaterialAsset("Placeholder_FlowerYellow", new Color(1f, 0.8f, 0.22f));
        Material green = MaterialAsset("Placeholder_TomatoGreen", new Color(0.42f, 0.70f, 0.25f));
        Material red = MaterialAsset("Placeholder_TomatoRed", new Color(0.88f, 0.19f, 0.12f));
        GameObject[] stages = new GameObject[6];
        for (int i = 1; i < 6; i++)
        {
            stages[i] = BuildTomatoStage(i, stem, leaf, flower, green, red);
        }

        string tomatoPath = "Assets/Prefabs/SeedsPrefab/TomatoSeed.prefab";
        GameObject tomato = PrefabUtility.LoadPrefabContents(tomatoPath);
        try
        {
            SerializedObject seed = new SerializedObject(tomato.GetComponent<SeedItem>());
            SerializedProperty slots = seed.FindProperty("tomatoStagePrefabs");
            slots.arraySize = 6;
            for (int i = 0; i < 6; i++)
                slots.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
            seed.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(tomato, tomatoPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(tomato); }

        GameObject panelAsset = BuildPanel();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        CalendarSystem calendar = UnityEngine.Object.FindFirstObjectByType<CalendarSystem>();
        if (calendar == null) throw new Exception("Garden_Moves has no CalendarSystem.");
        SerializedObject clock = new SerializedObject(calendar);
        SerializedProperty milestones = clock.FindProperty("milestones");
        milestones.arraySize = 6;
        for (int i = 0; i < 6; i++)
        {
            SerializedProperty item = milestones.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("day").intValue = Days[i];
            item.FindPropertyRelative("displayName").stringValue = Names[i];
            item.FindPropertyRelative("color").colorValue = PhaseColor(i);
            item.FindPropertyRelative("icon").objectReferenceValue = IconAsset(i);
        }
        clock.ApplyModifiedPropertiesWithoutUndo();

        SeedsController controller = UnityEngine.Object.FindFirstObjectByType<SeedsController>();
        SerializedObject seeds = new SerializedObject(controller);
        // Remove only a legacy tomato water rule; this menu may be run more than once.
        SerializedProperty requirements = seeds.FindProperty("requirements");
        for (int i = requirements.arraySize - 1; i >= 0; i--)
            if (requirements.GetArrayElementAtIndex(i).FindPropertyRelative("crop").enumValueIndex == 0)
                requirements.DeleteArrayElementAtIndex(i);
        seeds.ApplyModifiedPropertiesWithoutUndo();

        CalendarPanel existingPanel = UnityEngine.Object.FindFirstObjectByType<CalendarPanel>();
        GameObject panel = existingPanel != null ? existingPanel.gameObject : null;
        if (panel == null) panel = (GameObject)PrefabUtility.InstantiatePrefab(panelAsset);
        panel.name = "GardenCalendarCanvas";
        panel.transform.SetPositionAndRotation(new Vector3(0.50f, 1.48f, -1.55f), Quaternion.identity);
        SerializedObject panelProperties = new SerializedObject(panel.GetComponent<CalendarPanel>());
        panelProperties.FindProperty("calendar").objectReferenceValue = calendar;
        panelProperties.ApplyModifiedPropertiesWithoutUndo();

        if (UnityEngine.Object.FindFirstObjectByType<PointableCanvasModule>() == null)
        {
            GameObject events = new GameObject("GardenVR_UI_EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<PointableCanvasModule>();
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Garden calendar, five tomato stage prefabs, and VR scene wiring built.");
    }

    [MenuItem("Garden/Add Y toggle and close button")]
    public static void AddToggleAndClose()
    {
        GameObject prefab = PrefabUtility.LoadPrefabContents(PanelPath);
        try
        {
            ConfigureToggle(prefab);
            PrefabUtility.SaveAsPrefabAsset(prefab, PanelPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        CalendarPanel panel = UnityEngine.Object.FindFirstObjectByType<CalendarPanel>();
        if (panel == null) throw new Exception("Garden_Moves has no calendar panel.");
        panel.gameObject.name = "GardenCalendarCanvas";
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Garden calendar now toggles with left controller Y and has a Close button.");
    }

    private static GameObject BuildTomatoStage(int phase, Material stem, Material leaf,
        Material flower, Material green, Material red)
    {
        GameObject root = new GameObject(AssetNames[phase - 1]);
        float height = phase == 1 ? 0.11f : phase == 2 ? 0.22f : 0.30f;
        Part(root.transform, "Stem_PLACEHOLDER", PrimitiveType.Cylinder,
            new Vector3(0, height * 0.5f, 0), new Vector3(0.012f, height * 0.5f, 0.012f), stem);
        int leaves = phase == 1 ? 2 : 4;
        for (int i = 0; i < leaves; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float y = height * (i < 2 ? 0.60f : 0.85f);
            Part(root.transform, "Leaf_PLACEHOLDER_" + i, PrimitiveType.Sphere,
                new Vector3(side * (phase == 1 ? 0.035f : 0.065f), y, 0),
                new Vector3(phase == 1 ? 0.07f : 0.11f, 0.022f, 0.05f), leaf);
        }
        if (phase >= 3)
        {
            Material produce = phase == 3 ? flower : phase == 4 ? green : red;
            for (int i = 0; i < 3; i++)
                Part(root.transform, (phase == 3 ? "Flower" : "Tomato") + "_PLACEHOLDER_" + i,
                    PrimitiveType.Sphere, new Vector3((i - 1) * 0.065f, 0.15f + (i % 2) * 0.06f, -0.025f),
                    Vector3.one * (phase == 3 ? 0.035f : 0.055f), produce);
        }
        string path = StageFolder + "/" + root.name + ".prefab";
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return asset;
    }

    private static void Part(Transform parent, string name, PrimitiveType shape,
        Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        part.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Material MaterialAsset(string name, Color color)
    {
        string path = StageFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Sprite IconAsset(int index)
    {
        string path = IconFolder + "/" + index.ToString("00") + "_ICONO_REEMPLAZAR.png";
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Color tint = PhaseColor(index);
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
            texture.SetPixel(x, y, distance < 23f ? tint : Color.clear);
        }
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject BuildPanel()
    {
        GameObject root = new GameObject("GardenCalendarVR", typeof(RectTransform), typeof(Canvas),
            typeof(GraphicRaycaster), typeof(CalendarPanel), typeof(BoxCollider),
            typeof(ColliderSurface), typeof(PointableCanvas), typeof(RayInteractable));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(850, 710);
        rect.localScale = Vector3.one * 0.001f;
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        BoxCollider collider = root.GetComponent<BoxCollider>();
        collider.size = new Vector3(850, 710, 1);
        collider.center = new Vector3(0, 0, 1);
        SetObject(root.GetComponent<ColliderSurface>(), "_collider", collider);
        SetObject(root.GetComponent<PointableCanvas>(), "_canvas", canvas);
        SetObject(root.GetComponent<RayInteractable>(), "_surface", root.GetComponent<ColliderSurface>());
        SetObject(root.GetComponent<RayInteractable>(), "_pointableElement", root.GetComponent<PointableCanvas>());

        PanelImage(root.transform, "Background_REPLACEABLE", Vector2.zero, new Vector2(850, 710),
            new Color(0.08f, 0.14f, 0.12f, 0.95f));
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Text day = Label(root.transform, "CurrentDay", new Vector2(-235, 285), new Vector2(320, 60), 36, TextAnchor.MiddleLeft, "Day 0", font);
        Text phase = Label(root.transform, "CurrentPhase", new Vector2(125, 285), new Vector2(500, 60), 30, TextAnchor.MiddleLeft, "Seed", font);
        Text note = Label(root.transform, "ApproximateDates", new Vector2(0, 229), new Vector2(770, 42), 22, TextAnchor.MiddleCenter,
            "Approximate dates · tomato growth cycle", font);
        CalendarPanel.MilestoneView[] views = new CalendarPanel.MilestoneView[6];
        for (int i = 0; i < 6; i++)
        {
            float y = 162 - i * 70;
            Image bg = PanelImage(root.transform, "Milestone_" + i + "_Background_REPLACEABLE",
                new Vector2(0, y), new Vector2(760, 62), new Color(0.19f, 0.24f, 0.22f));
            Text number = Label(bg.transform, "Day", new Vector2(-305, 0), new Vector2(115, 55),
                25, TextAnchor.MiddleCenter, "≈ " + Days[i], font);
            Text name = Label(bg.transform, "Phase", new Vector2(40, 0), new Vector2(550, 55),
                24, TextAnchor.MiddleLeft, Names[i], font);
            Image icon = PanelImage(bg.transform, "Icon_REPLACEABLE", new Vector2(-215, 0),
                new Vector2(40, 40), Color.white);
            icon.gameObject.SetActive(false);
            views[i] = new CalendarPanel.MilestoneView
                { dayText = number, nameText = name, icon = icon, background = bg };
        }
        Text water = Label(root.transform, "WaterSummary", new Vector2(-145, -266),
            new Vector2(480, 35), 20, TextAnchor.MiddleLeft, "Total water: 0 ml", font);
        Image buttonImage = PanelImage(root.transform, "AdvancePhaseButton_REPLACEABLE",
            new Vector2(0, -315), new Vector2(450, 65), new Color(0.21f, 0.51f, 0.3f));
        Button button = buttonImage.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        Text buttonLabel = Label(buttonImage.transform, "ButtonLabel", Vector2.zero,
            new Vector2(430, 58), 29, TextAnchor.MiddleCenter, "Advance phase", font);
        SerializedObject panel = new SerializedObject(root.GetComponent<CalendarPanel>());
        panel.FindProperty("currentDayText").objectReferenceValue = day;
        panel.FindProperty("currentPhaseText").objectReferenceValue = phase;
        panel.FindProperty("approximateDatesText").objectReferenceValue = note;
        panel.FindProperty("waterSummaryText").objectReferenceValue = water;
        panel.FindProperty("buttonText").objectReferenceValue = buttonLabel;
        panel.FindProperty("advanceButton").objectReferenceValue = button;
        panel.FindProperty("buttonBackground").objectReferenceValue = buttonImage;
        SerializedProperty rows = panel.FindProperty("milestoneViews");
        rows.arraySize = 6;
        for (int i = 0; i < 6; i++)
        {
            var row = rows.GetArrayElementAtIndex(i);
            row.FindPropertyRelative("dayText").objectReferenceValue = views[i].dayText;
            row.FindPropertyRelative("nameText").objectReferenceValue = views[i].nameText;
            row.FindPropertyRelative("icon").objectReferenceValue = views[i].icon;
            row.FindPropertyRelative("background").objectReferenceValue = views[i].background;
        }
        panel.ApplyModifiedPropertiesWithoutUndo();
        ConfigureToggle(root);
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, PanelPath);
        UnityEngine.Object.DestroyImmediate(root);
        return asset;
    }

    private static Image PanelImage(Transform parent, string name, Vector2 position,
        Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = name.StartsWith("AdvancePhaseButton") || name.StartsWith("CloseButton");
        return image;
    }

    private static void ConfigureToggle(GameObject root)
    {
        root.name = "GardenCalendarCanvas";
        RectTransform canvasRect = root.GetComponent<RectTransform>();
        Transform content = root.transform.Find("GardenCalendarVR");
        if (content == null)
        {
            Transform[] children = new Transform[root.transform.childCount];
            for (int i = 0; i < children.Length; i++) children[i] = root.transform.GetChild(i);
            GameObject contentObject = new GameObject("GardenCalendarVR", typeof(RectTransform));
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.SetParent(root.transform, false);
            contentRect.sizeDelta = canvasRect.sizeDelta;
            foreach (Transform child in children)
            {
                RectTransform childRect = (RectTransform)child;
                Vector2 position = childRect.anchoredPosition;
                childRect.SetParent(contentRect, false);
                childRect.anchoredPosition = position;
            }
            content = contentRect;
        }

        RectTransform phaseRect = content.Find("CurrentPhase") as RectTransform;
        if (phaseRect != null)
        {
            phaseRect.anchoredPosition = new Vector2(70, 285);
            phaseRect.sizeDelta = new Vector2(390, 60);
        }

        Transform closeTransform = content.Find("CloseButton_REPLACEABLE");
        Button closeButton;
        if (closeTransform == null)
        {
            Image closeImage = PanelImage(content, "CloseButton_REPLACEABLE",
                new Vector2(341, 285), new Vector2(105, 55), new Color(0.48f, 0.20f, 0.18f));
            closeImage.raycastTarget = true;
            closeButton = closeImage.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            Label(closeImage.transform, "CloseLabel", Vector2.zero, new Vector2(100, 50),
                24, TextAnchor.MiddleCenter, "Close", Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        }
        else closeButton = closeTransform.GetComponent<Button>();

        CalendarVisibilityController visibility = root.GetComponent<CalendarVisibilityController>();
        if (visibility == null) visibility = root.AddComponent<CalendarVisibilityController>();
        SerializedObject settings = new SerializedObject(visibility);
        settings.FindProperty("panelContent").objectReferenceValue = content.gameObject;
        settings.FindProperty("rayCollider").objectReferenceValue = root.GetComponent<BoxCollider>();
        settings.FindProperty("calendarPanel").objectReferenceValue = root.GetComponent<CalendarPanel>();
        settings.FindProperty("closeButton").objectReferenceValue = closeButton;
        settings.FindProperty("startVisible").boolValue = false;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Text Label(Transform parent, string name, Vector2 position,
        Vector2 size, int fontSize, TextAnchor alignment, string content, Font font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text label = go.GetComponent<Text>();
        label.font = font;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        label.text = content;
        label.raycastTarget = false;
        return label;
    }

    private static void SetObject(UnityEngine.Object target, string property, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty field = serialized.FindProperty(property);
        if (field == null) throw new Exception(target.GetType().Name + " has no " + property);
        field.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Color PhaseColor(int i) => i switch
    {
        0 => new Color(0.75f, 0.55f, 0.35f),
        1 => new Color(0.55f, 0.75f, 0.38f),
        2 => new Color(0.32f, 0.7f, 0.36f),
        3 => new Color(0.95f, 0.8f, 0.38f),
        4 => new Color(0.42f, 0.72f, 0.3f),
        _ => new Color(0.9f, 0.34f, 0.25f)
    };

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        int separator = folder.LastIndexOf('/');
        AssetDatabase.CreateFolder(folder.Substring(0, separator), folder.Substring(separator + 1));
    }
}
