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
    [SerializeField] private AudioClip passiveSound;
    [SerializeField] private AudioClip chaseSound;

    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolWaypoints;
    private int currentWaypointIndex = 0;
    [SerializeField] private float waypointReachDistance = 1.5f;

    [Header("Investigate Settings")]
    [SerializeField] private float investigateWaitTime = 3.5f;
    private Vector3 _investigatePosition;
    private float _investigateTimer;

    // Jumpscare freeze & cooldown vlaggen
    private bool isJumpscareActive = false;
    private float respawnCooldownTimer = 0f;

    private void Awake()
    {
        _meter = GetComponent<DetectionMeter>();
        _vision = GetComponent<VisionSensor>();
        _mover = GetComponent<NavMeshMover>();
        _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        PlayPassiveSound();

        if (patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            MoveToNextWaypoint();
        }
    }

    private void Update()
    {
        if (isJumpscareActive) return;

        if (respawnCooldownTimer > 0f)
        {
            respawnCooldownTimer -= Time.deltaTime;
            return;
        }

        UpdateState();
    }

    private void UpdateState()
    {
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

        if (newState != CurrentState)
        {
            State oldState = CurrentState;
            CurrentState = newState;
            Debug.Log($"<StateMachine> {name} is in de {CurrentState} state.");

            HandleAudioStateChange(oldState, CurrentState);

            switch (CurrentState)
            {
                case State.Patrol:
                    MoveToNextWaypoint();
                    break;
                case State.Alert:
                    _mover.Stop();
                    break;
                case State.Investigate:
                    _investigatePosition = _vision.LastSeenPosition;
                    _investigateTimer = investigateWaitTime;
                    _mover.MoveTo(_investigatePosition);
                    break;
                case State.Attack:
                    break;
            }
        }

        if (CurrentState == State.Patrol)
        {
            if (patrolWaypoints != null && patrolWaypoints.Length > 0)
            {
                Vector2 currentPos2D = new Vector2(transform.position.x, transform.position.z);
                Vector2 targetPos2D = new Vector2(patrolWaypoints[currentWaypointIndex].position.x, patrolWaypoints[currentWaypointIndex].position.z);

                if (Vector2.Distance(currentPos2D, targetPos2D) <= waypointReachDistance)
                {
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
            }
        }
    }

    private void MoveToNextWaypoint()
    {
        if (patrolWaypoints != null && patrolWaypoints.Length > 0 && _mover != null)
        {
            _mover.MoveTo(patrolWaypoints[currentWaypointIndex].position);
        }
    }

    private void HandleAudioStateChange(State oldState, State newState)
    {
        if (_audioSource == null) return;

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

    // Aangeroepen door de EnemyCatchHandler
    public void FreezeForJumpscare()
    {
        isJumpscareActive = true;

        if (_audioSource != null) _audioSource.Stop();

        AudioSource[] allAudioSources = GetComponentsInChildren<AudioSource>();
        foreach (AudioSource src in allAudioSources)
        {
            src.Stop();
        }

        if (_mover != null) _mover.Stop();
    }

    // Aangeroepen door de EnemyCatchHandler bij respawn
    public void ResetToSpawn(Vector3 spawnPosition)
    {
        isJumpscareActive = false;
        respawnCooldownTimer = 1.5f;

        CurrentState = State.Patrol;

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