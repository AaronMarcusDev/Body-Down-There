using UnityEngine;
using UnityEngine.AI;

public class EnemyCatchHandler : MonoBehaviour
{
    [Header("Jumpscare & Respawn Instellingen")]
    [SerializeField] private GameObject robotJumpscareCamera;
    [SerializeField] private Transform robotSpawnPoint;

    [Header("Catch Settings")]
    [SerializeField] private float catchDistance = 1.8f; // Hier regel je hoe dichtbij hij moet zijn om te grijpen

    private EnemyStateMachine stateMachine;
    private bool isCaught = false;

    // Publieke properties voor de GameManager
    public GameObject RobotJumpscareCamera => robotJumpscareCamera;
    public Transform RobotSpawnPoint => robotSpawnPoint;

    private void Awake()
    {
        stateMachine = GetComponent<EnemyStateMachine>();
    }

    private void Update()
    {
        if (isCaught) return;

        // Als de robot een StateMachine heeft, mag hij alleen vangen als hij daadwerkelijk in de Attack-state zit
        if (stateMachine != null && stateMachine.CurrentState != EnemyStateMachine.State.Attack)
        {
            return;
        }

        // Zoek de speler en check de afstand
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
        if (isCaught) return; // Voorkom dat hij dubbel triggert
        isCaught = true;

        // 1. Bevries deze robot
        FreezeEnemy();

        // 2. Roep de GameManager aan
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
        if (agent != null) agent.isStopped = true;

        AudioSource[] childAudios = GetComponentsInChildren<AudioSource>();
        foreach (var src in childAudios)
        {
            src.Stop();
        }
    }

    public void ResetEnemy(Vector3 spawnPosition)
    {
        isCaught = false; // Reset de vlag zodat de robot na de respawn weer kan vangen
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