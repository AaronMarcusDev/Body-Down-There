using UnityEngine;

public class RandomNoise : MonoBehaviour
{

    public AudioSource audioSource;
    public AudioClip[] randomSounds;
    private AudioClip currentSound;

    public Collider Area;
    public GameObject Player;
    private float timer = 30;

    void Update()
    {
        Debug.Log(timer);
        if (Area.bounds.Contains(Player.transform.position))
        {
            
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                currentSound = randomSounds[Random.Range(0, randomSounds.Length)];
                audioSource.PlayOneShot(currentSound);

                // Reset timer
                timer += 35f;
            }
        }
        else
        {
            // Reset timer when leaving so it doesn't instantly trigger upon re-entry
            timer = 35f;
        }
    }
}
