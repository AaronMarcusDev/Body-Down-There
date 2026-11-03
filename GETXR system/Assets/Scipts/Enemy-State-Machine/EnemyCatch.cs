using UnityEngine;

public class EnemyCatch : MonoBehaviour
{
    public string playerTag = "Player";
    public float catchDistance = 1.5f; // Hoe dichtbij de robot moet zijn om te pakken
    private Transform player;

    void Start()
    {
        GameObject pObj = GameObject.FindGameObjectWithTag(playerTag);
        if (pObj != null) player = pObj.transform;
    }

    void Update()
    {
        if (player == null) return;

        // Bereken de afstand tussen de robot en de speler
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= catchDistance)
        {
            GrabPlayer();
        }
    }

    void GrabPlayer()
    {
        // Roep de centrale GameManager aan om de respawn te starten
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerRespawn(player.gameObject, this.gameObject);
        }
    }
}