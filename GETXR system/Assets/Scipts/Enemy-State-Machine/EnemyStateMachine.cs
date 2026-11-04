using UnityEngine;

[RequireComponent(typeof(VisionSensor))]
[RequireComponent(typeof(DetectionMeter))]
[RequireComponent(typeof(NavMeshMover))]
[RequireComponent(typeof(AudioSource))]
public class EnemyStateMachine : MonoBehaviour
{
    public enum State
    {
        Patrol,
        Alert,
        Investigate,
        Attack
    }

    public State CurrentState { get; private set; } = State.Patrol;

    private DetectionMeter _meter;
    private VisionSensor _vision;
    private NavMeshMover _mover;
    private AudioSource _audioSource;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip passiveSound; // Geluid voor Patrol, Alert en Investigate
    [SerializeField] private AudioClip chaseSound;   // Geluid voor Attack

    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolWaypoints; // Sleep hier je waypoints in vanuit de Inspector
    private int currentWaypointIndex = 0;
    [SerializeField] private float waypointReachDistance = 1.5f;

    // Investigate settings
    [SerializeField] private float investigateWaitTime = 3.5f;
    private Vector3 _investigatePosition;
    private float _investigateTimer;

    // Jumpscare freeze & cooldown vlaggen
    private bool isJumpscareActive = false;
    private float respawnCooldownTimer = 0f;

    private void Awake()
    {
        // Retrieve the components
        _meter = GetComponent<DetectionMeter>();
        _vision = GetComponent<VisionSensor>();
        _mover = GetComponent<NavMeshMover>();
        _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        // Start direct met het passive geluid als dat is ingesteld
        PlayPassiveSound();

        // Als er waypoints zijn, begin direct met patrouilleren
        if (patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            MoveToNextWaypoint();
        }
    }

    private void Update()
    {
        if (isJumpscareActive) return; // Als de jumpscare bezig is, doet de robot niets meer

        // Als we net zijn gerespawned, telt deze timer af om audio-glitches en direct aanvallen te voorkomen
        if (respawnCooldownTimer > 0f)
        {
            respawnCooldownTimer -= Time.deltaTime;
            return;
        }

        UpdateState();
    }

    private void UpdateState()
    {
        // Determine the state based on the meter
        State newState = CurrentState;

        if (_meter.IsAttacking)
        {
            newState = State.Attack;
        }
        else if (_meter.IsInvestigating)
        {
            newState = State.Investigate;
        }
        else if (_meter.IsAlerted)
        {
            newState = State.Alert;
        }
        else
        {
            newState = State.Patrol;
        }

        // On state change
        if (newState != CurrentState)
        {
            State oldState = CurrentState;
            CurrentState = newState;
            Debug.Log($"<StateMachine> {name} is in de {CurrentState} state.");

            // Handel audio-wissels af bij een state verandering
            HandleAudioStateChange(oldState, CurrentState);

            switch (CurrentState)
            {
                case State.Patrol:
                    MoveToNextWaypoint();
                    Debug.Log("<State> Patrol: Volgend waypoint opzoeken");
                    break;
                case State.Alert:
                    _mover.Stop();
                    Debug.Log("<State> Alert");
                    break;
                case State.Investigate:
                    _investigatePosition = _vision.LastSeenPosition;
                    _investigateTimer = investigateWaitTime;
                    _mover.MoveTo(_investigatePosition);
                    Debug.Log("<State> Investigate: Op weg naar laatste positie");
                    break;
                case State.Attack:
                    Debug.Log("<State> Attack");
                    break;
            }
        }

        // Movement for the states
        if (CurrentState == State.Patrol)
        {
            // Controleer of we dicht genoeg bij het huidige waypoint zijn om door te schakelen
            if (patrolWaypoints != null && patrolWaypoints.Length > 0)
            {
                float distanceToWaypoint = Vector3.Distance(transform.position, patrolWaypoints[currentWaypointIndex].position);

                if (distanceToWaypoint <= waypointReachDistance)
                {
                    // Pak het volgende waypoint in een eindeloze lus (%)
                    currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
                    MoveToNextWaypoint();
                }
            }
        }
        else if (CurrentState == State.Attack)
        {
            if (_vision.CanSeePlayer)
            {
                _mover.MoveTo(_vision.LastSeenPosition);
            }
        }
        else if (CurrentState == State.Investigate)
        {
            float distanceToTarget = Vector3.Distance(transform.position, _investigatePosition);

            if (distanceToTarget <= 1.5f)
            {
                _mover.Stop();

                _investigateTimer -= Time.deltaTime;
                if (_investigateTimer <= 0f)
                {
                    // Timer is op
                }
            }
        }
    }

    private void MoveToNextWaypoint()
    {
        if (patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            if (_mover != null)
            {
                _mover.MoveTo(patrolWaypoints[currentWaypointIndex].position);
            }
        }
    }

    private void HandleAudioStateChange(State oldState, State newState)
    {
        if (_audioSource == null) return;

        // Als we naar Attack gaan, spelen we het chase-geluid af
        if (newState == State.Attack)
        {
            if (chaseSound != null)
            {
                _audioSource.Stop();
                _audioSource.clip = chaseSound;
                _audioSource.loop = true;
                _audioSource.Play();
            }
        }
        // Als we vanuit Attack terugkeren naar een Passive state
        else if (oldState == State.Attack)
        {
            PlayPassiveSound();
        }
    }

    private void PlayPassiveSound()
    {
        if (_audioSource == null || passiveSound == null) return;

        if (_audioSource.clip != passiveSound || !_audioSource.isPlaying)
        {
            _audioSource.Stop();
            _audioSource.clip = passiveSound;
            _audioSource.loop = true;
            _audioSource.Play();
        }
    }

    // --- FUNCTIES AANGEROEPEN DOOR DE GAMEMANAGER ---
    public void FreezeForJumpscare()
    {
        isJumpscareActive = true;

        // Stop alle audiobronnen op de robot direct
        if (_audioSource != null) _audioSource.Stop();

        AudioSource[] allAudioSources = GetComponentsInChildren<AudioSource>();
        foreach (AudioSource src in allAudioSources)
        {
            src.Stop();
        }

        if (_mover != null) _mover.Stop();
    }

    public void ResetToSpawn(Vector3 spawnPosition)
    {
        isJumpscareActive = false;
        respawnCooldownTimer = 1.5f; // Voorkomt dat de robot direct na respawn triggert of geluid lekt

        // Verplaats de robot naar zijn eigen spawnpoint
        transform.position = spawnPosition;

        // Zet de state weer terug naar Patrol
        CurrentState = State.Patrol;

        // Stop beweging, start passive geluid en laat hem direct weer zijn ronde lopen
        if (_mover != null) _mover.Stop();
        PlayPassiveSound();
        MoveToNextWaypoint();
    }

    public void UnfreezeEnemy()
    {
        isJumpscareActive = false;
        PlayPassiveSound();
    }
}