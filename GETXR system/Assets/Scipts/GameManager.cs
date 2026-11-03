using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Instellingen")]
    public Transform playerSpawnPoint;
    public Transform enemySpawnPoint;
    public float respawnDelay = 2f;

    [Header("Camera's")]
    public GameObject normalPlayerCamera;
    public GameObject jumpscareCamera;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void TriggerRespawn(GameObject player, GameObject enemy)
    {
        StartCoroutine(JumpscareAndRespawn(player, enemy));
    }

    private IEnumerator JumpscareAndRespawn(GameObject player, GameObject enemy)
    {
        // 1. Camera's wisselen
        if (normalPlayerCamera != null) normalPlayerCamera.SetActive(false);
        if (jumpscareCamera != null) jumpscareCamera.SetActive(true);

        // Zet de speler tijdelijk uit
        player.SetActive(false);

        // 2. Wacht voor de jumpscare
        yield return new WaitForSeconds(respawnDelay);

        // 3. Camera's terugzetten
        if (jumpscareCamera != null) jumpscareCamera.SetActive(false);
        if (normalPlayerCamera != null) normalPlayerCamera.SetActive(true);

        // 4. Verplaats de speler VEILIG (met CharacterController check)
        CharacterController charController = player.GetComponent<CharacterController>();
        if (charController != null) charController.enabled = false; // Zet uit voor teleport

        player.transform.position = playerSpawnPoint.position;
        player.transform.rotation = playerSpawnPoint.rotation;

        if (charController != null) charController.enabled = true;  // Zet weer aan

        // 5. Verplaats de robot VEILIG met agent.Warp()
        if (enemy != null && enemySpawnPoint != null)
        {
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            NavMeshMover mover = enemy.GetComponent<NavMeshMover>();

            if (mover != null) mover.Stop();

            if (agent != null)
            {
                // GEBRUIK WARP IN PLAATS VAN TRANSFORM.POSITION!
                agent.Warp(enemySpawnPoint.position);

                // Zet ook direct de rotatie goed op de parent
                enemy.transform.rotation = enemySpawnPoint.rotation;
            }
        }

        // 6. Zet de speler weer aan (zorg dat je speler-script geen extra vertraging heeft in Start)
        player.SetActive(true);
    }
}