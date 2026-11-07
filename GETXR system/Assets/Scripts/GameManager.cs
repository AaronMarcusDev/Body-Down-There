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
    [SerializeField] private Transform playerSpawnPoint;

    // Being called by the EnemyCatchHandler of the robot that hit/caught you
    public void TriggerJumpscare(GameObject player, EnemyCatchHandler enemyHandler)
    {
        StartCoroutine(JumpscareAndRespawnRoutine(player, enemyHandler)); // has to be a coroutine to not freeze the game
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

        // Switch to the 'jumpscare camera'
        if (normalPlayerCamera != null) normalPlayerCamera.SetActive(false);
        if (specificJumpscareCamera != null) specificJumpscareCamera.SetActive(true);

        // Jumpscare audio
        if (jumpscareAudioSource != null && jumpscareSound != null)
        {
            jumpscareAudioSource.PlayOneShot(jumpscareSound);
        }

        // Disable the player for a short moment
        if (player != null) player.SetActive(false);

        // yeild for a delay (no freezing the program here :) )
        yield return new WaitForSeconds(respawnDelay);

        // Respawning the player
        if (player != null)
        {
            CharacterController charController = player.GetComponent<CharacterController>(); // retrieve player
            if (charController != null) charController.enabled = false; // stop controls

            // back to spawn
            if (playerSpawnPoint != null)
            {
                player.transform.position = playerSpawnPoint.position;
                player.transform.rotation = playerSpawnPoint.rotation;
            }

            // enable controls again, yay
            if (charController != null) charController.enabled = true;
        }

        // And let's not forget to reset the enemy robot as well
        if (enemyHandler != null && specificRobotSpawn != null)
        {
            enemyHandler.ResetEnemy(specificRobotSpawn.position);
        }

        // Reset camera and player
        if (specificJumpscareCamera != null) specificJumpscareCamera.SetActive(false);
        if (normalPlayerCamera != null) normalPlayerCamera.SetActive(true);
        if (player != null) player.SetActive(true);

        // important to be able to use the cursor for the menu
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }
}