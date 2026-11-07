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
        Debug.Log(timer);
        if (Player == null) return;

        // Check whether the player is in one of the outside areas
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

                // Reset timer (always 35)
                timer = 35f;
            }
        }
        else
        {
            // Reset timer when leaving so it doesn't instantly trigger when entering again
            timer = 30f;
        }
    }

    // title speaks for itself I think
    private bool IsInAnyArea(Collider[] areas, Vector3 pos)
    {
        if (areas == null) return false;

        foreach (Collider area in areas)
        {
            if (area != null && area.bounds.Contains(pos))
            {
                return true; // found whohoo
            }
        }
        return false;
    }
}