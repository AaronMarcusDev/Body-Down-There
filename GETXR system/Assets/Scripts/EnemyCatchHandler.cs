using UnityEngine;
using UnityEngine.AI;

public class EnemyCatchHandler : MonoBehaviour
{
    [Header("Jumpscare & Respawn Instellingen")]
    [SerializeField] private GameObject robotJumpscareCamera;
    [SerializeField] private Transform robotSpawnPoint;

    [Header("Catch Settings")]
    [SerializeField] private float catchDistance = 1.8f; // the distance at which it triggers as a catch

    // Public
    public GameObject RobotJumpscareCamera => robotJumpscareCamera;
    public Transform RobotSpawnPoint => robotSpawnPoint;

    // Private
    private EnemyStateMachine stateMachine;
    private bool isCaught = false;


    private void Awake()
    {
        stateMachine = GetComponent<EnemyStateMachine>();
    }

    private void Update()
    {
        if (isCaught) return;

        // If the robot has a StateMachine, it is only allowed to catch if it is in its attack state :O
        if (stateMachine != null && stateMachine.CurrentState != EnemyStateMachine.State.Attack)
        {
            return;
        }

        // Get the player from the game by its tag and check distance
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerObj.transform.position);

            if (distanceToPlayer <= catchDistance)
            {
                CatchPlayer(playerObj);
            }
        }
    }

    public void CatchPlayer(GameObject player)
    {
        if (isCaught) return; // Stop double triggers, like in GETXR A4 I believe
        isCaught = true;

        FreezeEnemy();

        // Then the game logic
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerJumpscare(player, this);
        }
    }

    private void FreezeEnemy()
    {
        if (stateMachine != null)
        {
            stateMachine.FreezeForJumpscare();
            return;
        }

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
        }

        AudioSource[] childAudios = GetComponentsInChildren<AudioSource>();
        foreach (var src in childAudios)
        {
            src.Stop();
        }
    }

    public void ResetEnemy(Vector3 spawnPosition)
    {
        isCaught = false; // We do want it to work again in another try of course
        transform.position = spawnPosition;

        if (stateMachine != null)
        {
            stateMachine.ResetToSpawn(spawnPosition);
            return;
        }

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.isStopped = false;
    }
}