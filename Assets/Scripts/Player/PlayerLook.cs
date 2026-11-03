
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles first-person camera rotation for mouse and gamepad.
/// Provides adjustable controller sensitivity, response curves,
/// deadzone, and smoothing for more precise aiming.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PlayerLook : MonoBehaviour
{
    #region Serialized Fields

    [Header("Mouse Settings")]
    [SerializeField, Min(0f)]
    private float mouseSensitivity = 0.1f;

    [Header("Controller Sensitivity")]
    [SerializeField, Min(0f)]
    private float controllerLookSpeed = 210f;

    [SerializeField, Min(0f)]
    private float controllerVerticalLookSpeed = 170f;

    [Header("Controller Response")]
    [SerializeField, Range(0f, 0.3f)]
    private float controllerDeadzone = 0.03f;

    [SerializeField, Range(1f, 3f)]
    private float controllerResponseCurve = 1.6f;

    [SerializeField, Range(0f, 0.2f)]
    private float controllerSmoothingTime = 0.04f;

    [Header("Camera Limits")]
    [SerializeField, Range(45f, 89f)]
    private float maxLookAngle = 80f;

    #endregion

    #region Private Fields

    private PlayerInput playerInput;
    private Transform playerCamera;
    private InputAction lookAction;

    private Vector2 lookInput;
    private Vector2 smoothedControllerInput;
    private Vector2 controllerSmoothVelocity;

    private float cameraPitch;
    private bool wasUsingGamepad;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Cache the PlayerInput component.
        playerInput = GetComponent<PlayerInput>();

        // Get the camera assigned to PlayerInput.
        if (playerInput.camera != null)
        {
            playerCamera = playerInput.camera.transform;

            // Preserve the camera's starting pitch.
            cameraPitch = Mathf.DeltaAngle(
                0f,
                playerCamera.localEulerAngles.x
            );
        }
        else
        {
            Debug.LogError(
                "PlayerLook requires a camera assigned in Player Input.",
                this
            );
        }

        // Locate the existing Look input action.
        lookAction = playerInput.actions.FindAction("Look");
    }

    private void Start()
    {
        // Lock the cursor during active gameplay.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleLook();
    }

    private void OnDisable()
    {
        // Reset look input when gameplay is disabled.
        lookInput = Vector2.zero;
        smoothedControllerInput = Vector2.zero;
        controllerSmoothVelocity = Vector2.zero;

        // Release the cursor for menus.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    #endregion

    #region Camera Rotation

    /// <summary>
    /// Determines the current input device and processes
    /// mouse and controller aiming independently.
    /// </summary>
    private void HandleLook()
    {
        if (playerCamera == null || Time.deltaTime <= 0f)
        {
            return;
        }

        // Identify the device currently driving Look.
        bool usingGamepad =
            lookAction != null &&
            lookAction.activeControl?.device is Gamepad;

        // Reset smoothing when changing input devices.
        if (usingGamepad != wasUsingGamepad)
        {
            smoothedControllerInput = Vector2.zero;
            controllerSmoothVelocity = Vector2.zero;
            wasUsingGamepad = usingGamepad;
        }

        if (usingGamepad)
        {
            HandleControllerLook();
        }
        else
        {
            HandleMouseLook();
        }
    }

    /// <summary>
    /// Applies direct mouse movement without controller
    /// acceleration or smoothing.
    /// </summary>
    private void HandleMouseLook()
    {
        Vector2 rotation = lookInput * mouseSensitivity;

        ApplyRotation(rotation.x, rotation.y);
    }

    /// <summary>
    /// Applies deadzone, response curve, and smoothing
    /// before rotating the camera with the right stick.
    /// </summary>
    private void HandleControllerLook()
    {
        // Process the analog stick for precision aiming.
        Vector2 targetInput = ProcessControllerInput(
            lookInput
        );

        // Stop immediately when the stick is released
        // to prevent unwanted camera drift.
        if (targetInput.sqrMagnitude <= 0.000001f)
        {
            smoothedControllerInput = Vector2.zero;
            controllerSmoothVelocity = Vector2.zero;
        }
        else if (controllerSmoothingTime > 0f)
        {
            // Smooth rapid changes in stick direction.
            smoothedControllerInput = Vector2.SmoothDamp(
                smoothedControllerInput,
                targetInput,
                ref controllerSmoothVelocity,
                controllerSmoothingTime
            );
        }
        else
        {
            smoothedControllerInput = targetInput;
        }

        // Convert stick movement into degrees per second.
        float horizontalRotation =
            smoothedControllerInput.x *
            controllerLookSpeed *
            Time.deltaTime;

        float verticalRotation =
            smoothedControllerInput.y *
            controllerVerticalLookSpeed *
            Time.deltaTime;

        ApplyRotation(
            horizontalRotation,
            verticalRotation
        );
    }

    /// <summary>
    /// Applies an additional radial deadzone and response
    /// curve to processed gamepad stick input.
    /// </summary>
    private Vector2 ProcessControllerInput(Vector2 input)
    {
        float magnitude = Mathf.Clamp01(input.magnitude);

        // Ignore tiny movements near stick center.
        if (magnitude <= controllerDeadzone)
        {
            return Vector2.zero;
        }

        // Map the usable stick range to zero through one.
        float adjustedMagnitude = Mathf.InverseLerp(
            controllerDeadzone,
            1f,
            magnitude
        );

        // Higher curve values provide finer control
        // near the center of the analog stick.
        float curvedMagnitude = Mathf.Pow(
            adjustedMagnitude,
            controllerResponseCurve
        );

        return input.normalized * curvedMagnitude;
    }

    /// <summary>
    /// Rotates the Player horizontally and applies
    /// limited vertical pitch to the camera.
    /// </summary>
    private void ApplyRotation(
        float horizontalRotation,
        float verticalRotation
    )
    {
        // Rotate the Player around the vertical axis.
        transform.Rotate(
            Vector3.up * horizontalRotation
        );

        // Update and clamp the camera pitch.
        cameraPitch -= verticalRotation;

        cameraPitch = Mathf.Clamp(
            cameraPitch,
            -maxLookAngle,
            maxLookAngle
        );

        // Apply pitch without rotating the Player body.
        playerCamera.localRotation = Quaternion.Euler(
            cameraPitch,
            0f,
            0f
        );
    }

    #endregion

    #region Input Callbacks

    /// <summary>
    /// Receives mouse delta or right-stick input from
    /// the existing Look action.
    /// </summary>
    /// <param name="value">Two-dimensional look input.</param>
    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    #endregion
}