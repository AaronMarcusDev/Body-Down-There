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

        // Is the player in an outside area?
        if (IsInAnyArea(outsideAreas, playerPos))
        {
            PlaySound(outsideWalking, volumeOutside);
        }

        // Is the player in an inside area?
        else if (IsInAnyArea(insideAreas, playerPos))
        {
            PlaySound(insideWalking, volumeInside);
        }
        // Is the player in an underground area?
        else if (IsInAnyArea(undergroundAreas, playerPos))
        {
            PlaySound(undergroundWalking, volumeUnderground);
        }
    }

    // check if the player is in one of the collider areas at all
    private bool IsInAnyArea(Collider[] areas, Vector3 pos)
    {
        if (areas == null) return false;

        foreach (Collider area in areas)
        {
            if (area != null && area.bounds.Contains(pos))
            {
                return true; // yep, the player is
            }
        }
        return false; // nope he aint
    }

    // Just a small seperation for code reuse, that's all
    private void PlaySound(AudioClip[] clips, float volume)
    {
        if (clips != null && clips.Length > 0)
        {
            currentSound = clips[Random.Range(0, clips.Length)];
            audioSource.PlayOneShot(currentSound, volume);
        }
    }
}