using UnityEngine;

public class ChangeFogTrigger : MonoBehaviour
{
    [Header("Exponential Fog Instellingen")]
    [SerializeField] private float targetFogDensity = 0.05f; // Hoe dik de mist moet worden in deze zone (probeer bijv. 0.02 tot 0.1)
    [SerializeField] private float transitionSpeed = 2f;       // Hoe snel de mist opbouwt

    private bool playerInside = false;
    private float originalFogDensity;

    private void Start()
    {
        // Sla de standaard mist-dichtheid van de scène op
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
            // Laat de mist langzaam toenemen tot de gewenste target density
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetFogDensity, Time.deltaTime * transitionSpeed);
        }
        else
        {
            // Laat de mist langzaam terugkeren naar de normale waarde als je weggaat
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, originalFogDensity, Time.deltaTime * transitionSpeed);
        }
    }
}