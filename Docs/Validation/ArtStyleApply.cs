var shader=UnityEngine.Shader.Find("Cozy Garden/Styled Props");
var messages=UnityEditor.ShaderUtil.GetShaderMessages(shader).Select(m=>new {severity=m.severity.ToString(),message=m.message}).ToArray();
if(messages.Any(m=>m.severity=="Error")) return new {applied=false,messages=messages};
var setup=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("GardenArtDirectionSetup")).First(t=>t!=null);
setup.GetMethod("Apply").Invoke(null,null);
return new {applied=true,messages=messages};
