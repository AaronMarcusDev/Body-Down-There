using UnityEngine;
using UnityEngine.SceneManagement;

public class Laser : MonoBehaviour
{
    private LayerMask _ignoredLayers;
    private bool _alreadyHit;

    public System.Action OnPlayerHit;

    public void Init(LayerMask ignoredLayers)
    {
        _ignoredLayers = ignoredLayers;
    }

    void OnTriggerEnter(Collider other)
    {
        // Ignore anything on the ignored layers
        if (((1 << other.gameObject.layer) & _ignoredLayers) != 0)
            return;

        if (_alreadyHit) return;

        if (other.CompareTag("Player"))
        {
            _alreadyHit = true;
            OnPlayerHit?.Invoke();
            HandlePlayerHit(other.gameObject);
        }

    }

    void HandlePlayerHit(GameObject player)
    {
        Debug.Log("Laser hit the player!");
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }
}