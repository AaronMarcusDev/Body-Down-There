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

    [Header("Jumpscare Audio")]
    [SerializeField] private AudioSource jumpscareAudioSource;
    [SerializeField] private AudioClip jumpscareSound;

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private Transform playerSpawnPoint; // Waar de speler zelf weer tot leven komt

    // Wordt aangeroepen door de EnemyCatchHandler van de robot die je grijpt
    public void TriggerJumpscare(GameObject player, EnemyCatchHandler enemyHandler)
    {
        StartCoroutine(JumpscareAndRespawnRoutine(player, enemyHandler));
    }

    private IEnumerator JumpscareAndRespawnRoutine(GameObject player, EnemyCatchHandler enemyHandler)
    {
        GameObject specificJumpscareCamera = null;
        Transform specificRobotSpawn = null;

        if (enemyHandler != null)
        {
            specificJumpscareCamera = enemyHandler.RobotJumpscareCamera;
            specificRobotSpawn = enemyHandler.RobotSpawnPoint;
        }

        // 1. Wissel naar de unieke jumpscare-camera van deze specifieke robot
        if (normalPlayerCamera != null) normalPlayerCamera.SetActive(false);
        if (specificJumpscareCamera != null) specificJumpscareCamera.SetActive(true);

        // Speel het jumpscare geluid af
        if (jumpscareAudioSource != null && jumpscareSound != null)
        {
            jumpscareAudioSource.PlayOneShot(jumpscareSound);
        }

        // Zet de speler tijdelijk uit
        if (player != null) player.SetActive(false);

        // 2. Wacht tot de jumpscare-vertraging voorbij is
        yield return new WaitForSeconds(respawnDelay);

        // --- RESPAWN SPELER ---
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

        // --- RESET DE AANVALLENDE ROBOT ---
        if (enemyHandler != null && specificRobotSpawn != null)
        {
            enemyHandler.ResetEnemy(specificRobotSpawn.position);
        }

        // 3. Zet de camera's en speler weer terug naar de normale gameplay
        if (specificJumpscareCamera != null) specificJumpscareCamera.SetActive(false);
        if (normalPlayerCamera != null) normalPlayerCamera.SetActive(true);
        if (player != null) player.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }
}