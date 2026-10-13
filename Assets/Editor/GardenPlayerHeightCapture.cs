using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Records real Play-mode heights without requiring the player to inspect the rig in VR.</summary>
[InitializeOnLoad]
public static class GardenPlayerHeightCapture
{
    [Serializable]
    private sealed class Sample
    {
        public string utc;
        public float playSeconds;
        public bool headsetPresent;
        public bool headsetWorn;
        public bool positionTracked;
        public string trackingOrigin;
        public Vector3 originPosition;
        public Vector3 originScale;
        public Vector3 trackingSpaceLocalPosition;
        public Vector3 trackingSpaceScale;
        public Vector3 eyeLocalPosition;
        public Vector3 eyeWorldPosition;
        public Vector3 headPoseOffset;
        public bool heightCalibrated;
        public float initialEyeHeight;
        public float calibrationOffset;
        public bool hasCharacter;
        public Vector3 capsulePosition;
        public float capsuleHeight;
        public float capsuleFeetY;
        public bool grounded;
        public string groundCollider;
        public string groundLayer;
        public int collisionMask;
        public bool floorFound;
        public float floorY;
    }

    private static bool recording;
    private static bool capturedWhileWorn;
    private static bool failed;
    private static double nextSampleAt;
    private static string directory;
    private static OVRCameraRig rig;
    private static Oculus.Interaction.Locomotion.CharacterController character;
    private static Collider floor;

    static GardenPlayerHeightCapture()
    {
        EditorApplication.update += Update;
    }

    private static void Update()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "Garden_Moves")
        {
            recording = false;
            failed = false;
            rig = null;
            character = null;
            floor = null;
            return;
        }
        if (failed || EditorApplication.timeSinceStartup < nextSampleAt) return;
        nextSampleAt = EditorApplication.timeSinceStartup + 0.5;
        try
        {
            if (!recording)
            {
                directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", "Validation"));
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "GardenPlayerHeight.jsonl"), string.Empty);
                File.WriteAllText(Path.Combine(directory, "GardenPlayerHeight.json"),
                    "{\"status\":\"Waiting for headset tracking in Play mode\"}");
                capturedWhileWorn = false;
                recording = true;
            }
            Capture();
        }
        catch (Exception error)
        {
            failed = true;
            Debug.LogWarning("Garden height capture stopped: " + error.Message);
        }
    }

    private static void Capture()
    {
        if (rig == null) rig = UnityEngine.Object.FindFirstObjectByType<OVRCameraRig>();
        if (rig == null || rig.gameObject.scene.name != "Garden_Moves" ||
            rig.centerEyeAnchor == null || rig.trackingSpace == null) return;
        if (character == null)
            character = rig.GetComponentInChildren<Oculus.Interaction.Locomotion.CharacterController>();
        if (floor == null)
            foreach (Collider candidate in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if (candidate.gameObject.scene == rig.gameObject.scene && candidate.name == "Floor" &&
                    candidate.enabled && !candidate.isTrigger)
                {
                    floor = candidate;
                    break;
                }

        OVRManager manager = rig.GetComponent<OVRManager>();
        Sample sample = new()
        {
            utc = DateTime.UtcNow.ToString("O"),
            playSeconds = Time.unscaledTime,
            headsetPresent = OVRManager.isHmdPresent,
            headsetWorn = manager != null && manager.isUserPresent,
            trackingOrigin = manager != null ? manager.trackingOriginType.ToString() : "No OVRManager",
            originPosition = rig.transform.position,
            originScale = rig.transform.lossyScale,
            trackingSpaceLocalPosition = rig.trackingSpace.localPosition,
            trackingSpaceScale = rig.trackingSpace.lossyScale,
            eyeLocalPosition = rig.centerEyeAnchor.localPosition,
            eyeWorldPosition = rig.centerEyeAnchor.position,
            headPoseOffset = manager != null ? manager.headPoseRelativeOffsetTranslation : Vector3.zero,
            hasCharacter = character != null
        };
        sample.positionTracked = sample.headsetPresent && OVRPlugin.GetNodePositionTracked(OVRPlugin.Node.EyeCenter);
        GardenPlayerHeightCalibration calibration = rig.GetComponent<GardenPlayerHeightCalibration>();
        if (calibration != null)
        {
            sample.heightCalibrated = calibration.IsCalibrated;
            sample.initialEyeHeight = calibration.InitialEyeHeight;
            sample.calibrationOffset = calibration.AppliedOffset;
        }
        if (character != null)
        {
            sample.capsulePosition = character.Pose.position;
            sample.capsuleHeight = character.Height;
            sample.capsuleFeetY = character.Pose.position.y - character.Height * 0.5f - character.SkinWidth;
            sample.grounded = character.IsGrounded;
            Collider ground = character.GroundHit.collider;
            sample.groundCollider = ground != null ? ground.name : "None";
            sample.groundLayer = ground != null ? LayerMask.LayerToName(ground.gameObject.layer) : "None";
            sample.collisionMask = character.LayerMask.value;
        }
        if (floor != null)
        {
            Vector3 rayOrigin = rig.transform.position;
            rayOrigin.y = floor.bounds.max.y + 1f;
            sample.floorFound = floor.Raycast(new Ray(rayOrigin, Vector3.down), out RaycastHit hit,
                floor.bounds.size.y + 2f);
            if (sample.floorFound) sample.floorY = hit.point.y;
        }

        File.AppendAllText(Path.Combine(directory, "GardenPlayerHeight.jsonl"), JsonUtility.ToJson(sample) + "\n");
        bool validWornSample = sample.headsetWorn && sample.positionTracked;
        // Removing the headset to return to the chat must not replace the last useful measurement.
        if (validWornSample || !capturedWhileWorn)
            File.WriteAllText(Path.Combine(directory, "GardenPlayerHeight.json"), JsonUtility.ToJson(sample, true));
        capturedWhileWorn |= validWornSample;
    }
}
