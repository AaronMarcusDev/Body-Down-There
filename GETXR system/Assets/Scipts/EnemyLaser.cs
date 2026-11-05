using System.Collections;
using UnityEngine;

public class EnemyLaser : MonoBehaviour
{
    public Transform player;
    public float cooldown = 3f;
    public float beamLength = 20f;
    public float beamWidth = 0.5f;
    public float beamHeightOffset = 0f;
    public float beamHeight = 1.5f;

    [Tooltip("Layers the laser beam will pass through and NOT hit")]
    public LayerMask ignoredLayers;   // ← shows as a dropdown in the Inspector

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;
        StartCoroutine(FireLoop());
    }

    IEnumerator FireLoop()
    {
        while (true)
        {
            // yield return new WaitForSeconds(cooldown);
            FireLaser();
        }
    }

    void FireLaser()
    {
        if (player == null) return;

        var laser = GameObject.CreatePrimitive(PrimitiveType.Cube);
        laser.name = "Laser";
        laser.transform.position = transform.position;

        Vector3 aimFrom = transform.position + Vector3.up * beamHeightOffset;
        Vector3 dir = (player.position - aimFrom).normalized;
        laser.transform.rotation = Quaternion.LookRotation(dir);

        laser.transform.localScale = new Vector3(beamWidth, beamHeight, beamLength);

        laser.transform.position = aimFrom + dir * (beamLength / 2f);

        laser.GetComponent<BoxCollider>().isTrigger = true;

        var mr = laser.GetComponent<MeshRenderer>();
        mr.material.color = Color.red;

        // Pass the ignore mask into the laser
        laser.AddComponent<Laser>().Init(ignoredLayers);

        Destroy(laser, 0.3f);
    }
}