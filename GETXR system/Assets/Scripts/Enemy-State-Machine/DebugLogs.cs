using UnityEngine;
class DebugLogs
{
    public enum DebugOption
    {
        SightChange,
        FoundPlayer,
        NoPlayer,
        CheckVision,
        FOVCheck,
        MissingVisionSensor
    }
    public static void msg(DebugOption dbo, string optionalValue = "")
    {
        switch (dbo)
        {
            case DebugOption.CheckVision:
                {
                    Debug.Log($"<VisionSensor> found player: {optionalValue}");
                    break;
                }
            case DebugOption.NoPlayer:
                {
                    Debug.LogWarning($"<VisionSensor> no object with tag 'Player' found.");
                    break;
                }
            case DebugOption.FoundPlayer:
                {
                    Debug.Log($"<VisionSensor> found player: {optionalValue}");
                    break;
                }
            case DebugOption.SightChange:
                {
                    Debug.Log($"<VisionSensor> sight changed: {optionalValue}");
                    break;
                }
            case DebugOption.FOVCheck:
                {
                    Debug.Log($"<VisionSensor> angle is outside of FOV.");
                    break;
                }
            case DebugOption.MissingVisionSensor:
                {
                    Debug.LogError($"<DetectionMeter> No VisionSensor found :(");
                    break;
                }

        }
    }
}