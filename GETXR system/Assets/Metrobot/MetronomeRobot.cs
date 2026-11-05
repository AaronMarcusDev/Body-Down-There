using UnityEngine;
using UnityEngine.AI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum RobotState
{
    Patrolling,
    Triggered,
    Angry
}

[RequireComponent(typeof(NavMeshAgent))]
public class MetronomeRobot : MonoBehaviour
{
    [Header("State (live aanpasbaar in de Inspector tijdens Play)")]
    [SerializeField] private RobotState state = RobotState.Patrolling;

    [Header("Patrolling")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitTimeAtWaypoint = 2f;
    [SerializeField] private bool randomWaypoints = false;

    [Header("Triggered")]
    [SerializeField] private float triggeredDuration = 2.5f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip triggeredSound;
    [Tooltip("Uit = blijft Triggered tot jij de state handmatig wijzigt (handig voor testen)")]
    [SerializeField] private bool autoLeaveTriggered = false;
    [SerializeField] private RobotState stateAfterTriggered = RobotState.Angry;

    [Header("Angry")]
    [SerializeField] private Transform player;
    [SerializeField] private float angrySpeed = 5f;
    [SerializeField] private float repathInterval = 0.2f;
    [SerializeField] private float catchDistance = 1.2f;
    [Tooltip("Hoelang de robot boos blijft (sec) voordat hij kalmeert. 0 = blijft boos tot je zelf wisselt.")]
    [SerializeField] private float angryDuration = 10f;
    [Tooltip("Is de speler dichter dan dit bij de robot, dan loopt de boos-timer niet af (hij blijft jagen)")]
    [SerializeField] private float keepAngryDistance = 6f;
    [Tooltip("Na het kalmeren hoort de robot even niks (sec), zodat de speler kan wegkomen")]
    [SerializeField] private float calmDownGrace = 4f;

    [Header("Feel - Agent (snappiness)")]
    [SerializeField] private float agentAngularSpeed = 720f;  // hoe snel de agent zelf draait tijdens lopen
    [SerializeField] private float agentAcceleration = 40f;   // hoe snel hij optrekt/remt

    [Header("Feel - Triggered snap")]
    [SerializeField] private float reactionDelay = 0.35f;     // freeze voordat hij draait
    [SerializeField] private float snapSharpness = 20f;       // hoger = sneller en harder draaien
    [SerializeField] private float twitchAmount = 4f;         // schok in graden na de draai (0 = uit)
    [SerializeField] private float twitchDuration = 0.25f;    // hoe lang die schok duurt

    [Header("Animation")]
    [Tooltip("Wordt automatisch gezocht op dit object of een child als je hem leeg laat")]
    [SerializeField] private Animator animator;
    [Tooltip("Naam van de Int-parameter in de Animator (0 = Idle, 1 = Walking, 2 = Angry)")]
    [SerializeField] private string animParameter = "AnimState";

    [Header("Model")]
    [Tooltip("Het visuele model (child, liefst een niet-geanimeerde Pivot). Leeg = de Animator-transform wordt gebruikt")]
    [SerializeField] private Transform visual;
    [Tooltip("Extra draai van het model rond Y. -90 = kwartslag naar links, 90 = naar rechts, 180 = omgekeerd")]
    [SerializeField] private float visualYawOffset = -90f;

    [Header("Debug")]
    [SerializeField] private bool debugKeys = true;           // 1 = Patrol, 2 = Triggered, 3 = Angry

    private NavMeshAgent agent;
    private RobotState currentState;

    // Patrol
    private int waypointIndex = -1;
    private float waitTimer;
    private bool waiting;

    // Triggered
    private Vector3? lastHeardPosition;
    private Quaternion targetRotation;
    private float triggeredTimer;
    private bool snapped;
    private float shakeTimer;

    // Angry
    private float repathTimer;
    private float angryTimer;
    private float angryEndTime = -100f;

    // Animation
    private int lastAnimState = -1;

    /// <summary>De huidige state van de robot (voor andere scripts, zoals RhythmValidator en de metronoom).</summary>
    public RobotState State => currentState;

    /// <summary>True zolang de robot net gekalmeerd is en nog niks hoort (grace period na Angry).</summary>
    public bool IsDeaf => currentState != RobotState.Angry && Time.time < angryEndTime + calmDownGrace;

    /// <summary>Hoeveel seconden hij nog doof is (0 als hij weer luistert). Handig voor debug of een lamp.</summary>
    public float DeafTimeRemaining => IsDeaf ? angryEndTime + calmDownGrace - Time.time : 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Model een extra draai geven (de root met de agent blijft ongemoeid)
        if (visual == null && animator != null) visual = animator.transform;
        if (visual != null && visual != transform)
            visual.localRotation = Quaternion.Euler(0f, visualYawOffset, 0f) * visual.localRotation;

        agent.angularSpeed = agentAngularSpeed;
        agent.acceleration = agentAcceleration;
        agent.autoBraking = false; // geen zachte afremming bij waypoints
    }

