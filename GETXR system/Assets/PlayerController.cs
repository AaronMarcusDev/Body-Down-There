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

    [Header("Gravity & Grounding")]
    [SerializeField] private float gravity = 15f;
    [SerializeField] private float maxFallSpeed = 20f;
    private float currentFallVelocity;

    [Header("Layer Masks to Ignore")]
    [SerializeField] private LayerMask ignoreLayers;

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

    // Walking Sound class
    private WalkingSound walkingSound;

    private Camera cam;

    private void Awake()
    {
        // Init methods from other class
        walkingSound = GetComponent<WalkingSound>();

        // Cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Camera
        if (viewCamera == null && Camera.main != null) viewCamera = Camera.main.transform;

        cam = viewCamera != null ? viewCamera.GetComponent<Camera>() : GetComponentInChildren<Camera>();
        if (cam != null) cam.fieldOfView = baseFov;

        targetPosition = transform.position;
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
        HandleGravity(); // Zwaartekracht continu controleren en toepassen
    }

    private void LateUpdate()
    {
        // Smoothly slide the player to the new step / fall position
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref positionVelocity, movementSmoothTime);
    }

    private void Look()
    {
        Vector2 rawLook = lookAction.action.ReadValue<Vector2>();

        smoothedLook = Vector2.SmoothDamp(smoothedLook, rawLook, ref lookVelocity, lookSmoothing);

        transform.Rotate(Vector3.up * smoothedLook.x * lookSensitivity);

        pitch -= smoothedLook.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

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

        Vector3 dir = transform.forward;
        Vector3 origin = targetPosition + Vector3.up * 0.9f;

        int mask = Physics.AllLayers & ~ignoreLayers.value;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, stepRayDistance, mask, QueryTriggerInteraction.Ignore))
        {
            float allowed = Mathf.Max(0f, hit.distance - 0.3f);
            targetPosition += dir * Mathf.Min(stepDistance, allowed);
        }
        else
        {
            targetPosition += dir * stepDistance;
        }

        if (walkingSound != null)
        {
            walkingSound.Footsteps();
        }
    }

    private void HandleGravity()
    {
        int mask = Physics.AllLayers & ~ignoreLayers.value;
        // Raycast omlaag vanaf de targetPosition om de vloer te vinden
        Vector3 rayOrigin = targetPosition + Vector3.up * 0.2f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 1.5f, mask, QueryTriggerInteraction.Ignore))
        {
            // Speler staat op de grond
            currentFallVelocity = 0f;
            targetPosition.y = hit.point.y; // Snap exact naar de vloerhoogte
        }
        else
        {
            // Geen grond onder ons -> vallen!
            currentFallVelocity += gravity * Time.deltaTime;
            currentFallVelocity = Mathf.Min(currentFallVelocity, maxFallSpeed);
            targetPosition.y -= currentFallVelocity * Time.deltaTime;
        }
    }

    private void UpdateTilt()
    {
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
        }
    }

    private void UpdateFov()
    {
        fovBoost = Mathf.MoveTowards(fovBoost, 0f, Time.deltaTime * fovBoostPerFastClick * 1.5f);

        if (cam != null)
        {
            float targetFov = baseFov + fovBoost;
            cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetFov, ref fovVelocity, fovSmoothTime);
        }
    }
}