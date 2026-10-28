using UnityEngine;
using UnityEngine.InputSystem;

public class LookAndStepController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference stepLeftAction;
    [SerializeField] private InputActionReference stepRightAction;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Step / Walk")]
    [SerializeField] private Transform viewCamera;
    [SerializeField] private float stepDistance = 0.75f;
    [SerializeField] private float stepCooldown = 0.05f;
    [SerializeField] private float stepRayDistance = 2f;

    [Header("Camera Tilt")]
    [SerializeField] private float maxTilt = 6f;
    [SerializeField] private float tiltSpeed = 12f;
    [SerializeField] private float tiltReturnSpeed = 8f;

    [Header("FOV / Running Simulation")]
    [SerializeField] private float baseFov = 60f;
    [SerializeField] private float maxFov = 75f;
    [SerializeField] private float fovSmooth = 4f;
    // Fast clicking = interval below this threshold counts toward "running"
    [SerializeField] private float fastClickThreshold = 0.30f;
    [SerializeField] private float fovBoostPerFastClick = 2.5f;

    private float pitch;
    private float currentTilt;
    private float targetTilt;
    private float lastStepTime = -10f;

    private float fovBoost;
    private Camera cam;

    private void Awake()
    {
        if (viewCamera == null && Camera.main != null)
            viewCamera = Camera.main.transform;

        cam = viewCamera != null ? viewCamera.GetComponent<Camera>() : GetComponentInChildren<Camera>();
        if (cam != null) cam.fieldOfView = baseFov;
    }

    private void OnEnable()
    {
        if (lookAction != null) lookAction.action.Enable();
        if (stepLeftAction != null)
        {
            stepLeftAction.action.Enable();
            stepLeftAction.action.performed += OnStepLeft;
        }
        if (stepRightAction != null)
        {
            stepRightAction.action.Enable();
            stepRightAction.action.performed += OnStepRight;
        }
    }

    private void OnDisable()
    {
        if (lookAction != null) lookAction.action.Disable();
        if (stepLeftAction != null)
        {
            stepLeftAction.action.performed -= OnStepLeft;
            stepLeftAction.action.Disable();
        }
        if (stepRightAction != null)
        {
            stepRightAction.action.performed -= OnStepRight;
            stepRightAction.action.Disable();
        }
    }

    private void Update()
    {
        Look();
        UpdateTilt();
        UpdateFov();
    }

    private void Look()
    {
        Vector2 look = lookAction.action.ReadValue<Vector2>();

        transform.Rotate(Vector3.up * look.x * lookSensitivity);

        pitch -= look.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        // Keep tilt as a Z roll on the camera; combine with pitch.
        viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, currentTilt);
    }

    private void OnStepLeft(InputAction.CallbackContext ctx) => TakeStep(-1f);
    private void OnStepRight(InputAction.CallbackContext ctx) => TakeStep(1f);

    private void TakeStep(float side)
    {
        float now = Time.time;
        float sinceLast = now - lastStepTime;

        // Fast-clicking detection for FOV boost
        if (sinceLast < fastClickThreshold)
        {
            fovBoost = Mathf.Min(fovBoost + fovBoostPerFastClick, maxFov - baseFov);
        }

        if (sinceLast < stepCooldown) return;
        lastStepTime = now;

        // Tilt: left click -> tilt left (positive Z roll), right click -> tilt right (negative Z roll)
        targetTilt = -side * maxTilt;

        // Move forward one step (in the direction the player body is facing
        // after the look update). Uses a small raycast to avoid walking into walls.
        Vector3 dir = transform.forward;
        Vector3 origin = transform.position + Vector3.up * 0.9f;
        if (Physics.Raycast(origin, dir, out RaycastHit hit, stepRayDistance))
        {
            float allowed = Mathf.Max(0f, hit.distance - 0.3f);
            transform.position += dir * Mathf.Min(stepDistance, allowed);
        }
        else
        {
            transform.position += dir * stepDistance;
        }
    }

    private void UpdateTilt()
    {
        if (Mathf.Approximately(targetTilt, 0f) && Mathf.Abs(currentTilt) < 0.01f)
        {
            currentTilt = 0f;
            return;
        }

        // When idle (no new click), tilt eases back to 0.
        // We ease toward targetTilt which gets reset each frame after being consumed.
        currentTilt = Mathf.Lerp(currentTilt, 0f, Time.deltaTime * tiltReturnSpeed);

        // Apply the impulse target on top
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);

        // Decay target back toward 0 so it doesn't stick
        targetTilt = Mathf.MoveTowards(targetTilt, 0f, Time.deltaTime * tiltSpeed * maxTilt * 4f);
    }

    private void UpdateFov()
    {
        // FOV boost decays over time when not clicking fast
        fovBoost = Mathf.MoveTowards(fovBoost, 0f, Time.deltaTime * fovBoostPerFastClick * 1.5f);

        if (cam != null)
        {
            float targetFov = baseFov + fovBoost;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * fovSmooth);
        }
    }
}