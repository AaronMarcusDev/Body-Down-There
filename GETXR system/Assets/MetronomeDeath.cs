using UnityEngine;
using UnityEngine.SceneManagement;

public class MetronomeDeath : MonoBehaviour
{
    private Transform playerTrans;
    public float killDistance = 2f;

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");

        if (player != null) { playerTrans = player.transform; }
        else { Debug.Log("<metronome death> Player not found"); }
    }

    void Update()
    {
        if (playerTrans == null) return;

        Vector3 heading = playerTrans.position - transform.position;
        Vector3 direction = heading.normalized;
        float distance = heading.magnitude;

        if (distance < killDistance)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
        }
    }
}
