using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

/// <summary>Own graph variants preserve the imported vertex/alpha networks and share explicit cel lighting.</summary>
public static class GardenPastelGraphBuilder
{
    private const string Folder = "Assets/Materials/GardenPastel";
    private const string Pack = "Assets/3D set of stylized nature - GHIBLI style/Art/Shaders/";

    [MenuItem("Garden/Create pastel nature shader variants")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Materials", "GardenPastel");
        BuildGraph(Pack + "Leaves.shadergraph", "PastelLeaves");
        BuildGraph(Pack + "Layered.shadergraph", "PastelLayered");
        BuildGraph(Pack + "Props.shadergraph", "PastelProps");
        BuildGraph("Assets/Materials/GardenFlowerLit.shadergraph", "PastelFlowers");
        AssetDatabase.Refresh();
    }

    private static void BuildGraph(string source, string name)
    {
        string path = Folder + "/" + name + ".shadergraph";
        if (File.Exists(path)) return;
        List<JObject> objects = Regex.Matches(File.ReadAllText(source), @"(?ms)^\{.*?^\}")
            .Cast<Match>().Select(m => JObject.Parse(m.Value)).ToList();
        JObject graph = objects[0];
        JObject block = objects.First(o => (string)o["m_SerializedDescriptor"] == "SurfaceDescription.BaseColor");
        JArray edges = (JArray)graph["m_Edges"];
        JObject colorEdge = edges.OfType<JObject>().Single(e => (string)e["m_InputSlot"]?["m_Node"]?["m_Id"] == (string)block["m_ObjectId"]);
        JToken destination = colorEdge["m_InputSlot"].DeepClone();
        string functionId = Id(), normalId = Id();
        string colorSlot = Id(), normalSlot = Id(), outputSlot = Id(), geometrySlot = Id();
        JObject function = Node("UnityEditor.ShaderGraph.CustomFunctionNode", functionId, "Pastel cel lighting",
            colorSlot, normalSlot, outputSlot);
        function["m_SourceType"] = 0;
        function["m_FunctionName"] = "GardenPastel";
        function["m_FunctionSource"] = AssetDatabase.AssetPathToGUID("Assets/Materials/GardenPastelLighting.hlsl");
        function["m_FunctionSourceUsePragmas"] = false;
        function["m_FunctionBody"] = "";
        JObject normal = Node("UnityEditor.ShaderGraph.NormalVectorNode", normalId, "Normal Vector", geometrySlot);
        normal["m_Space"] = 2;
        objects.Add(function); objects.Add(normal);
        objects.Add(Slot(colorSlot, 0, "Color", 0));
        objects.Add(Slot(normalSlot, 1, "Normal", 0));
        objects.Add(Slot(outputSlot, 2, "Out", 1));
        objects.Add(Slot(geometrySlot, 0, "Out", 1));
        ((JArray)graph["m_Nodes"]).Add(Ref(functionId));
        ((JArray)graph["m_Nodes"]).Add(Ref(normalId));
        colorEdge["m_InputSlot"] = EdgeSlot(functionId, 0);
        edges.Add(new JObject { ["m_OutputSlot"] = EdgeSlot(functionId, 2), ["m_InputSlot"] = destination });
        edges.Add(new JObject { ["m_OutputSlot"] = EdgeSlot(normalId, 0), ["m_InputSlot"] = EdgeSlot(functionId, 1) });
        // Unlit target avoids a second PBR evaluation: the custom function handles sun and ambient itself.
        foreach (JObject target in objects.Where(o => (string)o["m_Type"] == "UnityEditor.Rendering.Universal.ShaderGraph.UniversalLitSubTarget"))
            target["m_Type"] = "UnityEditor.Rendering.Universal.ShaderGraph.UniversalUnlitSubTarget";
        graph["m_Path"] = "Cozy Garden";
        File.WriteAllText(path, string.Join("\n\n", objects.Select(o => o.ToString(Formatting.Indented))));
        AssetDatabase.ImportAsset(path);
    }

    private static string Id() => Guid.NewGuid().ToString("N");
    private static JObject Ref(string id) => new() { ["m_Id"] = id };
    private static JObject EdgeSlot(string id, int slot) => new() { ["m_Node"] = Ref(id), ["m_SlotId"] = slot };

    private static JObject Node(string type, string id, string name, params string[] slots) => new()
    {
        ["m_SGVersion"] = 0, ["m_Type"] = type, ["m_ObjectId"] = id,
        ["m_Group"] = Ref(""), ["m_Name"] = name,
        ["m_DrawState"] = new JObject { ["m_Expanded"] = true, ["m_Position"] = new JObject { ["serializedVersion"] = "2", ["x"] = 500, ["y"] = 0, ["width"] = 250, ["height"] = 180 } },
        ["m_Slots"] = new JArray(slots.Select(Ref)), ["synonyms"] = new JArray(), ["m_Precision"] = 0,
        ["m_PreviewExpanded"] = true, ["m_PreviewMode"] = 0,
        ["m_CustomColors"] = new JObject { ["m_SerializableColors"] = new JArray() }
    };

    private static JObject Slot(string id, int index, string name, int direction) => new()
    {
        ["m_SGVersion"] = 0, ["m_Type"] = "UnityEditor.ShaderGraph.Vector3MaterialSlot", ["m_ObjectId"] = id,
        ["m_Id"] = index, ["m_DisplayName"] = name, ["m_SlotType"] = direction, ["m_Hidden"] = false,
        ["m_ShaderOutputName"] = name, ["m_StageCapability"] = 3,
        ["m_Value"] = new JObject { ["x"] = 0, ["y"] = 1, ["z"] = 0 },
        ["m_DefaultValue"] = new JObject { ["x"] = 0, ["y"] = 1, ["z"] = 0 }, ["m_Labels"] = new JArray()
    };
}
