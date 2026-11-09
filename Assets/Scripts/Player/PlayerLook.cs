
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles first-person camera rotation for mouse and gamepad.
/// Provides adjustable sensitivity, controller response curves,
/// deadzone, smoothing, and vertical camera limits.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PlayerLook : MonoBehaviour
{
    #region Serialized Fields

    [Header("Mouse Settings")]

    [Tooltip("Controls how quickly the camera rotates when moving the mouse.")]
    [SerializeField, Range(0f, 1f)]
    private float mouseSensitivity = 0.1f;

    [Header("Controller Sensitivity")]

    [Tooltip("Horizontal camera rotation speed for the right analog stick.")]
    [SerializeField, Range(0f, 500f)]
    private float controllerLookSpeed = 210f;

    [Tooltip("Vertical camera rotation speed for the right analog stick.")]
    [SerializeField, Range(0f, 500f)]
    private float controllerVerticalLookSpeed = 170f;

    [Header("Controller Response")]

    [Tooltip("Ignores small analog stick movements to prevent unwanted camera drift.")]
    [SerializeField, Range(0f, 0.3f)]
    private float controllerDeadzone = 0.03f;

    [Tooltip("Higher values provide finer aiming near the center of the analog stick.")]
    [SerializeField, Range(1f, 3f)]
    private float controllerResponseCurve = 1.6f;

    [Tooltip("Smooths controller camera movement. Set to zero to disable smoothing.")]
    [SerializeField, Range(0f, 0.2f)]
    private float controllerSmoothingTime = 0.04f;

    [Header("Camera Limits")]

    [Tooltip("Maximum vertical angle the player can look up or down.")]
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
        if (playerInput.actions != null)
        {
            lookAction = playerInput.actions.FindAction(
                "Look",
                false
            );
        }
    }

    private void Start()
    {
        // Lock and hide the cursor during gameplay.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Process the player's camera movement.
        HandleLook();
    }

    private void OnDisable()
    {
        // Clear look input when controls are disabled.
        lookInput = Vector2.zero;
        smoothedControllerInput = Vector2.zero;
        controllerSmoothVelocity = Vector2.zero;

        // Release the cursor for menus and death screens.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    #endregion

    #region Camera Rotation

    /// <summary>
    /// Determines the active input device and processes
    /// mouse and controller movement independently.
    /// </summary>
    private void HandleLook()
    {
        // A camera and valid delta time are required.
        if (playerCamera == null || Time.deltaTime <= 0f)
        {
            return;
        }

        // Determine whether the Look action is
        // currently controlled by a gamepad.
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

        // Apply the appropriate look calculations.
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
    /// Applies mouse movement directly without
    /// controller smoothing or acceleration.
    /// </summary>
    private void HandleMouseLook()
    {
        // Convert mouse input into camera rotation.
        Vector2 rotation = lookInput * mouseSensitivity;

        ApplyRotation(
            rotation.x,
            rotation.y
        );
    }

    /// <summary>
    /// Applies deadzone, response curve, and smoothing
    /// before processing right-stick camera movement.
    /// </summary>
    private void HandleControllerLook()
    {
        // Process the analog stick input.
        Vector2 targetInput = ProcessControllerInput(
            lookInput
        );

        // Immediately stop when the stick is released.
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
            // Apply direct controller input.
            smoothedControllerInput = targetInput;
        }

        // Convert analog input into degrees per second.
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
    /// curve to processed gamepad input.
    /// </summary>
    private Vector2 ProcessControllerInput(Vector2 input)
    {
        // Limit the input magnitude to a valid range.
        float magnitude = Mathf.Clamp01(input.magnitude);

        // Ignore small movements around stick center.
        if (magnitude <= controllerDeadzone)
        {
            return Vector2.zero;
        }

        // Map the usable stick range from zero to one.
        float adjustedMagnitude = Mathf.InverseLerp(
            controllerDeadzone,
            1f,
            magnitude
        );

        // Apply the controller response curve.
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

        // Update and clamp vertical camera rotation.
        cameraPitch -= verticalRotation;

        cameraPitch = Mathf.Clamp(
            cameraPitch,
            -maxLookAngle,
            maxLookAngle
        );

        // Rotate the camera vertically without
        // changing the Player's horizontal rotation.
        playerCamera.localRotation = Quaternion.Euler(
            cameraPitch,
            0f,
            0f
        );
    }

    #endregion

    #region Input Callbacks

    /// <summary>
    /// Receives mouse delta or right-stick input
    /// from the existing Look action.
    /// </summary>
    /// <param name="value">Two-dimensional look input.</param>
    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    #endregion
}
