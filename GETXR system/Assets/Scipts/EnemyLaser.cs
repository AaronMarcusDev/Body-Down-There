using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScreenRedEffect : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("Sleep hier je UI Panel naartoe dat rood moet worden.")]
    public Image redOverlay;

    [Header("Instellingen")]
    [Tooltip("Hoe lang (in seconden) het duurt voordat het scherm volledig rood is.")]
    public float timeToFullRed = 10f;

    private float currentTimer = 0f;
    private bool isTriggered = false;

    void Start()
    {
        if (redOverlay != null)
        {
            // Zorg dat het paneel aan het begin volledig onzichtbaar is
            Color c = redOverlay.color;
            c.a = 0f;
            redOverlay.color = c;
        }
        else
        {
            Debug.LogWarning("Geen redOverlay gekoppeld aan het script!");
        }
    }

    void Update()
    {
        if (isTriggered || redOverlay == null) return;

        // Tel de tijd op
        currentTimer += Time.deltaTime;
        
        // Bereken hoe ver we zijn (van 0.0 tot 1.0)
        float progress = Mathf.Clamp01(currentTimer / timeToFullRed);

        // Pas de doorzichtigheid (alpha) van het rode paneel aan
        Color c = redOverlay.color;
        c.a = progress;
        redOverlay.color = c;

        // Als de tijd om is en het scherm dus vol rood is
        if (currentTimer >= timeToFullRed)
        {
            isTriggered = true;
            HandleGameOver();
        }
    }

    void HandleGameOver()
    {
        Debug.Log("Scherm is volledig rood! Level wordt herladen.");
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        int previousSceneIndex = SceneManager.GetActiveScene().buildIndex - 1;
        if (previousSceneIndex >= 0)
        {
            SceneManager.LoadScene(previousSceneIndex);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}