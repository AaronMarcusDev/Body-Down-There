using UnityEngine;



// It does not function without these, which is why I require them
[RequireComponent(typeof(VisionSensor))]
[RequireComponent(typeof(DetectionMeter))]
[RequireComponent(typeof(NavMeshMover))]

public class EnemyStateMachineSpider : MonoBehaviour
{

    [Header("Animation")]
    [SerializeField] private float minStompDelay = 4f;
    [SerializeField] private float maxStompDelay = 10f;

    private Animator _animator;
    private float _stompTimer;

    private static readonly int StateHash = Animator.StringToHash("State");
    private static readonly int StompHash = Animator.StringToHash("Stomp");

    public State CurrentState { get; private set; } = State.Patrol;

    private DetectionMeter _meter;
    private VisionSensor _vision;
    private NavMeshMover _mover;

    // Invesitgate settings
    [SerializeField] private float investigateWaitTime = 3.5f;
    private Vector3 _investigatePosition;
    private float _investigateTimer;

    private void Awake()
    {
        // Retrieve the components
        _meter = GetComponent<DetectionMeter>();
        _vision = GetComponent<VisionSensor>();
        _mover = GetComponent<NavMeshMover>();

        _animator = GetComponent<Animator>();
        ResetStompTimer();
    }

    private void ResetStompTimer()
    {
    _stompTimer = Random.Range(minStompDelay, maxStompDelay);
    }

    private void Update() => UpdateState();

    private void UpdateState()
    {
        // Determine the state based on the meter
        State newState = CurrentState;

        // I practically just convert the booleans from the DetectionMeter to an enum state
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

        // on state change
        if (newState != CurrentState)
        {
            
            CurrentState = newState;
            Debug.Log($"<StateMachine> {name} is in the {CurrentState} state.");

            switch (CurrentState)
            {
                case State.Patrol:
                    _mover.Stop();
                    Debug.Log("<State> Patrol");
                    break;
                case State.Alert:
                    _mover.Stop();
                    Debug.Log("<State> Alert");
                    break;
                case State.Investigate:
                    // Save the last known position and walk there instead of continuous chasing like in attack
                    _investigatePosition = _vision.LastSeenPosition;
                    _investigateTimer = investigateWaitTime; // Reset timer
                    _mover.MoveTo(_investigatePosition);
                    Debug.Log("<State> Investigate");
                    break;
                case State.Attack:
                    Debug.Log("<State> Attack");
                    break;
            }
            _animator.SetInteger(StateHash, (int)CurrentState);
        }

        // Movement for the states
        if (CurrentState == State.Attack)
        {
            // Here the enemy does chase continuously
            if (_vision.CanSeePlayer) 
            {
                _mover.MoveTo(_vision.LastSeenPosition);
            }
        }
        else if (CurrentState == State.Investigate)
        {
            // Check the distance to the target first
            float distanceToTarget = Vector3.Distance(transform.position, _investigatePosition);

            if (distanceToTarget <= 1.5f)
            {
                _mover.Stop(); // Only stop when the enemy reached the target
                
                // count the timer down
                _investigateTimer -= Time.deltaTime;
                if (_investigateTimer <= 0f)
                {
                    // Here the time is up, I will leave in the if statement for the readability, but if the
                    // player is no longer visible, it will return to the patrol state eventually
                }
            }
        }
        // Random stomp only while idle (Patrol)
        if (CurrentState == State.Patrol)
        {
             _stompTimer -= Time.deltaTime;
         if (_stompTimer <= 0f)
        {
              _animator.SetTrigger(StompHash);
             ResetStompTimer();
         }
}
    }
}