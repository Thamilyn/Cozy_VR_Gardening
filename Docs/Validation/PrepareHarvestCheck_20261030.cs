if (!UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("Run only in Play Mode.");
UnityEngine.Application.runInBackground = true;
var menu = UnityEngine.Object.FindFirstObjectByType<GardenMenuController>();
if (menu != null && GardenMenuController.GardenSuspended)
{
    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    var sample = typeof(GardenMenuController).GetMethod("HandleInputRelease", flags);
    float now = UnityEngine.Time.unscaledTime;
    sample.Invoke(menu, new object[] { false, now });
    sample.Invoke(menu, new object[] { true, now });
    sample.Invoke(menu, new object[] { true, now + 0.16f });
    typeof(GardenMenuController).GetMethod("Continue", flags).Invoke(menu, null);
    sample.Invoke(menu, new object[] { true, now });
    sample.Invoke(menu, new object[] { true, now + 0.21f });
}
if (GardenMenuController.GardenSuspended)
    throw new System.InvalidOperationException("Garden is still suspended.");
return new { playing = UnityEditor.EditorApplication.isPlaying, suspended = GardenMenuController.GardenSuspended, frame = UnityEngine.Time.frameCount };
