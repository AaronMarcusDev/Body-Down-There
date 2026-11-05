using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    // Investigate settings
    [SerializeField] private float investigateWaitTime = 3.5f;
    private Vector3 _investigatePosition;
    private float _investigateTimer;

    [Header("Attack Screen Effect")]
    [Tooltip("Sleep hier het UI Panel naartoe dat rood moet worden.")]
    [SerializeField] private Image redOverlay;
    [Tooltip("Hoe lang (in seconden) de Attack state mag duren voordat het scherm volledig rood is en het level herlaadt.")]
    [SerializeField] private float timeToFullRed = 5f;

    private float _attackDangerLevel = 0f; // Loopt op in Attack, zakt daarbuiten

    private void Awake()
    {
        // Retrieve the components
        _meter = GetComponent<DetectionMeter>();
        _vision = GetComponent<VisionSensor>();
        _mover = GetComponent<NavMeshMover>();

        _animator = GetComponent<Animator>();
        ResetStompTimer();

        if (redOverlay != null)
        {
            Color c = redOverlay.color;
            c.a = 0f;
            redOverlay.color = c;
        }
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
                    _investigatePosition = _vision.LastSeenPosition;
                    _investigateTimer = investigateWaitTime;
                    _mover.MoveTo(_investigatePosition);
                    Debug.Log("<State> Investigate");
                    break;
                case State.Attack:
                    Debug.Log("<State> Attack");
                    break;
            }
            _animator.SetInteger(StateHash, (int)CurrentState);
        }

        // Movement & logic per state
        if (CurrentState == State.Attack)
        {
            if (_vision.CanSeePlayer) 
            {
                _mover.MoveTo(_vision.LastSeenPosition);
            }

            // Laat het scherm roder worden zolang we in Attack zijn
            if (redOverlay != null)
            {
                _attackDangerLevel += Time.deltaTime / timeToFullRed;
                _attackDangerLevel = Mathf.Clamp01(_attackDangerLevel);

                Color c = redOverlay.color;
                c.a = _attackDangerLevel;
                redOverlay.color = c;

                if (_attackDangerLevel >= 1f)
                {
                    HandleGameOver();
                }
            }
        }
        else
        {
            // Als we NIET in Attack zijn, laat het rode scherm weer geleidelijk wegtrekken naar transparant
            if (redOverlay != null && _attackDangerLevel > 0f)
            {
                _attackDangerLevel -= Time.deltaTime * 1.5f; // Gaat iets sneller weg dan dat het opbouwt
                _attackDangerLevel = Mathf.Clamp01(_attackDangerLevel);

                Color c = redOverlay.color;
                c.a = _attackDangerLevel;
                redOverlay.color = c;
            }
        }

        if (CurrentState == State.Investigate)
        {
            float distanceToTarget = Vector3.Distance(transform.position, _investigatePosition);

            if (distanceToTarget <= 1.5f)
            {
                _mover.Stop();
                _investigateTimer -= Time.deltaTime;
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

    private void HandleGameOver()
    {
        Debug.Log("Attack state duurde te lang! Level herladen.");
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        int previousSceneIndex = SceneManager.GetActiveScene().buildIndex - 1;
        if (previousSceneIndex >= 0)
        {
            SceneManager.LoadScene(previousSceneIndex);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}