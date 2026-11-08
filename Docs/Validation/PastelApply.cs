var setup=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("GardenPastelSceneSetup")).First(t=>t!=null);setup.GetMethod("Apply").Invoke(null,null);
return new{sky=UnityEngine.RenderSettings.skybox.shader.name,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path};
