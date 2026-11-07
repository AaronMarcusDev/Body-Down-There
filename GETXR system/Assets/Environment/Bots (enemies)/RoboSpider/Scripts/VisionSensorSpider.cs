using UnityEngine;

[RequireComponent(typeof(Transform))]

// I have 'designed' this class with three main steps:
// 1. a radius/distance check
// 2. an fov check similar to Maths3D
// 3. A raycast check to see whether there is not an object in the way, that's why there are layers below.
// I hope the rest of the code explains itself :)

public class VisionSensorSpider : MonoBehaviour
{
    [Header("Config")]
    public float viewRange = 10f;

    [Range(0f, 360f)] // just a circle all around
    public float fieldOfView = 90f;

    [Header("Layers")]
    public LayerMask playerLayer;
    public LayerMask obstacleLayer; // to block the raycast

    [Header("Debug")]
    public bool debugMode = false;
    public bool drawGizmos = true;

    // Output variables (public)
    public bool CanSeePlayer { get; private set; } // auto properties for shorthand notation
    //                                      ^-- the setters may only be changed from within

    public Vector3 LastSeenPosition { get; private set; } // ''

    // Private variables (_)
    private Transform _currPlayerTrans;
    private bool _prevSeenBool; // to compare to on a state change
                                //                      ^-- state, as in state of the player in vision t/f

    private void Awake()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player"); // retrieve player and set the transform
        if (playerObj != null)
        {
            _currPlayerTrans = playerObj.transform;
            if (debugMode)
            {
                // nice to have as feedback
                DebugLogs.msg(DebugLogs.DebugOption.FoundPlayer, _currPlayerTrans.name);
            }
        }
        else
        {
            // is useful to check whether the player tag is assigned for example,
            //  independent of the debug mode as this is important
            DebugLogs.msg(DebugLogs.DebugOption.NoPlayer);
        }
    }

    private void Update()
    {
        CanSeePlayer = CheckVision();

        if (CanSeePlayer != _prevSeenBool) // player enters or leaves vision compared to previous frame
        {
            if (debugMode)
            {
                DebugLogs.msg(DebugLogs.DebugOption.SightChange, CanSeePlayer.ToString());
            }
            _prevSeenBool = CanSeePlayer;
        }
    }

    private bool CheckVision()
    {
        if (_currPlayerTrans == null) return false;

        // vector from the enemy to the player
        Vector3 toPlayer = _currPlayerTrans.position - transform.position;
        float distance = toPlayer.magnitude;

        // Check whether the player is in the vision range :O
        if (distance > viewRange) return false;

        // Check of the FOV angle 
        float angle = Vector3.Angle(transform.forward, toPlayer);
        // ^-- much easier than in Math3D haha

        // the angle is from one side of the z axis, zo if it is large then half of the FOV then the player is out of sight
        if (angle > fieldOfView * 0.5f)
        {
            if (debugMode)
            {
                DebugLogs.msg(DebugLogs.DebugOption.FOVCheck);
                UnityEngine.Debug.Log($"Angle: {angle}, FOV/2: {fieldOfView * 0.5f}");
            }
            return false;
        }

        // Checking if there are no objects in the way with a raycast (nice for the player so they can hide)
        if (Physics.Raycast(transform.position, toPlayer.normalized, out RaycastHit hit, distance, obstacleLayer, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        LastSeenPosition = _currPlayerTrans.position; // update the known position
        return true; // the player is in the FOV
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRange);

        float halfFov = fieldOfView * 0.5f;
        Vector3 leftDir = Quaternion.Euler(0, -halfFov, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, halfFov, 0) * transform.forward;

        Gizmos.DrawRay(transform.position, leftDir * viewRange);
        Gizmos.DrawRay(transform.position, rightDir * viewRange);

        int steps = 20;
        Vector3 prev = transform.position + leftDir * viewRange;
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 dir = Quaternion.Euler(0, -halfFov + fieldOfView * t, 0) * transform.forward;
            Vector3 point = transform.position + dir * viewRange;
            Gizmos.DrawLine(prev, point);
            prev = point;
        }

        if (CanSeePlayer)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, LastSeenPosition);
        }
    }
}