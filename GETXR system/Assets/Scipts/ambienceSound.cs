using UnityEngine;

public class ambienceSound : MonoBehaviour
{
    [Header("Areas")]
    [Tooltip("Sleep hier al je losse Box Colliders in.")]
    public Collider[] Areas; // Dit is nu een lijst (array) van colliders
    public GameObject Player;

    void Update()
    {
        // Veiligheidscheck of er wel colliders en een speler zijn
        if (Areas == null || Areas.Length == 0 || Player == null) return;

        Vector3 playerPos = Player.transform.position;

        // Pak als beginpunt de eerste collider
        Vector3 closestPoint = Areas[0].ClosestPoint(playerPos);
        float minSqrDst = (closestPoint - playerPos).sqrMagnitude;

        // Loop door alle andere colliders om te kijken welke het ábsoluut dichtstbij is
        for (int i = 1; i < Areas.Length; i++)
        {
            if (Areas[i] == null) continue;

            Vector3 point = Areas[i].ClosestPoint(playerPos);
            float sqrDst = (point - playerPos).sqrMagnitude;

            // Als deze collider dichterbij is dan de vorige, slaan we deze op
            if (sqrDst < minSqrDst)
            {
                minSqrDst = sqrDst;
                closestPoint = point;
            }
        }

        // Verplaats de ambience sound naar het dichtstbijzijnde punt van de gekozen collider
        transform.position = closestPoint;
    }
}