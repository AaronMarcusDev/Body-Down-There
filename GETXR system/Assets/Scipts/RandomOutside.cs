using UnityEngine;

public class RandomOutside : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] outsideSounds;
    private AudioClip currentSound;

    [Header("Outside Areas (Meerdere colliders mogelijk)")]
    public Collider[] outsideAreas;
    public GameObject Player;
    private float timer = 30f;

    void Update()
    {
        if (Player == null) return;

        // Check of de speler in ÉÉN van de buiten-gebieden is
        if (IsInAnyArea(outsideAreas, Player.transform.position))
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                if (outsideSounds != null && outsideSounds.Length > 0 && audioSource != null)
                {
                    currentSound = outsideSounds[Random.Range(0, outsideSounds.Length)];
                    audioSource.PlayOneShot(currentSound);
                }

                // Reset timer (terug naar 35 seconden)
                timer = 35f;
            }
        }
        else
        {
            // Reset timer when leaving so it doesn't instantly trigger upon re-entry
            timer = 30f;
        }
    }

    // Hulpfunctie die controleert of de speler in een van de colliders uit de lijst zit
    private bool IsInAnyArea(Collider[] areas, Vector3 pos)
    {
        if (areas == null) return false;

        foreach (Collider area in areas)
        {
            if (area != null && area.bounds.Contains(pos))
            {
                return true; // Gevonden!
            }
        }
        return false;
    }
}