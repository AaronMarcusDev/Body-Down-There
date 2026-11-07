using UnityEngine;
using UnityEngine.SceneManagement;

public class TriggerSceneLoad : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Controleer optioneel of het de speler is (als je speler de tag "Player" heeft)
        if (other.CompareTag("Player"))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Laadt de vorige scène in de Build Settings
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
        }
    }
}