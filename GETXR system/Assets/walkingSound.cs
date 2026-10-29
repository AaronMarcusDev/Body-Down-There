using UnityEngine;

public class walkingSound : MonoBehaviour
{
    public Collider Area1;
    public Collider Area2;
    public Collider Area3;
    public AudioSource audioSource;

    public AudioClip[] outsideWalking;
    public AudioClip[] insideWalking;
    public AudioClip[] undergroundWalking;

    public GameObject Player;
  
    void Update()
    {
        
    }
}
