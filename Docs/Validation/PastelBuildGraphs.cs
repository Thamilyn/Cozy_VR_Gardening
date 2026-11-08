var builder=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("GardenPastelGraphBuilder")).First(t=>t!=null);builder.GetMethod("Build").Invoke(null,null);
return UnityEditor.AssetDatabase.FindAssets("t:Shader",new[]{"Assets/Materials/GardenPastel"}).Select(g=>UnityEditor.AssetDatabase.GUIDToAssetPath(g)).ToArray();
