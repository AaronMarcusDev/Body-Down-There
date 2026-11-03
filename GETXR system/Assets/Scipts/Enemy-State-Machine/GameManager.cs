using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Respawn Instellingen")]
    public Transform playerSpawnPoint;
    public Transform enemySpawnPoint; // Optioneel: waar de robot opnieuw begint
    public float respawnDelay = 2f;   // Tijd in seconden voordat de game reset

    private void Awake()
    {
        // Singleton patroon zodat andere scripts hier makkelijk bij kunnen
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void TriggerRespawn(GameObject player, GameObject enemy)
    {
        StartCoroutine(RespawnRoutine(player, enemy));
    }

    private IEnumerator RespawnRoutine(GameObject player, GameObject enemy)
    {
        // 1. Sch tijdelijk beweging/input uit (optioneel)
        player.SetActive(false);
        enemy.SetActive(false);

        // Wacht een paar seconden (bijv. voor een scherm-fade of animatie)
        yield return new WaitForSeconds(respawnDelay);

        // 2. Verplaats de speler naar de startplek
        player.transform.position = playerSpawnPoint.position;
        player.transform.rotation = playerSpawnPoint.rotation;

        // 3. Verplaats de robot terug naar zijn startplek
        if (enemy != null && enemySpawnPoint != null)
        {
            // Als je een NavMeshAgent gebruikt, moet je de agent even uitzetten tijdens teleporteren
            UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            enemy.transform.position = enemySpawnPoint.position;
            enemy.transform.rotation = enemySpawnPoint.rotation;

            if (agent != null) agent.enabled = true;
        }

        // 4. Zet alles weer aan
        player.SetActive(true);
        enemy.SetActive(true);
    }
}