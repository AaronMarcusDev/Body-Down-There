using UnityEngine;

public class ambienceSound : MonoBehaviour
{
    [Header("Areas")]
    [Tooltip("Sleep hier al je losse Box Colliders in.")]
    public Collider[] Areas; // Dit is nu een lijst (array) van colliders
    public GameObject Player;

    void Update()
    {
        // null checks
        if (Areas == null || Areas.Length == 0 || Player == null) return;

        Vector3 playerPos = Player.transform.position;

        // Start at the first collider, so we have to set it there first (just the first index of the areas)
        Vector3 closestPoint = Areas[0].ClosestPoint(playerPos);
        float minSqrDst = (closestPoint - playerPos).sqrMagnitude;

        // get the closest collider by looping through them
        for (int i = 1; i < Areas.Length; i++)
        {
            if (Areas[i] == null) continue; // and skip the nonexistant ones ofcourse so Unity doesn't crash

            Vector3 point = Areas[i].ClosestPoint(playerPos);
            float sqrDst = (point - playerPos).sqrMagnitude; // distance is the magnitude here, makes sense right

            // simple sorting algorithm basically with a distance condition
            if (sqrDst < minSqrDst)
            {
                minSqrDst = sqrDst;
                closestPoint = point;
            }
        }

        // Move the sound to the closest one that was just found
        transform.position = closestPoint;
    }
}