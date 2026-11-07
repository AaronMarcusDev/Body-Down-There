using UnityEngine;
using UnityEngine.SceneManagement;

public class TriggerSceneLoad : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // go back to the previous scene, which is always the menu in our case since that is index 0
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
        }
    }
}