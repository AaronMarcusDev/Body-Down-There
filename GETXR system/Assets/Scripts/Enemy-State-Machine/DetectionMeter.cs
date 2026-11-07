using System.Diagnostics;
using UnityEngine;

public class DetectionMeter : MonoBehaviour
{
    [Header("Time")]
    [Tooltip("How long the player needs to be in the vision for the alert state")]
    public float timeToAlert = 1f;

    [Tooltip("How long the player needs to be in the vision for the investigate state")]
    public float timeToInvestigate = 3f;

    [Tooltip("How long the player needs to be in the vision for the attack state")]
    public float timeToAttack = 5f;

    [Header("Meter Drain")]
    [Tooltip("How quickly the meter drains when the player leaves sight")]
    public float drainSpeed = 0.5f;

    public void ResetMeter()
    {
        CurrentDetection = 0f;
    }
    // Important to know, the meter is from 0 - 1
    public float CurrentDetection { get; private set; } = 0f;
    //                                      ^-- the setters may only be changed from within

    // Useful values for the statemachine as booleans
    // Here it is already being converted to a fraction less than 1, which is essentially the threshold for the states :O
    public bool IsAlerted => CurrentDetection >= (timeToAlert / timeToAttack);
    public bool IsInvestigating => CurrentDetection >= (timeToInvestigate / timeToAttack);
    public bool IsAttacking => CurrentDetection >= 1f;

    private void Update()
    {
        // Retrieve the VisionSensor that is on the same enemy
        VisionSensor VS = GetComponent<VisionSensor>();

        if (VS == null)
        {
            DebugLogs.msg(DebugLogs.DebugOption.MissingVisionSensor);
            return;
        }

        if (VS.CanSeePlayer)
        {
            // Here the meter is being filled, because the value has to be 0-1,
            // I divide 1 by the time. 
            // So, for example, 1 / 5 seconds = 0.2 (per second)
            CurrentDetection += (1f / timeToAttack) * Time.deltaTime;
            //                                             ^-- so it adds up the meter
            CurrentDetection = Mathf.Clamp01(CurrentDetection);
            // As a safety I clamp it between 0 and 1, because you can never be too safe
        }
        else
        {
            // Here the meter drains
            CurrentDetection -= drainSpeed * Time.deltaTime;
            CurrentDetection = Mathf.Clamp01(CurrentDetection); // ''
        }
    }
}