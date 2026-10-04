using System;
using UnityEngine;

public enum GardenInputMode { Unknown, Hands, Controllers }
public enum GardenControlDemo { None, Move, Grab, Calendar, CalendarSelect, Water, Upright }
public enum GardenDemoPose { Open, Pinch, Curled, Point, ThumbFree }

/// <summary>References the original audio and Meta visual assets without moving or duplicating them.</summary>
[CreateAssetMenu(menuName = "Garden/Gronny audio catalogue")]
public sealed class GronnyAudioCatalogue : ScriptableObject
{
    [Serializable]
    public sealed class Cue
    {
        public string id;
        public AudioClip clip;
        public GardenInputMode mode;
        public GardenControlDemo demo;
        [Min(0)] public int priority;
        [Range(0f, 1f)] public float volume = 1f;
        [Min(0f)] public float cooldown = 30f;
        public bool assisted;
        public string controllerTarget;
        public Step[] steps = Array.Empty<Step>();
    }

    [Serializable]
    public sealed class Step
    {
        [Min(0f)] public float at;
        public GardenDemoPose pose;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 thumbRotation;
        public string label;
        [Range(0f, 1f)] public float press;
    }

    [Serializable]
    public sealed class HandVisual
    {
        public GameObject prefab;
        public string animationRoot;
        public AnimationClip pinch;
        public AnimationClip curled;
        public AnimationClip point;
        public AnimationClip thumbFree;
    }

    public Cue[] cues = Array.Empty<Cue>();
    public HandVisual leftHand = new();
    public HandVisual rightHand = new();
    public GameObject leftController;
    public GameObject rightController;
    public AnimationClip controllerStick;
    public AnimationClip controllerGrip;
    public AnimationClip controllerY;
    public AnimationClip controllerTrigger;
    public Material demonstrationMaterial;
    [Header("Playback")]
    [Range(0f, 1f)] public float voiceVolume = 0.85f;
    [Range(0.75f, 1.25f)] public float pitch = 1f;
    [Min(0f)] public float gapSeconds = 0.35f;
    [Header("Progressive help")]
    [Min(5f)] public float idleHelpSeconds = 30f;
    [Min(5f)] public float independentHelpSeconds = 55f;
    [Header("VR presentation")]
    public Vector3 viewOffset = new(0.34f, -0.18f, 0.85f);
    public Vector3 handEulerAngles = new(0f, 90f, -75f);
    public Vector3 controllerEulerAngles = new(65f, 180f, 0f);
    [Range(0.5f, 2f)] public float visualScale = 1.15f;
    [Min(0.01f)] public float blendSeconds = 0.35f;

    public Cue Find(string id) => Array.Find(cues, cue => cue != null && cue.id == id);
}