    private void Start()
    {
        EnterState(state);
    }

    private void Update()
    {
        // Debug toetsen (werkt met het nieuwe en het oude Input System)
        if (debugKeys)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) state = RobotState.Patrolling;
                if (Keyboard.current.digit2Key.wasPressedThisFrame) state = RobotState.Triggered;
                if (Keyboard.current.digit3Key.wasPressedThisFrame) state = RobotState.Angry;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Alpha1)) state = RobotState.Patrolling;
            if (Input.GetKeyDown(KeyCode.Alpha2)) state = RobotState.Triggered;
            if (Input.GetKeyDown(KeyCode.Alpha3)) state = RobotState.Angry;
#endif
        }

        // Als je state in de Inspector aanpast, pikken we dat hier op
        if (state != currentState)
            EnterState(state);

        switch (currentState)
        {
            case RobotState.Patrolling: UpdatePatrolling(); break;
            case RobotState.Triggered:  UpdateTriggered();  break;
            case RobotState.Angry:      UpdateAngry();      break;
        }

        UpdateAnimator();
    }

    // ---------------------------------------------------------------
    // ANIMATIE: 0 = Idle, 1 = Walking, 2 = Angry
    // ---------------------------------------------------------------

    private void UpdateAnimator()
    {
        if (animator == null) return;

        int anim;
        switch (currentState)
        {
            case RobotState.Angry:
                anim = 2;
                break;
            case RobotState.Triggered:
                anim = 0; // stilstaan tijdens triggeren
                break;
            default: // Patrolling: lopen, behalve als hij bij een waypoint wacht
                bool noRoute = waypoints == null || waypoints.Length == 0;
                anim = (noRoute || waiting) ? 0 : 1;
                break;
        }

        if (anim != lastAnimState)
        {
            animator.SetInteger(animParameter, anim);
            lastAnimState = anim;
        }
    }

    // ---------------------------------------------------------------
    // PUBLIEKE API (voor later: voetstappen, geluid, etc.)
    // ---------------------------------------------------------------

    public void SetState(RobotState newState)
    {
        state = newState;
    }

    /// <summary>Roep dit aan als de speler een geluid maakt.</summary>
    public void HearNoise(Vector3 position)
    {
        // Boos? Dan jaagt hij al. En net gekalmeerd? Dan hoort hij even niks.
        if (currentState == RobotState.Angry) return;
        if (IsDeaf) return;

        lastHeardPosition = position;
        state = RobotState.Triggered;
        // Zat hij al in Triggered? Dan opnieuw starten.
        if (currentState == RobotState.Triggered) EnterState(RobotState.Triggered);
    }

    // ---------------------------------------------------------------
    // STATE WISSELEN
    // ---------------------------------------------------------------

    private void EnterState(RobotState newState)
    {
        // Onthoud wanneer hij stopte met boos zijn (voor de grace period)
        if (currentState == RobotState.Angry && newState != RobotState.Angry)
            angryEndTime = Time.time;

        currentState = newState;
        state = newState;

        switch (newState)
        {
            case RobotState.Patrolling:
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                waiting = false;
                GoToNextWaypoint();
                break;

            case RobotState.Triggered:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                triggeredTimer = 0f;
                snapped = false;
                shakeTimer = 0f;

                if (audioSource != null && triggeredSound != null)
                    audioSource.PlayOneShot(triggeredSound);

                // Draai richting het geluid, of anders 180 graden om
                if (lastHeardPosition.HasValue)
                {
                    Vector3 dir = lastHeardPosition.Value - transform.position;
                    dir.y = 0f;
                    targetRotation = dir.sqrMagnitude > 0.01f
                        ? Quaternion.LookRotation(dir)
                        : transform.rotation;
                }
                else
                {
                    targetRotation = transform.rotation * Quaternion.Euler(0f, 180f, 0f);
                }
                break;

            case RobotState.Angry:
                agent.isStopped = false;
                agent.speed = angrySpeed;
                repathTimer = 0f;
                angryTimer = angryDuration;
                break;
        }
    }

    // ---------------------------------------------------------------
    // PATROLLING
    // ---------------------------------------------------------------

    private void UpdatePatrolling()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                waiting = false;
                GoToNextWaypoint();
            }
            return;
        }

        bool arrived = !agent.pathPending &&
                       agent.remainingDistance <= agent.stoppingDistance + 0.1f;

        if (arrived)
        {
            waiting = true;
            waitTimer = waitTimeAtWaypoint;
        }
    }

    private void GoToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (randomWaypoints && waypoints.Length > 1)
        {
            int next;
            do { next = Random.Range(0, waypoints.Length); }
            while (next == waypointIndex);
            waypointIndex = next;
        }
        else
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
        }

        agent.SetDestination(waypoints[waypointIndex].position);
    }

    // ---------------------------------------------------------------
    // TRIGGERED
    // ---------------------------------------------------------------

    private void UpdateTriggered()
    {
        triggeredTimer += Time.deltaTime;

        // Eerst even verstijven, dan ineens snappen
        if (triggeredTimer >= reactionDelay)
        {
            if (!snapped)
            {
                // Exponentieel: begint heel snel en remt hard af op het doel
                float t = 1f - Mathf.Exp(-snapSharpness * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);

                if (Quaternion.Angle(transform.rotation, targetRotation) < 0.5f)
                {
                    transform.rotation = targetRotation;
                    snapped = true;
                    shakeTimer = 0f;
                }
            }
            else if (shakeTimer < twitchDuration && twitchAmount > 0f)
            {
                // Korte schok die snel uitdooft, daarna blijft hij strak stilstaan
                shakeTimer += Time.deltaTime;
                float decay = 1f - (shakeTimer / twitchDuration);
                float shake = Mathf.Sin(shakeTimer * 70f) * twitchAmount * decay;
                transform.rotation = targetRotation * Quaternion.Euler(0f, shake, 0f);
            }
            else
            {
                transform.rotation = targetRotation;
            }
        }

        if (!autoLeaveTriggered) return;

        if (triggeredTimer >= triggeredDuration)
            state = stateAfterTriggered;
    }

    // ---------------------------------------------------------------
    // ANGRY
    // ---------------------------------------------------------------

    private void UpdateAngry()
    {
        if (player == null) return;

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;
            agent.SetDestination(player.position);
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= catchDistance)
        {
            // TODO: game over / speler gepakt
            Debug.Log("Speler gepakt!");
        }

        // Cooldown: blijft de speler uit de buurt, dan kalmeert de robot en gaat hij weer patrouilleren
        if (angryDuration > 0f)
        {
            if (dist < keepAngryDistance) angryTimer = angryDuration; // speler nog dichtbij: timer reset
            else angryTimer -= Time.deltaTime;

            if (angryTimer <= 0f)
                state = RobotState.Patrolling;
        }
    }

    // ---------------------------------------------------------------
    // GIZMOS: zie je patrol route in de Scene view
    // ---------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.2f);

            Transform next = waypoints[(i + 1) % waypoints.Length];
            if (next != null)
                Gizmos.DrawLine(waypoints[i].position, next.position);
        }
    }
}