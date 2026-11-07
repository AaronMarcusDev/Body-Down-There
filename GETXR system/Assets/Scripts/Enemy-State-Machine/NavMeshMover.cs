using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshMover : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private NavMeshAgent agent;

    [Tooltip("Seconds between path updates. So the time it 're-paths' :O")]
    [SerializeField] private float repathInterval = 0.25f;

    private float repathTimer;
    private Vector3 _currentDestination;
    private bool _hasDestination = false;

    void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
    }

    public void MoveTo(Vector3 position)
    {
        _currentDestination = position;
        _hasDestination = true;

        // This line is necessary for unpausing the movement, without this it would not move
        if (agent.isOnNavMesh) agent.isStopped = false;

        // Reset timer so it updates immediately
        repathTimer = 0f;
    }

    public void Stop()
    // Stops the agent movement, so also the chasing when going into the patrol state for example
    {
        _hasDestination = false;
        if (agent.isOnNavMesh)
            agent.isStopped = true;
    }

    void Update()
    {
        if (!_hasDestination || !agent.isOnNavMesh) return;

        // Countdown timer for the repath (same as in my GETXR A4)
        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
        {
            agent.SetDestination(_currentDestination);
            repathTimer = repathInterval;
        }
    }

    // Handy helper for the State Machine to check if it arrived
    public bool HasReachedDestination()
    {
        if (!_hasDestination || !agent.isOnNavMesh) return false;
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    }
}