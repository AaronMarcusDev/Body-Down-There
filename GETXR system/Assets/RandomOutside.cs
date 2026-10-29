using UnityEngine;

public class RandomOutside : MonoBehaviour
{

    public AudioSource audioSource;
    public AudioClip[] outsideSounds;
    private AudioClip currentSound;

    public Collider outsideArea;
    public GameObject Player;
    private float timer = 30;

    void Update()
    {
        if (outsideArea.bounds.Contains(Player.transform.position))
        {
            
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                currentSound = outsideSounds[Random.Range(0, outsideSounds.Length)];
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
