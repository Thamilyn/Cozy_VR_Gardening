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
    private static bool restartAtPlanting;
    private static bool reloading;
    private bool startAtPlanting;
    private bool startOnReady;
    private GardenMenuView menu;
    private GardenMenuView launcher;
    private GardenMenuView gameOver;
    private bool gameOverVisible;
    private int gameOverAttempt;
    private float nextGameOverPlacement;
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
    private GronnyAudioCatalogue audioCatalogue;
    private GronnyVoicePlayer menuVoice;
    private GardenControlDemonstration menuDemonstration;
    private GardenInputMode controlsMode = GardenInputMode.Hands;
    private enum Page { Home, Controls, Demonstration, Sound, GameOver }
    private Page page;
    private string previewId;
    private bool controlsIntroPlayed;

    public static bool HasStarted => active == null || active.started;
    public static bool StartsAtPlanting => active != null && active.startAtPlanting;
    public static bool GardenSuspended => active != null && (active.visible || active.awaitingRelease);

    public static bool RayHitsMenu(Ray ray, float distance)
    {
        if (active == null) return false;
        GardenMenuView target = active.gameOverVisible ? active.gameOver : active.visible ? active.menu : active.launcher;
        return target != null && target.Root.activeInHierarchy && target.Collider.enabled &&
            target.Collider.Raycast(ray, out _, distance);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { active = null; restartAtPlanting = false; reloading = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Garden_Moves") return;
        reloading = false;
        if (FindFirstObjectByType<GardenMenuController>() != null) return;
        new GameObject("Garden menu and preferences").AddComponent<GardenMenuController>();
    }

    private void Awake()
    {
        active = this;
        startAtPlanting = restartAtPlanting;
        startOnReady = startAtPlanting;
        restartAtPlanting = false;
        GardenPreferences.Load();
        if (FindFirstObjectByType<GardenWaterSupply>() == null) gameObject.AddComponent<GardenWaterSupply>();
        suspension = new GardenInteractionSuspension(gameObject.scene);
        menu = new GardenMenuView("Garden menu", transform, new Vector2(860f, 710f));
        launcher = new GardenMenuView("Open garden menu", transform, new Vector2(360f, 155f));
        gameOver = new GardenMenuView("Water game over", transform, new Vector2(860f, 610f));
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
        InitializeAudio();
        ShowHome();
        menu.Root.SetActive(true);
        suspension.Tick();
    }

    private void InitializeAudio()
    {
        audioCatalogue = Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial");
        if (audioCatalogue == null)
        {
            Debug.LogWarning("Garden menu audio catalogue is missing.", this);
            return;
        }
        menuDemonstration = gameObject.AddComponent<GardenControlDemonstration>();
        menuDemonstration.Configure(audioCatalogue);
        menuVoice = gameObject.AddComponent<GronnyVoicePlayer>();
        menuVoice.Configure(audioCatalogue, menuDemonstration, menuPlayback: true);
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
                gameOver.SetViewer(viewer);
            }
        }
        if (startOnReady && placed)
        {
            startOnReady = false;
            menuInputArmed = true;
            Continue();
        }
        GardenWaterSupply supply = GardenWaterSupply.Active;
        if (started && supply != null && supply.Exhausted && gameOverAttempt != supply.Attempt)
            ShowGameOver(supply);
        if (gameOverVisible && viewer != null && Time.unscaledTime >= nextGameOverPlacement)
        {
            nextGameOverPlacement = Time.unscaledTime + 0.5f;
            Vector3 direction = (gameOver.Root.transform.position - viewer.position).normalized;
            if (!gameOver.Root.GetComponent<Canvas>().enabled || Vector3.Dot(viewer.forward, direction) < 0.25f)
                PlaceGameOver();
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
                if (gameOverVisible) gameOver.SetInputEnabled(true);
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
        GardenWaterSupply.Active?.EvaluateExhaustion();
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
        if (!menuInputArmed || (gameOverVisible && GardenWaterSupply.Active != null && GardenWaterSupply.Active.Exhausted)) return;
        StopMenuAudio();
        gameOverVisible = false;
        gameOver.Root.SetActive(false);
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
        gameOver.SetInputEnabled(false);
    }

    public static bool RestartAtPlanting(Scene scene)
    {
        if (reloading) return false;
        if (!Application.CanStreamedLevelBeLoaded(scene.path))
        {
            Debug.LogError("Add Garden_Moves to Build Settings to enable retrying.");
            return false;
        }
        reloading = true;
        restartAtPlanting = true;
        // Reload the scene so crop state, props, water and spoken-cue history
        // all belong to a new session. Only the planting entry point crosses the load.
        foreach (GronnyVoicePlayer voice in FindObjectsByType<GronnyVoicePlayer>(FindObjectsSortMode.None)) voice.Clear();
        try
        {
            if (SceneManager.LoadSceneAsync(scene.path, LoadSceneMode.Single) != null) return true;
        }
        catch (System.Exception error)
        {
            Debug.LogException(error);
        }
        restartAtPlanting = false;
        reloading = false;
        return false;
    }

    private void ShowGameOver(GardenWaterSupply supply)
    {
        if (!visible) Open();
        StopMenuAudio();
        gameOverAttempt = supply.Attempt;
        gameOverVisible = true;
        page = Page.GameOver;
        ResetMenuInput();
        menu.Root.SetActive(false);
        launcher.Root.SetActive(false);
        gameOver.Clear();
        gameOver.Label("Game over", new Vector2(0f, 225f), new Vector2(790f, 75f), 46);
        gameOver.Label(supply.FailureMessage,
            new Vector2(0f, 120f), new Vector2(790f, 100f), 29);
        gameOver.Label(supply.SummaryText, new Vector2(0f, 10f), new Vector2(790f, 80f), 27);
        gameOver.Label("Start again by planting a new seed.\nGronny will guide you from the planting step.",
            new Vector2(0f, -90f), new Vector2(790f, 90f), 25);
        gameOver.Button("Try again", new Vector2(0f, -215f), new Vector2(650f, 85f), () =>
        {
            if (menuInputArmed) supply.Retry();
        });
        gameOver.Root.SetActive(true);
        PlaceGameOver();
        // The empty-supply recording only describes literal exhaustion. For an
        // insufficient remainder, the visible explanation accompanies the retry cue.
        if (supply.AvailableMillilitres <= 0.001f)
            menuVoice?.Enqueue("water_supply_exhausted", "game-over-" + supply.Attempt, () => gameOverVisible && supply.Exhausted);
        menuVoice?.Enqueue("water_retry", "game-over-retry-" + supply.Attempt, () => gameOverVisible && supply.Exhausted);
    }

    private void PlaceGameOver()
    {
        if (viewer == null) return;
        Vector3 forward = Vector3.ProjectOnPlane(viewer.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        gameOver.Root.transform.SetPositionAndRotation(viewer.position + forward * 1.2f - Vector3.up * 0.05f,
            Quaternion.LookRotation(forward, Vector3.up));
        var placement = gameOver.Root.GetComponent<GardenPresentationPlacement>() ?? gameOver.Root.AddComponent<GardenPresentationPlacement>();
        placement.Configure(gameOver.Root.transform, viewer, gameOver.Collider, checkFullSightline: true);
    }

    private void ShowHome()
    {
        StopMenuAudio();
        page = Page.Home;
        menu.Clear();
        menu.Label(started ? "Garden paused" : "Cozy Garden", new Vector2(0f, 275f), new Vector2(790f, 65f), 42);
        menu.Label("Point at a button, then pinch or press Trigger.", new Vector2(0f, 200f), new Vector2(790f, 70f));
        bool retry = started && GardenWaterSupply.Active != null && GardenWaterSupply.Active.Exhausted;
        menu.Button(retry ? "Try again" : started ? "Return to garden" : "Play", new Vector2(0f, 95f), new Vector2(650f, 85f), () =>
        {
            if (!menuInputArmed) return;
            if (retry) { GardenWaterSupply.Active.Retry(); return; }
            Continue();
        });
        menu.Button("Controls", new Vector2(0f, -15f), new Vector2(650f, 85f), ShowControls);
        menu.Button("Sound settings", new Vector2(0f, -125f), new Vector2(650f, 85f), ShowSound);
        menu.Label(started
            ? "Adjust sound or review the controls.\nYour current garden continues when you return."
            : "Practise first with free refills.\nThen grow your tomato with a limited water reserve.\nRefilling uses that reserve: save water to finish.",
            new Vector2(0f, -260f), new Vector2(790f, 110f), 25);
    }

    private void ShowControls()
    {
        StopMenuAudio();
        page = Page.Controls;
        menu.Clear();
        menu.Label("Controls", new Vector2(0f, 280f), new Vector2(790f, 65f), 38);
        menu.Button(controlsMode == GardenInputMode.Hands ? "Hands — selected" : "Hands",
            new Vector2(-185f, 200f), new Vector2(340f, 65f), () => { controlsMode = GardenInputMode.Hands; ShowControls(); });
        menu.Button(controlsMode == GardenInputMode.Controllers ? "Controllers — selected" : "Controllers",
            new Vector2(185f, 200f), new Vector2(340f, 65f), () => { controlsMode = GardenInputMode.Controllers; ShowControls(); });
        menu.Label("Choose a demonstration to watch and hear.\nThe garden stays paused while you review.",
            new Vector2(0f, 125f), new Vector2(790f, 70f), 25);
        menu.Button("Movement", new Vector2(-185f, 25f), new Vector2(340f, 65f), () => ShowDemonstration("move"));
        menu.Button("Grab and release", new Vector2(185f, 25f), new Vector2(340f, 65f), () => ShowDemonstration("grab"));
        menu.Button("Open and close calendar", new Vector2(-185f, -75f), new Vector2(340f, 65f), () => ShowDemonstration("calendar"));
        menu.Button("Select buttons", new Vector2(185f, -75f), new Vector2(340f, 65f), () => ShowDemonstration("calendar_select"));
        menu.Button("Watering can", new Vector2(-185f, -175f), new Vector2(340f, 65f), () => ShowDemonstration("watering"));
        menu.Button("Refill water", new Vector2(185f, -175f), new Vector2(340f, 65f), () => ShowDemonstration("refill_select"));
        menu.Button("Back", new Vector2(0f, -290f), new Vector2(710f, 65f), ShowHome);
        if (!controlsIntroPlayed && menuVoice != null)
        {
            controlsIntroPlayed = true;
            menuVoice.Enqueue("menu_tutorial_intro", "controls-intro", () => visible && page == Page.Controls);
        }
    }

    private void ShowDemonstration(string control, bool right = false)
    {
        StopMenuAudio();
        page = Page.Demonstration;
        string id = "controls_" + control + (controlsMode == GardenInputMode.Hands ? "_hands" : "_controllers");
        bool hasSides = control == "move" || control == "calendar";
        if (hasSides && right) id += "_right";
        GronnyAudioCatalogue.Cue cue = audioCatalogue != null ? audioCatalogue.Find(id) : null;
        previewId = id;
        menu.Clear();
        menu.Label(control switch { "move" => "Movement", "grab" => "Grab and release",
            "calendar" => "Calendar", "watering" => "Watering can", "refill_select" => "Refill water",
            _ => "Select buttons" }, new Vector2(0f, 280f), new Vector2(790f, 65f), 38);
        string description = controlsMode == GardenInputMode.Hands ? "Hand tracking" : "Touch controllers";
        if (hasSides) description += right
            ? " — right-hand preview\nLive movement and calendar still use the left side."
            : " — left side\nMatches the current movement and calendar controls.";
        menu.Label(description, new Vector2(0f, 195f), new Vector2(790f, 95f), 24);
        menu.Label(cue != null && !string.IsNullOrEmpty(cue.instruction) ? cue.instruction + "\n" + cue.instructionDetail
            : "Watch the demonstration.\nYou can replay it or stop it at any time.",
            new Vector2(0f, 30f), new Vector2(760f, 225f), 26);
        menu.Button("Replay", new Vector2(-185f, -125f), new Vector2(340f, 65f), () => PlayDemonstration(id));
        menu.Button("Stop", new Vector2(185f, -125f), new Vector2(340f, 65f), StopMenuAudio);
        if (hasSides)
            menu.Button(right ? "Preview left hand" : "Preview right hand", new Vector2(0f, -205f),
                new Vector2(710f, 65f), () => ShowDemonstration(control, !right));
        menu.Button("Back", new Vector2(0f, -290f), new Vector2(710f, 65f), ShowControls);
        PlayDemonstration(id);
    }

    private void PlayDemonstration(string id)
    {
        StopMenuAudio();
        previewId = id;
        if (menuVoice == null) return;
        menuDemonstration.SetInputMode(controlsMode);
        menuVoice.Enqueue(id, "preview-" + id, () => visible && page == Page.Demonstration && previewId == id,
            once: false, waitForAction: true);
    }

    private void StopMenuAudio()
    {
        previewId = null;
        menuVoice?.Clear();
    }

    private void ShowSound()
    {
        if (page != Page.Sound) StopMenuAudio();
        page = Page.Sound;
        menu.Clear();
        menu.Label("Sound settings", new Vector2(0f, 285f), new Vector2(790f, 65f), 38);
        VolumeRow("Overall", GardenPreferences.MasterVolume, 185f, GardenPreferences.SetMasterVolume);
        VolumeRow("Gronny", GardenPreferences.VoiceVolume, 85f, GardenPreferences.SetVoiceVolume);
        menu.Button(GardenPreferences.Muted ? "Sound: muted — select to unmute" : "Mute sound", new Vector2(0f, -15f),
            new Vector2(710f, 65f), () => { GardenPreferences.SetMuted(!GardenPreferences.Muted); ShowSound(); });
        menu.Button("Test voice", new Vector2(-185f, -105f), new Vector2(340f, 65f), () =>
        {
            StopMenuAudio();
            menuVoice?.Enqueue("settings_voice_sample", "voice-sample", () => visible && page == Page.Sound, once: false);
        });
        menu.Button("Stop voice", new Vector2(185f, -105f), new Vector2(340f, 65f), StopMenuAudio);
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
            // Snapshot the authored pose once so tracking and later rig motion cannot move the UI.
            transform.SetPositionAndRotation(top + panel.up * guidePanelGap, panel.rotation);
            menu.Root.transform.localPosition = Vector3.up * (menu.Collider.size.y * menu.Root.transform.localScale.y * 0.5f);
            launcher.Root.transform.localPosition = Vector3.up * (launcher.Collider.size.y * launcher.Root.transform.localScale.y * 0.5f);
            GardenWaterTankDisplay tank = FindFirstObjectByType<GardenWaterTankDisplay>();
            if (tank != null)
            {
                Pose aboveGuide = new(launcher.Root.transform.position, launcher.Root.transform.rotation);
                launcher.Root.transform.SetPositionAndRotation(tank.transform.position, tank.transform.rotation);
                tank.transform.SetPositionAndRotation(aboveGuide.position, aboveGuide.rotation);
            }
            return;
        }
        // A scene without the guide keeps a usable pause menu at the original launch station.
    }

    private void OnDestroy()
    {
        StopMenuAudio();
        suspension?.Release();
        if (active == this) active = null;
    }
}
