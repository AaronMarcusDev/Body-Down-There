using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Camera Settings")]
    [SerializeField] private GameObject normalPlayerCamera;
    [SerializeField] private GameObject jumpscareCamera;

    [Header("Jumpscare Audio")]
    [SerializeField] private AudioSource jumpscareAudioSource;
    [SerializeField] private AudioClip jumpscareSound;

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform enemySpawnPoint; // <-- HIER IS HET ENEMY SPAWN POINT

    public void TriggerRespawn(GameObject player, GameObject enemy)
    {
        TriggerJumpscare(player, enemy);
    }

    public void TriggerJumpscare(GameObject player, GameObject enemy)
    {
        StartCoroutine(JumpscareAndRespawn(player, enemy));
    }

    private IEnumerator JumpscareAndRespawn(GameObject player, GameObject enemy)
    {
        // 1. Wissel camera's
        if (normalPlayerCamera != null) normalPlayerCamera.SetActive(false);
        if (jumpscareCamera != null) jumpscareCamera.SetActive(true);

        // Bevries de robot en stop direct zijn audio
        if (enemy != null)
        {
            EnemyStateMachine enemyScript = enemy.GetComponent<EnemyStateMachine>();
            if (enemyScript != null)
            {
                enemyScript.FreezeForJumpscare();
            }
        }

        // Speel het jumpscare geluid af
        if (jumpscareAudioSource != null && jumpscareSound != null)
        {
            jumpscareAudioSource.PlayOneShot(jumpscareSound);
        }

        // Zet de speler tijdelijk uit
        if (player != null) player.SetActive(false);

        // 2. Wacht tot de jumpscare-vertraging voorbij is
        yield return new WaitForSeconds(respawnDelay);

        // --- VERPLAATS DE SPELER NAAR ZIJN SPAWNPOINT ---
        if (player != null)
        {
            CharacterController charController = player.GetComponent<CharacterController>();
            if (charController != null) charController.enabled = false;

            if (playerSpawnPoint != null)
            {
                player.transform.position = playerSpawnPoint.position;
                player.transform.rotation = playerSpawnPoint.rotation;
            }

            if (charController != null) charController.enabled = true;
        }

        // --- VERPLAATS DE ROBOT NAAR ZIJN EIGEN SPAWNPOINT EN RESET HEM ---
        if (enemy != null)
        {
            EnemyStateMachine enemyScript = enemy.GetComponent<EnemyStateMachine>();
            if (enemyScript != null)
            {
                if (enemySpawnPoint != null)
                {
                    enemyScript.ResetToSpawn(enemySpawnPoint.position);
                }
                else
                {
                    enemyScript.UnfreezeEnemy(); // Fallback als je vergeten bent een spawnpoint in te stellen
                }
            }
        }

        // Zet de camera's en speler weer terug
        if (jumpscareCamera != null) jumpscareCamera.SetActive(false);
        if (normalPlayerCamera != null) normalPlayerCamera.SetActive(true);
        if (player != null) player.SetActive(true);

        // back to menu
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }
}