using Oculus.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Session menu, installed before the garden's Start methods without changing the saved scene.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(-200)]
public sealed class GardenMenuController : MonoBehaviour
{
    private static GardenMenuController active;
    private GardenMenuView menu;
    private GardenMenuView launcher;
    [SerializeField, Min(0f), Tooltip("Space above the fixed gardening guide, in metres.")]
    private float guidePanelGap = 0.08f;
    [SerializeField, Tooltip("Launch menu position relative to the player's initial floor origin, in metres.")]
    private Vector3 launchOffset = new Vector3(0f, 1.45f, 1.3f);
    private GardenInteractionSuspension suspension;
    private Transform viewer;
    private bool visible;
    private bool started;
    private bool awaitingRelease;
    private bool menuInputArmed;
    private float releasedAt = float.NaN;
    private float menuReleasedAt = float.NaN;
    private bool placed;
    private Text launcherHint;

    public static bool HasStarted => active == null || active.started;
    public static bool GardenSuspended => active != null && (active.visible || active.awaitingRelease);

    public static bool RayHitsMenu(Ray ray, float distance)
    {
        if (active == null) return false;
        GardenMenuView target = active.visible ? active.menu : active.launcher;
        return target != null && target.Root.activeInHierarchy && target.Collider.enabled &&
            target.Collider.Raycast(ray, out _, distance);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { active = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Garden_Moves") return;
        if (FindFirstObjectByType<GardenMenuController>() != null) return;
        new GameObject("Garden menu and preferences").AddComponent<GardenMenuController>();
    }

    private void Awake()
    {
        active = this;
        GardenPreferences.Load();
        suspension = new GardenInteractionSuspension(gameObject.scene);
        menu = new GardenMenuView("Garden menu", transform, new Vector2(860f, 710f));
        launcher = new GardenMenuView("Open garden menu", transform, new Vector2(360f, 155f));
        launcher.Clear();
        launcher.Button("Menu", new Vector2(0f, 25f), new Vector2(320f, 75f), Open);
        launcherHint = launcher.Label("Point + pinch / Trigger", new Vector2(0f, -45f), new Vector2(340f, 50f), 22);
        if (FindFirstObjectByType<PointableCanvasModule>() == null)
        {
            EventSystem events = FindFirstObjectByType<EventSystem>();
            if (events == null) events = new GameObject("Garden menu events", typeof(EventSystem)).GetComponent<EventSystem>();
            events.gameObject.AddComponent<PointableCanvasModule>();
        }
        visible = true;
        ResetMenuInput();
        ShowHome();
        menu.Root.SetActive(true);
        suspension.Tick();
    }

    private void Update()
    {
        if (viewer == null)
        {
            OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
            viewer = rig != null ? rig.centerEyeAnchor : Camera.main != null ? Camera.main.transform : null;
            if (viewer != null)
            {
                if (!placed)
                {
                    PlaceLaunchStation(rig != null ? rig.transform : viewer);
                    placed = true;
                }
                menu.SetViewer(viewer);
                launcher.SetViewer(viewer);
            }
        }
        if (GardenSuspended)
        {
            suspension.Tick();
            bool released = suspension.InputsReleased();
            HandleInputRelease(released, Time.unscaledTime);
        }
    }

    // Keep the release decision independent of the device sample so it can be verified
    // with deterministic samples without pretending to simulate tracked hands or controllers.
    private void HandleInputRelease(bool released, float now)
    {
        if (visible)
        {
            if (!released) menuReleasedAt = float.NaN;
            else if (float.IsNaN(menuReleasedAt)) menuReleasedAt = now;
            if (!menuInputArmed && !float.IsNaN(menuReleasedAt) && now - menuReleasedAt >= 0.15f)
            {
                menuInputArmed = true;
                menu.SetInputEnabled(true);
            }
        }
        if (!awaitingRelease) return;
        if (!released) releasedAt = float.NaN;
        else if (float.IsNaN(releasedAt)) releasedAt = now;
        if (float.IsNaN(releasedAt) || now - releasedAt < 0.2f) return;
        awaitingRelease = false;
        suspension.Release();
        launcherHint.text = "Point + pinch / Trigger";
    }

    private void LateUpdate()
    {
        // Catch props released by Meta later in the frame without forcing held objects out of the hand.
        if (GardenSuspended) suspension.Tick();
    }

    public void Open()
    {
        if (visible) return;
        visible = true;
        awaitingRelease = false;
        ResetMenuInput();
        FindFirstObjectByType<CalendarVisibilityController>()?.HideForMenu();
        foreach (GronnyVoicePlayer voice in FindObjectsByType<GronnyVoicePlayer>(FindObjectsSortMode.None)) voice.SuspendForMenu();
        suspension.Tick();
        foreach (WateringCan can in FindObjectsByType<WateringCan>(FindObjectsSortMode.None)) can.SuspendPouring();
        launcher.Root.SetActive(false);
        ShowHome();
        menu.Root.SetActive(true);
    }

    private void Continue()
    {
        if (!menuInputArmed) return;
        bool firstLaunch = !started;
        started = true;
        visible = false;
        awaitingRelease = true;
        releasedAt = float.NaN;
        menu.Root.SetActive(false);
        if (firstLaunch) PlacePauseStation();
        launcher.Root.SetActive(true);
        launcherHint.text = "Release selection.\nStraighten your free index finger.";
    }

    private void ResetMenuInput()
    {
        menuInputArmed = false;
        menuReleasedAt = float.NaN;
        menu.SetInputEnabled(false);
    }

    private void ShowHome()
    {
        menu.Clear();
        menu.Label(started ? "Garden paused" : "Cozy Garden", new Vector2(0f, 275f), new Vector2(790f, 65f), 42);
        menu.Label("Point at a button, then pinch or press Trigger.", new Vector2(0f, 200f), new Vector2(790f, 70f));
        menu.Button(started ? "Return to garden" : "Play", new Vector2(0f, 95f), new Vector2(650f, 85f), Continue);
        menu.Button("Controls", new Vector2(0f, -15f), new Vector2(650f, 85f), ShowControls);
        menu.Button("Sound settings", new Vector2(0f, -125f), new Vector2(650f, 85f), ShowSound);
        menu.Label(started
            ? "Adjust sound or review the controls.\nYour current garden continues when you return."
            : "Select Play to enter the garden.\nThe pause menu will be beside Gronny.",
            new Vector2(0f, -260f), new Vector2(790f, 110f), 25);
    }

    private void ShowControls()
    {
        menu.Clear();
        menu.Label("Controls", new Vector2(0f, 280f), new Vector2(790f, 65f), 38);
        menu.Label("Hands\nPoint + pinch: select buttons. Pinch: grab; release to let go.\nCalendar: left-hand pinch away from objects.\nMove: curl index, tap thumb, swipe; straighten index to exit.\n\nTouch controllers\nGrip: grab. Trigger: select buttons.\nCalendar: Y. Refill held can: B.\n\nUse the visible Menu button at any tutorial step.\nHold the can upright after returning to the garden.",
            new Vector2(0f, 10f), new Vector2(790f, 445f), 26);
        menu.Button("Back", new Vector2(0f, -280f), new Vector2(650f, 75f), ShowHome);
    }

    private void ShowSound()
    {
        menu.Clear();
        menu.Label("Sound settings", new Vector2(0f, 285f), new Vector2(790f, 65f), 38);
        VolumeRow("Overall", GardenPreferences.MasterVolume, 165f, GardenPreferences.SetMasterVolume);
        VolumeRow("Gronny", GardenPreferences.VoiceVolume, 35f, GardenPreferences.SetVoiceVolume);
        menu.Button(GardenPreferences.Muted ? "Sound: muted — select to unmute" : "Mute sound", new Vector2(0f, -95f),
            new Vector2(710f, 75f), () => { GardenPreferences.SetMuted(!GardenPreferences.Muted); ShowSound(); });
        menu.Button("Restore sound defaults", new Vector2(0f, -195f), new Vector2(710f, 75f),
            () => { GardenPreferences.RestoreSoundDefaults(); ShowSound(); });
        menu.Button("Back", new Vector2(0f, -290f), new Vector2(710f, 65f), ShowHome);
    }

    private void VolumeRow(string title, float value, float y, System.Action<float> change)
    {
        menu.Label(title + ": " + Mathf.RoundToInt(value * 100f) + "%", new Vector2(0f, y), new Vector2(460f, 75f), 30);
        menu.Button("−", new Vector2(-300f, y), new Vector2(110f, 80f), () => { change(value - 0.1f); ShowSound(); });
        menu.Button("+", new Vector2(300f, y), new Vector2(110f, 80f), () => { change(value + 0.1f); ShowSound(); });
    }

    private void PlaceLaunchStation(Transform playerOrigin)
    {
        // Snapshot the spawn origin once so the launch screen stays where the player enters.
        Vector3 forward = Vector3.ProjectOnPlane(playerOrigin.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
        transform.SetPositionAndRotation(playerOrigin.position + rotation * launchOffset, rotation);
    }

    private void PlacePauseStation()
    {
        TomatoPrototypeGuide guide = FindFirstObjectByType<TomatoPrototypeGuide>();
        RectTransform panel = guide != null ? guide.GetComponent<RectTransform>() : null;
        if (panel != null)
        {
            Vector3 top = panel.TransformPoint(new Vector3(panel.rect.center.x, panel.rect.yMax, 0f));
            // Both panels start above the board's top edge and expand upwards. Snapshot
            // the authored pose once so tracking and later rig motion cannot move them.
            transform.SetPositionAndRotation(top + panel.up * guidePanelGap, panel.rotation);
            menu.Root.transform.localPosition = Vector3.up * (menu.Collider.size.y * menu.Root.transform.localScale.y * 0.5f);
            launcher.Root.transform.localPosition = Vector3.up * (launcher.Collider.size.y * launcher.Root.transform.localScale.y * 0.5f);
            return;
        }
        // A scene without the guide keeps a usable pause menu at the original launch station.
    }

    private void OnDestroy()
    {
        suspension?.Release();
        if (active == this) active = null;
    }
}
