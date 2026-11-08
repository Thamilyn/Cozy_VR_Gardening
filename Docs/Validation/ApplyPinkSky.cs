UnityEditor.AssetDatabase.ImportAsset("Assets/Materials/GardenPastelSky.shader",UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var sky=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Materials/GardenPastel/PastelSky.mat");
sky.SetColor("_HorizonPink",new UnityEngine.Color(0.91f,0.81f,0.85f));sky.SetColor("_ZenithBlue",new UnityEngine.Color(0.55f,0.78f,0.95f));sky.SetFloat("_PastelGradientStrength",0.65f);sky.SetFloat("_PastelGradientHeight",0.65f);
UnityEditor.EditorUtility.SetDirty(sky);UnityEditor.AssetDatabase.SaveAssets();
return new{shader=sky.shader.name,errors=UnityEditor.ShaderUtil.GetShaderMessages(sky.shader).Select(m=>new{severity=m.severity.ToString(),message=m.message}).ToArray(),saved=UnityEditor.AssetDatabase.GetAssetPath(sky)};
