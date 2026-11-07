using UnityEngine;

public class ChangeFogTrigger : MonoBehaviour
{
    [Header("Exponential Fog Instellingen")]
    [SerializeField] private float targetFogDensity = 0.05f; // thickness of the fog, 0.5 is already quite a lot of fog
    [SerializeField] private float transitionSpeed = 2f; // speed at which the fog adds up

    private bool playerInside = false;
    private float originalFogDensity;

    private void Start()
    {
        // Save the original fog so that we can alter it logically
        originalFogDensity = RenderSettings.fogDensity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
        }
    }

    private void Update()
    {
        if (playerInside)
        {
            // Sloooowwly add the fog with Lerp
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetFogDensity, Time.deltaTime * transitionSpeed);
        }
        else
        {
            // ^-- That but the reduction of fog instead
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, originalFogDensity, Time.deltaTime * transitionSpeed);
        }
    }
}