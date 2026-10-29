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
    [Tooltip("Smooths out mouse jitter. Higher = snappier, Lower = smoother")]
    [SerializeField] private float lookSmoothing = 0.05f;

    [Header("Step / Walk")]
    [SerializeField] private Transform viewCamera;
    [SerializeField] private float stepDistance = 0.75f;
    [SerializeField] private float stepCooldown = 0.1f;
    [SerializeField] private float stepRayDistance = 2f;
    [Tooltip("How fast the player slides to the new step position")]
    [SerializeField] private float movementSmoothTime = 0.08f;

    [Header("Camera Tilt")]
    [SerializeField] private float maxTilt = 6f;
    [SerializeField] private float tiltSmoothTime = 0.1f;

    [Header("FOV / Running Simulation")]
    [SerializeField] private float baseFov = 60f;
    [SerializeField] private float maxFov = 75f;
    [SerializeField] private float fovSmoothTime = 0.3f;
    [SerializeField] private float fastClickThreshold = 0.30f;
    [SerializeField] private float fovBoostPerFastClick = 2.5f;

    // Internal State
    private float pitch;
    private float targetTilt;
    private float currentTilt;
    private float tiltVelocity;

    private float lastStepTime = -10f;
    private float fovBoost;
    private float fovVelocity;

    // Look Smoothing
    private Vector2 smoothedLook;
    private Vector2 lookVelocity;

    // Position Smoothing
    private Vector3 targetPosition;
    private Vector3 positionVelocity;

    private Camera cam;

    private void Awake()
    {
        if (viewCamera == null && Camera.main != null) viewCamera = Camera.main.transform;

        cam = viewCamera != null ? viewCamera.GetComponent<Camera>() : GetComponentInChildren<Camera>();
        //               if view camera is not null, set the camera to the child of the parent (the player)
        if (cam != null) cam.fieldOfView = baseFov;

        targetPosition = transform.position;
    }

    private void OnEnable()
    {
        if (lookAction != null) lookAction.action.Enable();
        if (stepLeftAction != null)
        {
            stepLeftAction.action.Enable();
            stepLeftAction.action.performed += OnStepLeft; // subscription service, see A4 from GETXR
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

    private void LateUpdate()
    {
        // Smoothly slide the player to the new step position
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref positionVelocity, movementSmoothTime);
        // once again, SmoothDamp is similar to Lerp but way more effect on these small switches
    }

    private void Look()
    {
        Vector2 rawLook = lookAction.action.ReadValue<Vector2>();

        // Smooth the mouse input to remove jitter
        smoothedLook = Vector2.SmoothDamp(smoothedLook, rawLook, ref lookVelocity, lookSmoothing);

        transform.Rotate(Vector3.up * smoothedLook.x * lookSensitivity);

        pitch -= smoothedLook.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        // Apply tilt smoothly
        currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, tiltSmoothTime);

        viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, currentTilt);
    }

    private void OnStepLeft(InputAction.CallbackContext ctx) => TakeStep(-1f);
    private void OnStepRight(InputAction.CallbackContext ctx) => TakeStep(1f);

    private void TakeStep(float side)
    {
        float now = Time.time;
        float sinceLast = now - lastStepTime;

        if (sinceLast < fastClickThreshold)
        {
            fovBoost = Mathf.Min(fovBoost + fovBoostPerFastClick, maxFov - baseFov);
        }

        if (sinceLast < stepCooldown) return;
        lastStepTime = now;

        targetTilt = -side * maxTilt;

        // Move the target position, not the actual position
        Vector3 dir = transform.forward;
        Vector3 origin = targetPosition + Vector3.up * 0.9f;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, stepRayDistance))
        {
            float allowed = Mathf.Max(0f, hit.distance - 0.3f);
            targetPosition += dir * Mathf.Min(stepDistance, allowed);
        }
        else
        {
            targetPosition += dir * stepDistance;
        }
    }

    private void UpdateTilt()
    {
        // If a button is currently held down, tilt. Otherwise, return to center.
        if (stepLeftAction.action.IsPressed())
        {
            targetTilt = maxTilt;
        }
        else if (stepRightAction.action.IsPressed())
        {
            targetTilt = -maxTilt;
        }
        else
        {
            targetTilt = 0f;
            // TODO: Maybe add the timer again for keeping the camera tilted for 3 seconds
        }
    }

    private void UpdateFov()
    {
        fovBoost = Mathf.MoveTowards(fovBoost, 0f, Time.deltaTime * fovBoostPerFastClick * 1.5f);
        // SO to GETXR assignment A6

        if (cam != null)
        {
            float targetFov = baseFov + fovBoost;
            // SmoothDamp once again, see above.
            cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetFov, ref fovVelocity, fovSmoothTime);
        }
    }
}