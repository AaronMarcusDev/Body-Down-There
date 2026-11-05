using UnityEngine;

/// <summary>
/// Luistert naar de voetstappen van de speler. Staat de speler binnen hoorafstand van de robot
/// en valt de stap NIET op een tik/bel van de metronoom? Dan hoort de robot hem.
/// </summary>
public class RhythmValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LookAndStepController player;
    [SerializeField] private MetronomeController metronome;
    [SerializeField] private MetronomeRobot robot;

    [Header("Settings")]
    [Tooltip("Binnen deze afstand kan de robot de speler horen (A3: hazard zone)")]
    [SerializeField] private float hearingRadius = 12f;
    [Tooltip("Hoe precies je moet zijn, in seconden (voor en na de tik). Kleiner = moeilijker.")]
    [SerializeField] private float tolerance = 0.12f;
    [Tooltip("Compenseert input/audio latency. Positief = stappen worden als eerder gezien.")]
    [SerializeField] private float latencyOffset = 0f;

    [Header("Debug")]
    [SerializeField] private bool logResults = true;

    private void OnEnable()
    {
        if (player != null) player.StepTaken += OnPlayerStep;
    }

    private void OnDisable()
    {
        if (player != null) player.StepTaken -= OnPlayerStep;
    }

    private void OnPlayerStep()
    {
        if (metronome == null || robot == null || !metronome.IsRunning) return;

        // Robot is al aan het jagen: niet terugzetten naar Triggered
        if (robot.State == RobotState.Angry) return;

        // Te ver weg om te horen
        float dist = Vector3.Distance(player.transform.position, robot.transform.position);
        if (dist > hearingRadius) return;

        double stepTime = AudioSettings.dspTime - latencyOffset;
        bool onBeat = metronome.IsOnBeat(stepTime, tolerance, out float offset);

        if (logResults)
            Debug.Log(onBeat ? $"On beat ({offset * 1000f:F0} ms)" : $"MISS ({offset * 1000f:F0} ms)");

        if (!onBeat)
            robot.HearNoise(player.transform.position);
    }
}
