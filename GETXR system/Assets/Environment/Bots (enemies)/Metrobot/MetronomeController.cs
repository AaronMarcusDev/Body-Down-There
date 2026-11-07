using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class MetronomeController : MonoBehaviour
{
    private class Tick
    {
        public double time;
        public int slot;
        public bool bell;
        public bool fired;
    }

    [Header("Tempo (stays constant as long as the robot is not angry)")]
    [SerializeField] private float bpm = 100f;
    [SerializeField] private int beatsPerBar = 4;

    [Header("Rhythms")]
    [SerializeField] private MetronomeBeatData[] patterns;
    [Tooltip("How many bars in a row the same pattern plays before a different one is picked")]
    [SerializeField] private int barsPerPattern = 4;

    [Header("Audio")]
    [SerializeField] private AudioClip bellClip;
    [SerializeField] private AudioClip tickClip;
    [Range(0f, 1f)] [SerializeField] private float bellVolume = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float tickVolume = 1f;
    [Tooltip("Within this distance the sound is at full volume")]
    [SerializeField] private float minDistance = 3f;
    [Tooltip("Beyond this distance you hear nothing")]
    [SerializeField] private float maxDistance = 25f;
    [Tooltip("1 = fully 3D (hard left/right), lower = softer and more natural. 0.7 - 0.85 is a good start.")]
    [Range(0f, 1f)] [SerializeField] private float spatialBlend = 0.8f;
    [Tooltip("Spreads the sound over more speakers so it doesn't jump sharply from left to right (0-360)")]
    [Range(0f, 360f)] [SerializeField] private float spread = 90f;
    [Tooltip("Number of AudioSources used to schedule sounds one after another (at least 16 if you use Angry)")]
    [SerializeField] private int voices = 16;
    [Tooltip("How far ahead we schedule sounds (sec). Must be larger than the validator's tolerance.")]
    [SerializeField] private double lookAhead = 0.3;
    [SerializeField] private bool playOnStart = true;

    [Header("Warning before rhythm change")]
    [Tooltip("Sound that plays in the LAST bar of a pattern, right before the rhythm changes")]
    [SerializeField] private AudioClip warningClip;
    [Range(0f, 1f)] [SerializeField] private float warningVolume = 0.9f;

    [Header("Angry (metronome going wild)")]
    [Tooltip("The robot whose state we follow. Found automatically on this object.")]
    [SerializeField] private MetronomeRobot robot;
    [Tooltip("1.6 = 60% faster than normal")]
    [SerializeField] private float angryBpmMultiplier = 1.6f;
    [Range(0f, 1f)] [SerializeField] private float angryBellVolume = 0.45f;

    /// <summary>(slotIndex, isBell) - fired exactly at the moment of the tick. Handy for animations.</summary>
    public event Action<int, bool> TickFired;

    public bool IsRunning { get; private set; }
    public bool IsAngry => isAngry;

    private float CurrentBpm => isAngry ? bpm * angryBpmMultiplier : bpm;
    public double BarDuration => beatsPerBar * 60.0 / CurrentBpm;

    private AudioSource[] sources;
    private int voiceIndex;
    private readonly List<Tick> ticks = new List<Tick>();
    private double nextBarTime;

    private MetronomeBeatData currentPattern;
    private int barsRemaining;
    private bool isAngry;

    private void Awake()
    {
        if (robot == null) robot = GetComponent<MetronomeRobot>();

        // In Angry up to ~7 bells sound at the same time, so create enough voices
        sources = new AudioSource[Mathf.Max(16, voices)];
        for (int i = 0; i < sources.Length; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = spatialBlend; // 3D: the player hears the robot from where it stands
            s.spread = spread;
            s.dopplerLevel = 0f; // NO Doppler: it caused the 'distorted' pitch effect when moving
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = minDistance;
            s.maxDistance = maxDistance;
            sources[i] = s;
        }
    }

    private void Start()
    {
        if (playOnStart) StartMetronome();
    }

    public void StartMetronome()
    {
        ResetSchedule(0.5);
        IsRunning = true;
    }

    public void StopMetronome()
    {
        IsRunning = false;
        foreach (var s in sources) s.Stop();
        ticks.Clear();
    }

    private void ResetSchedule(double delay)
    {
        foreach (var s in sources) s.Stop(); // also cancels sounds that were already scheduled
        ticks.Clear();
        currentPattern = null;
        barsRemaining = 0;
        nextBarTime = AudioSettings.dspTime + delay;
    }

    private void Update()
    {
        if (!IsRunning) return;

        // Does the robot switch to/from Angry? Then reschedule immediately with the new tempo
        bool angryNow = robot != null && robot.State == RobotState.Angry;
        if (angryNow != isAngry)
        {
            isAngry = angryNow;
            ResetSchedule(0.05);
        }

        double now = AudioSettings.dspTime;

        // Schedule bars ahead of time so the sound is sample-accurate
        while (nextBarTime < now + lookAhead)
            ScheduleBar();

        // Fire events at the moment the tick sounds
        for (int i = 0; i < ticks.Count; i++)
        {
            Tick t = ticks[i];
            if (!t.fired && t.time <= now)
            {
                t.fired = true;
                TickFired?.Invoke(t.slot, t.bell);
            }
        }

        // Clean up old ticks (we keep a few around for the validator)
        ticks.RemoveAll(t => t.time < now - 2.0);
    }

    private void ScheduleBar()
    {
        double slotDuration = BarDuration / MetronomeBeatData.SlotsPerBar;

        if (isAngry)
        {
            // Going wild: a loud bell on EVERY whole and half beat (1 & 2 & 3 & 4 &)
            for (int slot = 0; slot < MetronomeBeatData.SlotsPerBar; slot++)
            {
                double t = nextBarTime + slot * slotDuration;
                PlayScheduled(bellClip, angryBellVolume, t);
                ticks.Add(new Tick { time = t, slot = slot, bell = true });
            }
            nextBarTime += BarDuration;
            return;
        }

        // Normal: is the current pattern 'used up'? Then pick a new pattern
        if (barsRemaining <= 0)
        {
            currentPattern = PickNewPattern();
            barsRemaining = Mathf.Max(1, barsPerPattern);
        }
        barsRemaining--;

        // Last bar of this pattern? Then the warning plays so that it ends exactly on the next bell
        if (warningClip != null && barsRemaining == 0 && patterns != null && patterns.Length > 1)
        {
            double warnStart = nextBarTime + BarDuration - warningClip.length;
            PlayScheduled(warningClip, warningVolume, warnStart);
        }

        for (int slot = 0; slot < MetronomeBeatData.SlotsPerBar; slot++)
        {
            bool bell = slot == 0;
            if (!bell && (currentPattern == null || !currentPattern.IsTick(slot))) continue;

            double t = nextBarTime + slot * slotDuration;
            PlayScheduled(bell ? bellClip : tickClip, bell ? bellVolume : tickVolume, t);
            ticks.Add(new Tick { time = t, slot = slot, bell = bell });
        }

        nextBarTime += BarDuration;
    }

    /// <summary>Picks a random pattern, never the same as the previous one.</summary>
    private MetronomeBeatData PickNewPattern()
    {
        if (patterns == null || patterns.Length == 0) return null;
        if (patterns.Length == 1) return patterns[0];

        MetronomeBeatData pick;
        do { pick = patterns[Random.Range(0, patterns.Length)]; }
        while (pick == currentPattern);

        return pick;
    }

    private void PlayScheduled(AudioClip clip, float volume, double dspTime)
    {
        if (clip == null) return;
        AudioSource s = sources[voiceIndex];
        voiceIndex = (voiceIndex + 1) % sources.Length;
        s.clip = clip;
        s.volume = volume;
        s.PlayScheduled(dspTime);
    }

    /// <summary>
    /// Does this moment (dspTime) fall within 'tolerance' seconds of a tick or bell?
    /// offset = difference from the nearest tick (negative = too early, positive = too late).
    /// </summary>
    public bool IsOnBeat(double time, float tolerance, out float offset)
    {
        offset = float.MaxValue;
        foreach (Tick t in ticks)
        {
            float d = (float)(time - t.time);
            if (Mathf.Abs(d) < Mathf.Abs(offset)) offset = d;
        }
        return Mathf.Abs(offset) <= tolerance;
    }
}