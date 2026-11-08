UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
var view=UnityEditor.SceneView.lastActiveSceneView;
if(view!=null){view.orthographic=false;view.LookAtDirect(new UnityEngine.Vector3(0,1.3f,4.4f),UnityEngine.Quaternion.Euler(2,0,0),6f);view.Repaint();}
return new {saved=true, scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path};

