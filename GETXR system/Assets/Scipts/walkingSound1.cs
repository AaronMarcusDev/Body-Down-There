using UnityEngine;

public class WalkingSound : MonoBehaviour
{
    [Header("Walking Areas (Meerdere colliders mogelijk)")]
    public Collider[] outsideAreas;
    public Collider[] insideAreas;
    public Collider[] undergroundAreas;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip[] outsideWalking;
    public AudioClip[] insideWalking;
    public AudioClip[] undergroundWalking;
    private AudioClip currentSound;

    public GameObject Player;

    public void Footsteps()
    {
        if (Player == null || audioSource == null) return;

        Vector3 playerPos = Player.transform.position;

        float volumeOutside = Random.Range(0.2f, 0.5f);
        float volumeInside = Random.Range(0.8f, 1f);
        float volumeUnderground = Random.Range(0.8f, 1f);

        // Check of de speler in een van de buiten-gebieden is
        if (IsInAnyArea(outsideAreas, playerPos))
        {
            PlaySound(outsideWalking, volumeOutside);
        }
        // Check of de speler in een van de binnen-gebieden is
        else if (IsInAnyArea(insideAreas, playerPos))
        {
            PlaySound(insideWalking, volumeInside);
        }
        // Check of de speler in een van de ondergrondse-gebieden is
        else if (IsInAnyArea(undergroundAreas, playerPos))
        {
            PlaySound(undergroundWalking, volumeUnderground);
        }
    }

    // Hulpfunctie die controleert of de speler in ÉÉN van de colliders in de lijst zit
    private bool IsInAnyArea(Collider[] areas, Vector3 pos)
    {
        if (areas == null) return false;

        foreach (Collider area in areas)
        {
            if (area != null && area.bounds.Contains(pos))
            {
                return true; // Hij is gevonden!
            }
        }
        return false;
    }

    // Kleine helper om code te besparen voor het afspelen
    private void PlaySound(AudioClip[] clips, float volume)
    {
        if (clips != null && clips.Length > 0)
        {
            currentSound = clips[Random.Range(0, clips.Length)];
            audioSource.PlayOneShot(currentSound, volume);
        }
    }
}