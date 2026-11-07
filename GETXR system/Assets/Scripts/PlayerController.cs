using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class LookAndStepController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference stepLeftAction;
    [SerializeField] private InputActionReference stepRightAction;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float maxPitch = 80f;
    [Tooltip("Smooths out mouse jitter. Lower is smoother")]
    [SerializeField] private float lookSmoothing = 0.05f;

    [Header("Walking")]
    [SerializeField] private Transform viewCamera;
    [SerializeField] private float stepDistance = 0.75f;
    [SerializeField] private float stepCooldown = 0.1f;
    [Tooltip("How fast the player slides to the new step position")]
    [SerializeField] private float movementSpeed = 8f;

    [Header("Gravity & Grounding")]
    [SerializeField] private float gravity = 15f;
    private float verticalVelocity;

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

    // Position State
    private Vector3 moveTargetDirection;
    private float remainingStepDistance;

    // Components
    private CharacterController characterController;
    private WalkingSound walkingSound;
    private Camera cam;

    // For the Metrobot
    public event Action StepTaken;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        walkingSound = GetComponent<WalkingSound>();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (viewCamera == null && Camera.main != null) viewCamera = Camera.main.transform;

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

        HandleMovementAndGravity();
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
        StepTaken?.Invoke();
        float now = Time.time;
        float sinceLast = now - lastStepTime;

        if (sinceLast < fastClickThreshold)
        {
            fovBoost = Mathf.Min(fovBoost + fovBoostPerFastClick, maxFov - baseFov);
        }

        if (sinceLast < stepCooldown) return;
        lastStepTime = now;

        targetTilt = -side * maxTilt;

        // make new stap with transform instead of raycast (was a raycast before, that's why I note this ~ Aaron)
        moveTargetDirection = transform.forward;
        remainingStepDistance = stepDistance;

        if (walkingSound != null)
        {
            walkingSound.Footsteps();
        }
    }

    private void HandleMovementAndGravity()
    {
        Vector3 finalMoveVelocity = Vector3.zero;

        // Calculate step movement (distance)
        if (remainingStepDistance > 0f)
        {
            float moveThisFrame = movementSpeed * Time.deltaTime;
            moveThisFrame = Mathf.Min(moveThisFrame, remainingStepDistance);

            finalMoveVelocity += moveTargetDirection * moveThisFrame;
            remainingStepDistance -= moveThisFrame;
        }

        // Calculate da gravity
        if (characterController.isGrounded)
        {
            verticalVelocity = -0.5f; // to stay on the ground instead of the 2f
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        finalMoveVelocity.y += verticalVelocity * Time.deltaTime;

        characterController.Move(finalMoveVelocity);
    }

    private void UpdateTilt()
    {
        if (stepLeftAction.action.IsPressed()) targetTilt = maxTilt;
        else if (stepRightAction.action.IsPressed()) targetTilt = -maxTilt;
        else targetTilt = 0f;
    }

    private void UpdateFov()
    {
        if (fovBoost > 0) fovBoost = Mathf.MoveTowards(fovBoost, 0f, Time.deltaTime * 5f);
        float targetFov = baseFov + fovBoost;
        if (cam != null) cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetFov, ref fovVelocity, fovSmoothTime);
    }
}
